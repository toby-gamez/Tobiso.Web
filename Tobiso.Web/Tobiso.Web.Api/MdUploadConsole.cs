using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tobiso.Api.Infrastructure.Data;
using Tobiso.Web.Api.Helpers;
using Tobiso.Web.Api.Services;

namespace Tobiso.Web.Api;

public class MdUploadConsole
{
    public static async Task Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Zadejte cestu ke složce s .md soubory jako argument.");
            return;
        }
        var directoryPath = args[0];
        var host = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                // Registrace služeb jako v API
                services.AddDbContext<TobisoDbContext>(options =>
                    options.UseSqlServer(context.Configuration.GetConnectionString("DefaultConnection")));
                services.AddScoped<IPostService, PostService>();
            })
            .Build();
        using var scope = host.Services.CreateScope();
        var postService = scope.ServiceProvider.GetRequiredService<IPostService>();
        var uploader = new MdUploader(postService);
        var posts = await uploader.UploadFromDirectory(directoryPath);
        Console.WriteLine($"Nahráno {posts.Count} postů.");
        foreach (var post in posts)
        {
            Console.WriteLine($"{post.Title} -> {post.FilePath}");
        }
    }
}
