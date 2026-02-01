using Microsoft.AspNetCore.Mvc;
using Tobiso.Web.Shared.DTOs;
using Microsoft.AspNetCore.StaticFiles;

namespace Tobiso.Web.Files.Controllers;

[ApiController]
[Route("api/files")]
public class FilesController : ControllerBase
{
    private readonly string _imagesPath;

    public FilesController(IWebHostEnvironment env)
    {
        _imagesPath = Path.Combine(env.ContentRootPath, "wwwroot", "images");
        Directory.CreateDirectory(_imagesPath);
    }

    [HttpGet]
    public ActionResult<IList<FileUploadResponse>> GetAllFiles([FromQuery] string? subDirectory = null)
    {
        var dir = _imagesPath;
        if (!string.IsNullOrEmpty(subDirectory))
        {
            dir = Path.Combine(dir, subDirectory);
        }

        if (!Directory.Exists(dir))
            return Ok(new List<FileUploadResponse>());

        var provider = new FileExtensionContentTypeProvider();

        var files = Directory.EnumerateFiles(dir)
            .Select(f => new FileInfo(f))
            .Select(fi => new FileUploadResponse
            {
                FileName = fi.Name,
                OriginalFileName = fi.Name,
                Url = $"/images/{fi.Name}",
                Size = fi.Length,
                ContentType = provider.TryGetContentType(fi.Name, out var ct) ? ct : "application/octet-stream"
            })
            .ToList();

        return Ok(files);
    }

    [HttpPost("upload")]
    public async Task<ActionResult<FileUploadResponse>> UploadImage([FromForm(Name = "file")] IFormFile? file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded");

        // sanitize filename
        var originalFileName = Path.GetFileName(file.FileName);
        var safeName = originalFileName;

        // ensure unique name
        var uniqueName = $"{DateTime.UtcNow.Ticks}_{safeName}";
        var savePath = Path.Combine(_imagesPath, uniqueName);

        await using (var fs = System.IO.File.Create(savePath))
        {
            await file.CopyToAsync(fs);
        }

        var result = new FileUploadResponse
        {
            FileName = uniqueName,
            OriginalFileName = originalFileName,
            Url = $"/images/{uniqueName}",
            Size = file.Length,
            ContentType = file.ContentType
        };

        return Ok(result);
    }

    [HttpDelete("{fileName}")]
    public ActionResult DeleteImage(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
            return BadRequest();

        var filePath = Path.Combine(_imagesPath, fileName);
        if (!System.IO.File.Exists(filePath))
            return NotFound();

        System.IO.File.Delete(filePath);

        return NoContent();
    }
}
