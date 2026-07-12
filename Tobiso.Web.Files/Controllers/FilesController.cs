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
    private readonly ILogger<FilesController> _logger;
    private readonly IWebHostEnvironment _environment;
    private readonly IConfiguration _configuration;

    public FilesController(ILogger<FilesController> logger, IWebHostEnvironment environment, IConfiguration configuration)
    {
        _logger = logger;
        _environment = environment;
        _configuration = configuration;
    }

    [HttpPost("upload")]
    public async Task<ActionResult<FileUploadResponse>> UploadImage(
        IFormFile file,
        [FromForm] string? subDirectory = null)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { error = "Žádný soubor nebyl nahrán" });
            }

            // Validate by extension (server-derived), not the client-supplied ContentType which is spoofable.
            // SVG is intentionally excluded: it can carry inline <script> and would execute when served inline.
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var allowedExts = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            if (!allowedExts.Contains(ext))
            {
                return BadRequest(new { error = "Nepodporovaný typ souboru. Povolené jsou pouze: JPEG, PNG, GIF, WebP" });
            }

            const int maxFileSize = 10 * 1024 * 1024;
            if (file.Length > maxFileSize)
            {
                return BadRequest(new { error = "Soubor je příliš velký. Maximální velikost je 10MB" });
            }

            // Build a safe filename: strip any path components from the original name and append a
            // random suffix. This prevents path traversal (e.g. "../../wwwroot/x") and overwriting.
            var baseName = Path.GetFileNameWithoutExtension(file.FileName);
            baseName = string.Join("_", baseName.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
            if (string.IsNullOrEmpty(baseName)) baseName = "image";
            var fileName = $"{baseName}-{Guid.NewGuid():N}{ext}";

            var uploadsPath = Path.Combine(_environment.WebRootPath ?? _environment.ContentRootPath, "images");

            if (!Directory.Exists(uploadsPath))
            {
                Directory.CreateDirectory(uploadsPath);
            }

            var filePath = Path.Combine(uploadsPath, fileName);

            // Defence in depth: ensure the resolved path stays inside the images directory.
            var uploadsRoot = Path.GetFullPath(uploadsPath) + Path.DirectorySeparatorChar;
            if (!Path.GetFullPath(filePath).StartsWith(uploadsRoot, StringComparison.Ordinal))
            {
                return BadRequest(new { error = "Neplatný název souboru" });
            }

            using (var stream = new FileStream(filePath, FileMode.CreateNew))
            {
                await file.CopyToAsync(stream);
            }

            var baseUrl = _configuration["Api:BaseAddress"] ?? Request.Scheme + "://" + Request.Host;
            var fileUrl = $"{baseUrl}/images/{fileName}";

            var response = new FileUploadResponse
            {
                FileName = fileName,
                OriginalFileName = file.FileName,
                Url = fileUrl,
                Size = file.Length,
                ContentType = GetContentType(fileName)
            };

            _logger.LogInformation("Successfully uploaded file: {FileName} -> {StoredName}", file.FileName, fileName);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file");
            return StatusCode(500, new { error = "Chyba při nahrávání souboru" });
        }
    }

    [HttpGet]
    public ActionResult<IEnumerable<FileUploadResponse>> GetAllFiles([FromQuery] string? subDirectory = null)
    {
        try
        {
            var imagesPath = Path.Combine(_environment.WebRootPath ?? _environment.ContentRootPath, "images");

            if (!Directory.Exists(imagesPath))
            {
                return Ok(new List<FileUploadResponse>());
            }

            var baseUrl = _configuration["Api:BaseAddress"] ?? Request.Scheme + "://" + Request.Host;

            var files = Directory.GetFiles(imagesPath)
                .Select(filePath =>
                {
                    var fileInfo = new FileInfo(filePath);
                    var fileName = fileInfo.Name;

                    return new FileUploadResponse
                    {
                        FileName = fileName,
                        OriginalFileName = fileName,
                        Url = $"{baseUrl}/images/{fileName}",
                        Size = fileInfo.Length,
                        ContentType = GetContentType(fileName)
                    };
                })
                .ToList();

            return Ok(files);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting file list");
            return StatusCode(500, new { error = "Chyba při získávání seznamu souborů" });
        }
    }

    [HttpDelete("{fileName}")]
    public ActionResult DeleteImage(string fileName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return BadRequest(new { error = "Název souboru je vyžadován" });
            }

            // Strip any directory components so callers cannot traverse out of the images folder.
            var safeName = Path.GetFileName(fileName);
            if (string.IsNullOrWhiteSpace(safeName) || safeName != fileName)
            {
                return BadRequest(new { error = "Neplatný název souboru" });
            }

            var imagesRoot = Path.Combine(_environment.WebRootPath ?? _environment.ContentRootPath, "images");
            var filePath = Path.Combine(imagesRoot, safeName);
            if (!Path.GetFullPath(filePath).StartsWith(Path.GetFullPath(imagesRoot) + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            {
                return BadRequest(new { error = "Neplatný název souboru" });
            }

            if (!System.IO.File.Exists(filePath))
            {
                return NotFound(new { error = "Soubor nebyl nalezen" });
            }

            System.IO.File.Delete(filePath);

            _logger.LogInformation("Successfully deleted file: {FileName}", fileName);
            return Ok(new { message = "Soubor byl úspěšně smazán" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file: {FileName}", fileName);
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
            _ => "application/octet-stream"
        };
    }
}
