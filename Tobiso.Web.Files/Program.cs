using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Tobiso.Web.Shared.DTOs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddAuthentication("Basic")
    .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, Tobiso.Web.Files.Authentication.BasicAuthHandler>("Basic", null);
builder.Services.AddAuthorization();

// CORS: local dev origins in Development; production origins come from configuration
// (Cors:AllowedOrigins) so a real frontend domain can be granted access without ever
// falling back to a wildcard or leaving the policy pointed at localhost.
builder.Services.AddCors(options =>
{
    options.AddPolicy("Default", b =>
    {
        if (builder.Environment.IsDevelopment())
        {
            b.WithOrigins("http://localhost:5000", "https://localhost:5001", "http://localhost:7273", "https://localhost:7273",
                          "https://localhost:7270", "https://localhost:7271")
             .AllowAnyMethod()
             .AllowAnyHeader()
             .AllowCredentials();
        }
        else
        {
            var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
            b.WithOrigins(allowedOrigins)
             .AllowAnyMethod()
             .AllowAnyHeader()
             .AllowCredentials();
        }
    });
});

builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("basic", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "basic",
        Description = "Basic authentication for protected endpoints"
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "basic"
                }
            },
            new string[] {}
        }
    });
});

var app = builder.Build();

// Ensure folders exist
foreach (var folder in new[] { "images", "videos", "documents" })
{
    Directory.CreateDirectory(Path.Combine(app.Environment.ContentRootPath, "wwwroot", folder));
}

// Prevent the browser from MIME-sniffing served images as something executable (e.g. HTML/JS)
// if a client-supplied Content-Type or file content is ever misidentified.
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    await next();
});

// CORS must run before UseStaticFiles: the static files middleware short-circuits the
// pipeline once it serves a file, so registering UseCors after it (as before) meant
// image/video/document responses never got Access-Control-Allow-Origin/-Headers at all.
app.UseCors("Default");

// Serve static files from wwwroot/images at /images
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(Path.Combine(app.Environment.ContentRootPath, "wwwroot")),
    RequestPath = ""
});

if (app.Environment.IsDevelopment())
{
    // Redirect root to Swagger UI (dev only - don't advertise the API surface in production)
    app.MapGet("/", () => Results.Redirect("/swagger"));

    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Tobiso.Web.Files v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseAuthentication();
app.UseAuthorization();

app.UseHttpsRedirection();

app.MapControllers();

app.Run();
