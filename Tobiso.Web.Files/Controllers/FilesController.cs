using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Tobiso.Web.Shared.DTOs;

namespace Tobiso.Web.Files.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = "Basic")]
public class FilesController : ControllerBase
{
    private const string FilesBaseAddress = "https://files.tobiso.com";

    // Per-category config: physical folder name (also the public URL segment), allowed
    // extensions (server-derived, never trusting the client-supplied ContentType), and
    // max upload size. SVG is intentionally excluded from images: it can carry inline
    // <script> and would execute when served inline.
    private static readonly Dictionary<string, (string Folder, string[] Exts, long MaxSize)> Categories =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["images"] = ("images", new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" }, 10 * 1024 * 1024L),
            ["videos"] = ("videos", new[] { ".mp4" }, 200 * 1024 * 1024L),
            ["documents"] = ("documents", new[] { ".docx", ".pdf", ".pptx", ".xlsx" }, 25 * 1024 * 1024L),
        };

    private readonly ILogger<FilesController> _logger;
    private readonly IWebHostEnvironment _environment;

    public FilesController(ILogger<FilesController> logger, IWebHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
    }

    [HttpPost("{category}/upload")]
    [RequestSizeLimit(210 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 210 * 1024 * 1024)]
    public async Task<ActionResult<FileUploadResponse>> Upload(string category, IFormFile file)
    {
        if (!Categories.TryGetValue(category, out var cat))
        {
            return BadRequest(new { error = "Neznámá kategorie souborů" });
        }

        try
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { error = "Žádný soubor nebyl nahrán" });
            }

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!cat.Exts.Contains(ext))
            {
                return BadRequest(new { error = $"Nepodporovaný typ souboru. Povolené jsou pouze: {string.Join(", ", cat.Exts)}" });
            }

            if (file.Length > cat.MaxSize)
            {
                return BadRequest(new { error = $"Soubor je příliš velký. Maximální velikost je {cat.MaxSize / (1024 * 1024)}MB" });
            }

            // Build a safe filename: strip any path components from the original name and append a
            // random suffix. This prevents path traversal (e.g. "../../wwwroot/x") and overwriting.
            var baseName = Path.GetFileNameWithoutExtension(file.FileName);
            baseName = string.Join("_", baseName.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
            if (string.IsNullOrEmpty(baseName)) baseName = category;
            var fileName = $"{baseName}-{Guid.NewGuid():N}{ext}";

            var uploadsPath = Path.Combine(_environment.WebRootPath ?? _environment.ContentRootPath, cat.Folder);

            if (!Directory.Exists(uploadsPath))
            {
                Directory.CreateDirectory(uploadsPath);
            }

            var filePath = Path.Combine(uploadsPath, fileName);

            // Defence in depth: ensure the resolved path stays inside the category directory.
            var uploadsRoot = Path.GetFullPath(uploadsPath) + Path.DirectorySeparatorChar;
            if (!Path.GetFullPath(filePath).StartsWith(uploadsRoot, StringComparison.Ordinal))
            {
                return BadRequest(new { error = "Neplatný název souboru" });
            }

            using (var stream = new FileStream(filePath, FileMode.CreateNew))
            {
                await file.CopyToAsync(stream);
            }

            var fileUrl = $"{FilesBaseAddress}/{cat.Folder}/{fileName}";

            var response = new FileUploadResponse
            {
                FileName = fileName,
                OriginalFileName = file.FileName,
                Url = fileUrl,
                Size = file.Length,
                ContentType = GetContentType(fileName)
            };

            _logger.LogInformation("Successfully uploaded {Category} file: {FileName} -> {StoredName}", category, file.FileName, fileName);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading {Category} file", category);
            return StatusCode(500, new { error = "Chyba při nahrávání souboru" });
        }
    }

    [HttpGet("{category}")]
    public ActionResult<IEnumerable<FileUploadResponse>> GetAllFiles(string category)
    {
        if (!Categories.TryGetValue(category, out var cat))
        {
            return BadRequest(new { error = "Neznámá kategorie souborů" });
        }

        try
        {
            var folderPath = Path.Combine(_environment.WebRootPath ?? _environment.ContentRootPath, cat.Folder);

            if (!Directory.Exists(folderPath))
            {
                return Ok(new List<FileUploadResponse>());
            }

            var files = Directory.GetFiles(folderPath)
                .Select(filePath =>
                {
                    var fileInfo = new FileInfo(filePath);
                    var fileName = fileInfo.Name;

                    return new FileUploadResponse
                    {
                        FileName = fileName,
                        OriginalFileName = fileName,
                        Url = $"{FilesBaseAddress}/{cat.Folder}/{fileName}",
                        Size = fileInfo.Length,
                        ContentType = GetContentType(fileName)
                    };
                })
                .ToList();

            return Ok(files);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting {Category} file list", category);
            return StatusCode(500, new { error = "Chyba při získávání seznamu souborů" });
        }
    }

    [HttpDelete("{category}/{fileName}")]
    public ActionResult DeleteFile(string category, string fileName)
    {
        if (!Categories.TryGetValue(category, out var cat))
        {
            return BadRequest(new { error = "Neznámá kategorie souborů" });
        }

        try
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return BadRequest(new { error = "Název souboru je vyžadován" });
            }

            // Strip any directory components so callers cannot traverse out of the category folder.
            var safeName = Path.GetFileName(fileName);
            if (string.IsNullOrWhiteSpace(safeName) || safeName != fileName)
            {
                return BadRequest(new { error = "Neplatný název souboru" });
            }

            var categoryRoot = Path.Combine(_environment.WebRootPath ?? _environment.ContentRootPath, cat.Folder);
            var filePath = Path.Combine(categoryRoot, safeName);
            if (!Path.GetFullPath(filePath).StartsWith(Path.GetFullPath(categoryRoot) + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            {
                return BadRequest(new { error = "Neplatný název souboru" });
            }

            if (!System.IO.File.Exists(filePath))
            {
                return NotFound(new { error = "Soubor nebyl nalezen" });
            }

            System.IO.File.Delete(filePath);

            _logger.LogInformation("Successfully deleted {Category} file: {FileName}", category, fileName);
            return Ok(new { message = "Soubor byl úspěšně smazán" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting {Category} file: {FileName}", category, fileName);
            return StatusCode(500, new { error = "Chyba při mazání souboru" });
        }
    }

    private string GetContentType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".mp4" => "video/mp4",
            ".pdf" => "application/pdf",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            _ => "application/octet-stream"
        };
    }
}
