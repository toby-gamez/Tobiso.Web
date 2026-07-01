using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tobiso.Api.Authentication;
using Tobiso.Api.Infrastructure.Data;
using Tobiso.Web.Api.Helpers;
using Tobiso.Web.Api.Services;
using Tobiso.Web.Domain.Entities;
using Tobiso.Web.Shared.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Tobiso.Web.Api.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class PostsController : ControllerBase
{
    private readonly IPostService _postService;
    private readonly IWebHostEnvironment _env;
    private readonly TobisoDbContext _db;

    public PostsController(IPostService postService, IWebHostEnvironment env, TobisoDbContext db)
    {
        _postService = postService;
        _env = env;
        _db = db;
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> GetPosts([FromQuery] int? gradeId = null)
    {
        return Ok(await _postService.GetAll(gradeId));
    }

    [AllowAnonymous]
    [HttpGet("summaries")]
    public async Task<IActionResult> GetPostSummaries()
    {
        return Ok(await _postService.GetSummaries());
    }

    [AllowAnonymous]
    [HttpGet("links")]
    public async Task<IActionResult> GetPostLinks()
    {
        return Ok(await _postService.GetLinks());
    }

    [AllowAnonymous]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetPost(int id, [FromQuery] int? gradeId = null)
    {
        var post = await _postService.GetById(id, gradeId);
        if (post == null) return NotFound();
        return Ok(post);
    }

    [HttpPost]
    public async Task<IActionResult> CreatePost([FromBody] CreatePostRequest req)
    {
        var created = await _postService.Create(req);
        if (created == null) return BadRequest("Post se nepodařilo vytvořit.");
        return CreatedAtAction(nameof(GetPost), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdatePost(int id, [FromBody] UpdatePostRequest req)
    {
        var updated = await _postService.UpdateMetadata(id, req);
        if (!updated) return NotFound();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePost(int id)
    {
        var deleted = await _postService.Delete(id);
        if (!deleted) return NotFound();
        return NoContent();
    }

    [AllowAnonymous]
    [HttpGet("{id}/difficulty")]
    public async Task<IActionResult> GetDifficulty(int id)
    {
        var ratings = await _db.PostDifficultyRatings.Where(r => r.PostId == id).ToListAsync();
        return Ok(new
        {
            Easy  = ratings.Count(r => r.Rating == 1),
            Ok    = ratings.Count(r => r.Rating == 2),
            Hard  = ratings.Count(r => r.Rating == 3),
            Total = ratings.Count
        });
    }

    [AllowAnonymous]
    [HttpPost("{id}/rate-difficulty")]
    public async Task<IActionResult> RateDifficulty(int id, [FromBody] DifficultyRatingRequest req)
    {
        if (req == null || req.Rating < 1 || req.Rating > 3) return BadRequest();
        var deviceId = req.DeviceId ?? Request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";
        var existing = await _db.PostDifficultyRatings.FirstOrDefaultAsync(r => r.PostId == id && r.DeviceId == deviceId);
        if (existing != null) return Ok(new { message = "already_rated" });

        _db.PostDifficultyRatings.Add(new PostDifficultyRating
        {
            PostId = id, Rating = req.Rating, DeviceId = deviceId
        });
        await _db.SaveChangesAsync();
        return Ok(new { message = "ok" });
    }

    [AllowAnonymous]
    [HttpGet("{id}/video")]
    public async Task<IActionResult> GetVideo(int id)
    {
        var video = await _db.PostVideos.FirstOrDefaultAsync(v => v.PostId == id);
        if (video == null) return NotFound();
        return Ok(new { video.YoutubeUrl, video.Timestamp, video.Label });
    }

    [Authorize]
    [HttpPut("{id}/video")]
    public async Task<IActionResult> PutVideo(int id, [FromBody] PostVideoRequest req)
    {
        if (req == null) return BadRequest();
        var existing = await _db.PostVideos.FirstOrDefaultAsync(v => v.PostId == id);
        if (existing != null)
        {
            existing.YoutubeUrl = req.YoutubeUrl ?? "";
            existing.Timestamp = req.Timestamp;
            existing.Label = req.Label ?? "";
        }
        else
        {
            _db.PostVideos.Add(new PostVideo { PostId = id, YoutubeUrl = req.YoutubeUrl ?? "", Timestamp = req.Timestamp, Label = req.Label ?? "" });
        }
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("upload-md")]
    public async Task<IActionResult> UploadMdFiles([FromQuery] string directory)
    {
        if (string.IsNullOrWhiteSpace(directory))
            return BadRequest("Neplatná cesta ke složce.");

        var root = Path.GetFullPath(_env.ContentRootPath);
        var target = Path.GetFullPath(directory);
        if (!target.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            return BadRequest("Cesta musí být pod adresářem aplikace.");

        if (!Directory.Exists(target))
            return BadRequest("Neplatná cesta ke složce.");

        var uploader = new MdUploader(_postService);
        var posts = await uploader.UploadFromDirectory(target);
        return Ok(new { count = posts.Count, titles = posts.Select(p => p.Title).ToList() });
    }
}
