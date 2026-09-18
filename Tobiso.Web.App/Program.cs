using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Tobiso.Web.Shared.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using QuestPDF;
using QuestPDF.Infrastructure;
using Refit;
using Serilog;
using System.Net;
using System.Text;
using System.Threading.RateLimiting;
using Tobiso.Api.Authentication;
using Tobiso.Api.Infrastructure.Data;
using Tobiso.Web.Api.Services;
using Tobiso.Web.App.Authentication;
using Tobiso.Web.App.Components;
using Tobiso.Web.App.Handlers;
using Tobiso.Web.App.Services;


var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Host.UseSerilog();

QuestPDF.Settings.License = LicenseType.Community;

var services = builder.Services;

// Add services
services.Configure<BasicAuthOptions>(builder.Configuration.GetSection("Auth:Basic"));
builder.Services.AddScoped<IPreferenceService, PreferenceService>();
services.AddHttpContextAccessor();

services.AddDbContext<TobisoDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

// Separate factory so components that can run DB calls concurrently with the rest of a Blazor
// circuit (e.g. AiChatBox's history lookups alongside its host page's own queries) get their own
// short-lived DbContext instead of racing everyone else on the circuit's single scoped instance.
// A plain singleton wrapper (not EF's AddDbContextFactory helper) because that helper's own
// DbContextOptions<TobisoDbContext> registration conflicts with the scoped one AddDbContext above
// already added for the same TContext.
services.AddSingleton<IDbContextFactory<TobisoDbContext>, TobisoDbContextFactory>();

// Add Authentication and Authorization
var jwtSecret = builder.Configuration["Auth:Jwt:Secret"] ?? "";
if (Encoding.UTF8.GetByteCount(jwtSecret) < 32)
    throw new InvalidOperationException(
        $"Auth:Jwt:Secret must be at least 32 characters (got {jwtSecret.Length}). " +
        "Set the AUTH__JWT__SECRET environment variable to a longer value.");

services
    .AddAuthentication("SmartAuth")
    .AddPolicyScheme("SmartAuth", "JWT or Basic", options =>
    {
        options.ForwardDefaultSelector = ctx =>
        {
            var auth = ctx.Request.Headers.Authorization.FirstOrDefault();
            return auth?.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) == true
                ? "Bearer"
                : BasicAuthConstants.Scheme;
        };
    })
    .AddScheme<ManualJwtAuthOptions, ManualJwtAuthHandler>("Bearer", options =>
    {
        options.Secret           = jwtSecret;
        options.ValidIssuer      = builder.Configuration["Auth:Jwt:Issuer"]   ?? "tobiso";
        options.ValidAudience    = builder.Configuration["Auth:Jwt:Audience"] ?? "tobiso";
        options.ValidateIssuer   = true;
        options.ValidateAudience = true;
        options.ValidateLifetime = true;
    })
    .AddScheme<AuthenticationSchemeOptions, BasicAuthHandler>(BasicAuthConstants.Scheme, null)
    .AddCookie("TempCookie", opts => opts.ExpireTimeSpan = TimeSpan.FromMinutes(10));

var googleClientId = builder.Configuration["Google:ClientId"];
var googleClientSecret = builder.Configuration["Google:ClientSecret"];
if (!string.IsNullOrEmpty(googleClientId) && !string.IsNullOrEmpty(googleClientSecret))
{
    services.AddAuthentication()
        .AddGoogle("Google", opts =>
        {
            opts.ClientId     = googleClientId;
            opts.ClientSecret = googleClientSecret;
            opts.CallbackPath = "/signin-google";
            opts.SignInScheme = "TempCookie";
            opts.ClaimActions.MapJsonKey("picture", "picture");
        });
}

services.AddAuthorization();

