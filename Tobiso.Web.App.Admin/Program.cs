using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Refit;
using Serilog;
using System.Text;
using Tobiso.Api.Authentication;
using Tobiso.Api.Infrastructure.Data;
using Tobiso.Web.Api.Services;
using Tobiso.Web.App.Admin.Components;
using Tobiso.Web.App.Authentication;
using Tobiso.Web.App.Handlers;
using Tobiso.Web.Shared.Interfaces;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Host.UseSerilog();

var services = builder.Services;

// Add services
services.Configure<BasicAuthOptions>(builder.Configuration.GetSection("Auth:Basic"));
services.AddScoped(sp =>
{
    var httpClientHandler = new HttpClientHandler();
    
    // In development, bypass SSL certificate validation
    if (builder.Environment.IsDevelopment())
    {
        httpClientHandler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
    }
    
    return new HttpClient(httpClientHandler)
    {
        BaseAddress = new Uri(builder.Configuration["Api:BaseAddress"] ?? "https://localhost:7270"),
    };
});
services.AddHttpContextAccessor();

services.AddDbContext<TobisoDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

var jwtSecret = builder.Configuration["Auth:Jwt:Secret"] ?? "";
services
    .AddAuthentication("SmartAuth")
    .AddPolicyScheme("SmartAuth", "JWT or Basic", options =>
    {
        options.ForwardDefaultSelector = ctx =>
        {
            var auth = ctx.Request.Headers.Authorization.FirstOrDefault();
            return auth?.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) == true
                ? JwtBearerDefaults.AuthenticationScheme
                : BasicAuthConstants.Scheme;
        };
    })
    .AddScheme<ManualJwtAuthOptions, ManualJwtAuthHandler>(JwtBearerDefaults.AuthenticationScheme, options =>
    {
        options.Secret           = jwtSecret;
        options.ValidIssuer      = builder.Configuration["Auth:Jwt:Issuer"]   ?? "tobiso";
        options.ValidAudience    = builder.Configuration["Auth:Jwt:Audience"] ?? "tobiso";
        options.ValidateIssuer   = true;
        options.ValidateAudience = true;
        options.ValidateLifetime = true;
    })
    .AddScheme<AuthenticationSchemeOptions, BasicAuthHandler>(BasicAuthConstants.Scheme, null);

services.AddAuthorization();

// Add Blazor authentication
services.AddCascadingAuthenticationState();
services.AddScoped<AuthenticationStateProvider, TokenAuthenticationStateProvider>();

services.AddRazorComponents().AddInteractiveServerComponents();
services.AddScoped<ICategoryService, CategoryService>();
services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter()
        );
    });
services.AddEndpointsApiExplorer();

            services.AddScoped<IPostService, PostService>();
            services.AddScoped<IPostVersionService, PostVersionService>();
            services.AddScoped<IGradeService, GradeService>();
services.AddScoped<IQuestionService, QuestionService>();
services.AddScoped<IExplanationService, ExplanationService>();
services.AddScoped<IEventService, EventService>();
services.AddScoped<IRelatedPostService, RelatedPostService>();
services.AddScoped<IFeedbackService, FeedbackService>();
services.AddScoped<IInteractiveExerciseService, InteractiveExerciseService>();

services.AddSingleton<CredentialStore>();
services.AddTransient<AuthenticationHeaderHandler>();
services.AddTransient<HttpLoggingHandler>();

services
    .AddRefitClient<ITobisoWebApi>()
    .ConfigureHttpClient(c =>
    {
        var baseAddress = builder.Configuration["Api:BaseAddress"];
        if (string.IsNullOrEmpty(baseAddress))
        {
            throw new InvalidOperationException("API base address is not configured.");
        }
        c.BaseAddress = new Uri(baseAddress);
        c.Timeout = TimeSpan.FromMinutes(5); // 5 minut pro upload
    })
    .ConfigurePrimaryHttpMessageHandler(() =>
    {
        var handler = new HttpClientHandler();
        
        // In development, bypass SSL certificate validation
        if (builder.Environment.IsDevelopment())
        {
            handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
        }
        
        return handler;
    })
    .AddHttpMessageHandler<AuthenticationHeaderHandler>()
    .AddHttpMessageHandler<HttpLoggingHandler>();

services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Tobiso API", Version = "v1" });

    options.AddSecurityDefinition(
        "basic",
        new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "basic",
            In = ParameterLocation.Header,
            Description = "Enter your username and password for Basic Authentication",
        }
    );

    options.AddSecurityRequirement(
        new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "basic",
                    },
                },
                Array.Empty<string>()
            },
        }
    );
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
else
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// HTTPS redirection is handled by the reverse proxy in production
if (!app.Environment.IsProduction())
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseRouting();
app.UseAntiforgery();

app.UseAuthentication();
app.UseAuthorization();

// Redirect 401 to login page
app.Use(async (context, next) =>
{
    await next();
    if (context.Response.StatusCode == StatusCodes.Status401Unauthorized
        && !context.Request.Path.StartsWithSegments("/login")
        && !context.Request.Path.StartsWithSegments("/_"))
    {
        context.Response.Redirect("/login");
    }
});

app.MapControllers();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
