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

// CORS for local development: allow frontend origins and Authorization header
builder.Services.AddCors(options =>
{
    options.AddPolicy("LocalDev", b => b
        .WithOrigins("http://localhost:5000", "https://localhost:5001", "http://localhost:7273", "https://localhost:7273")
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials()
    );
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
var filesRoot = Path.Combine(app.Environment.ContentRootPath, "wwwroot", "images");
Directory.CreateDirectory(filesRoot);

// Serve static files from wwwroot/images at /images
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(Path.Combine(app.Environment.ContentRootPath, "wwwroot")),
    RequestPath = ""
});

// Redirect root to Swagger UI
app.MapGet("/", () => Results.Redirect("/swagger"));


app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Tobiso.Web.Files v1");
    c.RoutePrefix = "swagger";
});

app.UseCors("LocalDev");

app.UseAuthentication();
app.UseAuthorization();

app.UseHttpsRedirection();

app.MapControllers();

app.Run();
