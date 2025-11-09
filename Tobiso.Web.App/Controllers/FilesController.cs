using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Tobiso.Web.Shared.DTOs;

namespace Tobiso.Web.App.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FilesController : ControllerBase
{
    private readonly ILogger<FilesController> _logger;
    private readonly IHostEnvironment _environment;
    private readonly IConfiguration _configuration;

    public FilesController(ILogger<FilesController> logger, IHostEnvironment environment, IConfiguration configuration)
    {
        _logger = logger;
        _environment = environment;
        _configuration = configuration;
    }

    /// <summary>
    /// Nahraje soubor - STEJNĚ jako v SentrySMP
    /// </summary>
    [HttpPost("upload")]
    public async Task<ActionResult<FileUploadResponse>> UploadImage(
        IFormFile file,
        [FromForm] string? subDirectory = null)
    {
        try
        {
            // Ignorum subDirectory parametr pro jednodušší implementaci podle SentrySMP
            
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { error = "Žádný soubor nebyl nahrán" });
            }

            // Ověř typ souboru
            var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp" };
            if (!allowedTypes.Contains(file.ContentType.ToLowerInvariant()))
            {
                return BadRequest(new { error = "Nepodporovaný typ souboru. Povolené jsou pouze: JPEG, PNG, GIF, WebP" });
            }

            // Ověř velikost souboru (max 10MB)
            const int maxFileSize = 10 * 1024 * 1024;
            if (file.Length > maxFileSize)
            {
                return BadRequest(new { error = "Soubor je příliš velký. Maximální velikost je 10MB" });
            }

            // Vytvoř unikátní název souboru
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            
            // Cesta do App/wwwroot/images složky (jako v SentrySMP)
            var appProject = Path.Combine(_environment.ContentRootPath, "..", "Tobiso.Web.App");
            var uploadsPath = Path.Combine(appProject, "wwwroot", "images");
            
            if (!Directory.Exists(uploadsPath))
            {
                Directory.CreateDirectory(uploadsPath);
            }

            var filePath = Path.Combine(uploadsPath, fileName);

            // Ulož soubor
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Vytvoř URL pro soubor - plnou URL pro admin aplikaci
            var baseUrl = _configuration["Api:BaseAddress"] ?? Request.Scheme + "://" + Request.Host;
            var fileUrl = $"{baseUrl}/images/{fileName}";

            var response = new FileUploadResponse
            {
                FileName = fileName,
                OriginalFileName = file.FileName,
                Url = fileUrl,
                Size = file.Length,
                ContentType = file.ContentType
            };

            _logger.LogInformation("Successfully uploaded file: {FileName} -> {FilePath}", file.FileName, fileName);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file");
            return StatusCode(500, new { error = "Chyba při nahrávání souboru" });
        }
    }

    /// <summary>
    /// Získá seznam všech souborů - GET endpoint (anonymní pro synchronizaci)
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public ActionResult<IEnumerable<FileUploadResponse>> GetAllFiles([FromQuery] string? subDirectory = null)
    {
        try
        {
            // Ignorum subDirectory parametr pro jednodušší implementaci podle SentrySMP
            
            // Cesta do App/wwwroot/images složky
            var appProject = Path.Combine(_environment.ContentRootPath, "..", "Tobiso.Web.App");
            var imagesPath = Path.Combine(appProject, "wwwroot", "images");

            if (!Directory.Exists(imagesPath))
            {
                return Ok(new List<FileUploadResponse>());
            }

            // Získej základní URL pro admin aplikaci
            var baseUrl = _configuration["Api:BaseAddress"] ?? Request.Scheme + "://" + Request.Host;
            
            var files = Directory.GetFiles(imagesPath)
                .Select(filePath =>
                {
                    var fileInfo = new FileInfo(filePath);
                    var fileName = fileInfo.Name;
                    
                    return new FileUploadResponse
                    {
                        FileName = fileName,
                        OriginalFileName = fileName, // Nemáme původní název uložený
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

    /// <summary>
    /// Smaže soubor - STEJNĚ jako v SentrySMP
    /// </summary>
    [HttpDelete("{fileName}")]
    public ActionResult DeleteImage(string fileName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return BadRequest(new { error = "Název souboru je vyžadován" });
            }

            // Cesta do App/wwwroot/images složky
            var appProject = Path.Combine(_environment.ContentRootPath, "..", "Tobiso.Web.App");
            var filePath = Path.Combine(appProject, "wwwroot", "images", fileName);

            if (!System.IO.File.Exists(filePath))
            {
                return NotFound(new { error = "Soubor nebyl nalezen" });
            }

            // Smaž soubor
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
}