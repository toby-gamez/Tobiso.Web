using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Tobiso.Web.Domain.Entities;
using Tobiso.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Tobiso.Web.Api.Services;
using Tobiso.Web.Shared.DTOs;
using Microsoft.Extensions.DependencyInjection;

namespace Tobiso.Web.Api.Helpers;

public class MdUploader
{
    private readonly IPostService _postService;

    public MdUploader(IPostService postService)
    {
        _postService = postService;
    }

    public async Task<List<PostResponse>> UploadFromDirectory(string directoryPath)
    {
        var posts = new List<PostResponse>();
        var files = Directory.GetFiles(directoryPath, "*.md");
        foreach (var file in files)
        {
            var lines = await File.ReadAllLinesAsync(file);
            if (lines.Length < 3) continue;
            var titleLine = lines[1];
            var title = titleLine.StartsWith("title:") ? titleLine.Substring(6).Trim() : "";
            var content = string.Join("\n", lines.Skip(3));
            var post = new PostResponse
            {
                Title = title,
                Content = content,
                FilePath = "/" + Path.GetFileName(file),
                UpdatedAt = null,
                CategoryId = null,
                Category = null
            };
            var created = await _postService.Create(post);
            if (created != null)
                posts.Add(created);
        }
        return posts;
    }

    public static async Task RunUpload(IServiceProvider services, string directoryPath)
    {
        var postService = services.GetRequiredService<IPostService>();
        var uploader = new MdUploader(postService);
        var posts = await uploader.UploadFromDirectory(directoryPath);
        Console.WriteLine($"Nahráno {posts.Count} postů.");
        foreach (var post in posts)
        {
            Console.WriteLine($"{post.Title} -> {post.FilePath}");
        }
    }
}