// Production hosts on IIS with hostingModel=inprocess (see web.config), so IIS terminates
// the connection itself and RemoteIpAddress already reflects the real client IP - there is
// no separate proxy network hop today, and Proxy:KnownProxies/KnownNetworks are left empty
// in production config, so this is a no-op there. It only starts mattering (and needs those
// settings populated with the provider's real edge ranges) if a CDN/load balancer/WAF is
// ever placed in front of IIS - without it in that scenario, RemoteIpAddress would resolve
// to that front door's address for every request, collapsing the per-client AI rate limiting
// (see AiController.GetRateKey) into a single shared bucket. Only proxies explicitly listed
// are trusted to set X-Forwarded-For; with none configured, ASP.NET Core's default of
// trusting only the loopback network still applies, so a public client can't spoof it.
services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    foreach (var proxy in builder.Configuration.GetSection("Proxy:KnownProxies").Get<string[]>() ?? [])
    {
        if (IPAddress.TryParse(proxy, out var ip))
            options.KnownProxies.Add(ip);
    }
    foreach (var network in builder.Configuration.GetSection("Proxy:KnownNetworks").Get<string[]>() ?? [])
    {
        var parts = network.Split('/');
        if (parts.Length == 2 && IPAddress.TryParse(parts[0], out var netIp) && int.TryParse(parts[1], out var prefix))
            options.KnownNetworks.Add(new Microsoft.AspNetCore.HttpOverrides.IPNetwork(netIp, prefix));
    }
});

// Throttle authentication endpoints (login/register) against online brute-forcing.
services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("auth", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(1);
        opt.PermitLimit = 5;
        opt.QueueLimit = 0;
    });
});

services.AddRazorComponents().AddInteractiveServerComponents();
services.AddCascadingAuthenticationState();
services.AddScoped<StudentCredentialStore>();
services.AddScoped<StudentAuthStateProvider>();
services.AddScoped<AuthenticationStateProvider>(p => p.GetRequiredService<StudentAuthStateProvider>());

// Register API services from Tobiso.Web.Api.Services
services.AddScoped<ICategoryService, CategoryService>();
services.AddScoped<IPostService, PostService>();
services.AddScoped<IPostVersionService, PostVersionService>();
services.AddScoped<IGradeService, GradeService>();
services.AddScoped<IQuestionService, QuestionService>();
services.AddScoped<IExplanationService, ExplanationService>();
services.AddScoped<IEventService, EventService>();
services.AddScoped<IRelatedPostService, RelatedPostService>();
services.AddScoped<IAddendumService, AddendumService>();
services.AddScoped<AddendumModalService>();
services.AddScoped<PostsGraphModalService>();
services.AddScoped<PersonModalService>();
services.AddScoped<IPushNotificationService, PushNotificationService>();
services.AddScoped<IDeviceService, DeviceService>();
services.AddScoped<IFeedbackService, FeedbackService>();
services.AddScoped<IInteractiveExerciseService, InteractiveExerciseService>();
// Register PDF service implementation from API assembly so App controllers can use it (pattern used for other services)
services.AddScoped<Tobiso.Web.Api.Services.IPdfService, Tobiso.Web.Api.Services.PdfService>();

// AI chat services
services.AddSingleton<Tobiso.Web.App.Services.IAiRateLimitService, Tobiso.Web.App.Services.AiRateLimitService>();
services.AddScoped<Tobiso.Web.Api.Services.IAnonymousUsageService, Tobiso.Web.Api.Services.AnonymousUsageService>();
services.AddScoped<Tobiso.Web.App.Services.IAiUsageGuard, Tobiso.Web.App.Services.AiUsageGuard>();
services.AddScoped<Tobiso.Web.App.Services.IAiService, Tobiso.Web.App.Services.AiService>();
// Also register shared IAiService so API services can receive it via DI when hosted in the App
services.AddScoped<Tobiso.Web.Shared.Interfaces.IAiService, Tobiso.Web.App.Services.AiService>();
services.AddHttpClient("OpenAI")
    .ConfigurePrimaryHttpMessageHandler(() =>
    {
        var handler = new HttpClientHandler();
        var ignoreInvalid = false;
        try
        {
            var cfgVal = builder.Configuration["OpenAI:IgnoreInvalidCertificates"]; 
            ignoreInvalid = string.Equals(cfgVal, "true", StringComparison.OrdinalIgnoreCase);
        }
        catch { }

        if (builder.Environment.IsDevelopment() || ignoreInvalid)
        {
            handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
            Serilog.Log.Warning("OpenAI HttpClient SSL validation is disabled. Environment={Environment}, OpenAI:IgnoreInvalidCertificates={Ignore}", builder.Environment.EnvironmentName, ignoreInvalid);
        }
        return handler;
    });

services.AddControllers()
    .ConfigureApplicationPartManager(manager =>
    {
        // Odstraň controllery z Tobiso.Web.Api assembly, aby nevznikaly konflikty v Swagger
        var apiAssembly = typeof(Tobiso.Web.Api.Services.ICategoryService).Assembly;
        var partsToRemove = manager.ApplicationParts
            .Where(part => part.Name == apiAssembly.GetName().Name)
            .ToList();
        foreach (var part in partsToRemove)
        {
            manager.ApplicationParts.Remove(part);
        }
    })
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
services.AddEndpointsApiExplorer();

services.AddScoped<JwtTokenService>();
services.AddScoped<IUserService, UserService>();
services.AddScoped<IAiChatHistoryService, AiChatHistoryService>();
services.AddScoped<IUserProgressService, UserProgressService>();
services.AddScoped<IQuestionAttemptService, QuestionAttemptService>();
services.AddTransient<HttpLoggingHandler>();
// Register PDF JS interop service for minimal Blazor-JS PDF calls
services.AddScoped<PdfJsInterop>();

services.AddRefitClient<ITobisoAnonymApi>()
    .ConfigureHttpClient(c =>
    {
        var baseAddress = builder.Configuration["Api:BaseAddress"];
        if (string.IsNullOrEmpty(baseAddress))
        {
            throw new InvalidOperationException("API base address is not configured.");
        }
        c.BaseAddress = new Uri(baseAddress);
    })
    .ConfigurePrimaryHttpMessageHandler(() =>
    {
        var handler = new HttpClientHandler();
        if (builder.Environment.IsDevelopment())
        {
            // Ignore SSL certificate errors in development
            handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
        }
        return handler;
    })
    .AddHttpMessageHandler<HttpLoggingHandler>();

services.AddRefitClient<ITobisoWebApi>()
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
        if (builder.Environment.IsDevelopment())
        {
            // Ignore SSL certificate errors in development
            handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
        }
        return handler;
    })
    .AddHttpMessageHandler<HttpLoggingHandler>();

// PDF API is called via raw HTTP or existing clients - no additional Refit interface registered.

services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Tobiso API",
        Version = "v1"
    });

    options.AddSecurityDefinition("basic", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "basic",
        In = ParameterLocation.Header,
        Description = "Enter your username and password for Basic Authentication"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "basic"
                }
            },
            Array.Empty<string>()
        }
    });
});

var firebaseCreds = builder.Configuration["Auth:Firebase:CredentialsPath"];
if (!string.IsNullOrEmpty(firebaseCreds))
{
    // Resolve relative paths against the app's content root (same folder as appsettings.json)
    var fullPath = Path.IsPathRooted(firebaseCreds)
        ? firebaseCreds
        : Path.Combine(builder.Environment.ContentRootPath, firebaseCreds);
    if (File.Exists(fullPath))
        FirebaseApp.Create(new AppOptions { Credential = GoogleCredential.FromFile(fullPath) });
    else
        Log.Warning("Firebase credentials file not found at {Path} - push notifications disabled", fullPath);
}

var app = builder.Build();

// Must run before anything that inspects the scheme/remote IP (HTTPS redirection, HSTS,
// rate limiting keyed on client IP).
app.UseForwardedHeaders();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

// HTTPS redirection is handled by the reverse proxy in production
if (!app.Environment.IsProduction())
{
    app.UseHttpsRedirection();
}

app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["Content-Security-Policy"] =
        "default-src 'self'; " +
        "script-src 'self' https://cdn.jsdelivr.net 'unsafe-inline'; " +
        "style-src 'self' https://cdn.jsdelivr.net https://fonts.googleapis.com 'unsafe-inline'; " +
        "font-src 'self' https://fonts.gstatic.com; " +
        "img-src 'self' https: data:; " +
        "media-src 'self' https:; " +
        "connect-src 'self'; " +
        "frame-ancestors 'none'";
    await next();
});

app.UseStaticFiles();
app.UseRouting();
// Add Authentication and Authorization middleware
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.UseAntiforgery();
app.MapControllers();

app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Run();
