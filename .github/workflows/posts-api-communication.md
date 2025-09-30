# Posts API Komunikace - Architektura a Propojení

## Přehled architektury

Projekt Tobiso.Web používá třívrstvou architekturu s jasně oddělenými komponenty pro API, frontend a sdílené objekty.

## Klíčové komponenty

### 1. Shared Layer (`Tobiso.Web.Shared`)

#### DTOs (Data Transfer Objects)
- **`PostResponse`** - hlavní DTO pro přenos dat o příspěvku
- **`PostLinkResponse`** - zjednodušená verze pro seznamy příspěvků

#### Interfaces
- **`ITobisoWebApi`** - kompletní Refit interface pro autentizované API volání
- **`ITobisoAnonymApi`** - interface pro anonymní přístup k veřejným datům

### 2. API Layer (`Tobiso.Web.Api`)

#### Controllers
Každý projekt má vlastní PostsController s různými úrovněmi autentizace:

**`Tobiso.Web.App/Controllers/PostsController.cs`**
```csharp
[Authorize(AuthenticationSchemes = BasicAuthConstants.Scheme)]
[Route("api/[controller]")]
[ApiController] 
public class PostsController : ControllerBase
{
    [AllowAnonymous] // Veřejný přístup ke čtení
    [HttpGet]
    public async Task<IActionResult> GetPosts()
    
    [HttpPost] // Vyžaduje autentizaci pro zápis
    public async Task<IActionResult> CreatePost()
}
```

**`Tobiso.Web.App.Admin/Controllers/PostsController.cs`**
```csharp
// Stejná struktura jako App, ale pro admin rozhraní s plnými CRUD operacemi
```

#### Services
**`PostService`** implementuje `IPostService` a poskytuje:
- `GetAll()` - načte všechny příspěvky
- `GetLinks()` - načte pouze základní informace (ID, Title, FilePath)
- `GetById(int id)` - načte konkrétní příspěvek
- `Create(PostResponse post)` - vytvoří nový příspěvek
- `Update(PostResponse post)` - aktualizuje příspěvek
- `Delete(int id)` - smaže příspěvek

```csharp
public class PostService : IPostService
{
    private readonly TobisoDbContext _context;
    
    public PostService(TobisoDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }
}
```

### 3. Frontend Layer

#### Blazor komponenty
**`Posts.razor`** v Admin aplikaci:
```razor
@using Tobiso.Web.Shared.DTOs
@using Markdig
@inject Tobiso.Web.Shared.Interfaces.ITobisoWebApi Api

@code {
    private IList<PostLinkResponse>? postLinks;
    private PostResponse? selectedPost;

    protected override async Task OnInitializedAsync()
    {
        postLinks = await Api.GetPostLinks(); // Refit volání
    }
}
```

## Dependency Injection konfigurace

### App (`Tobiso.Web.App/Program.cs`)
```csharp
// API Services (přímá registrace pro interní použití)
services.AddScoped<Tobiso.Web.Api.Services.IPostService, Tobiso.Web.Api.Services.PostService>();

// Refit client pro anonymní volání
services.AddRefitClient<ITobisoAnonymApi>()
    .ConfigureHttpClient(c => c.BaseAddress = new Uri(baseAddress))
    .AddHttpMessageHandler<HttpLoggingHandler>();

// Database context
services.AddDbContext<TobisoDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
```

### Admin (`Tobiso.Web.App.Admin/Program.cs`)
```csharp
// API Services
services.AddScoped<IPostService, PostService>();

// Autentizace a credentials
services.AddSingleton<CredentialStore>();
services.AddTransient<AuthenticationHeaderHandler>();

// Refit client s autentizací
services.AddRefitClient<ITobisoWebApi>()
    .ConfigureHttpClient(c => c.BaseAddress = new Uri(baseAddress))
    .AddHttpMessageHandler<AuthenticationHeaderHandler>() // Přidává Basic Auth
    .AddHttpMessageHandler<HttpLoggingHandler>();
```

## Autentizace a autorizace

### Basic Authentication
Systém používá vlastní Basic Authentication handler:

1. **`BasicAuthHandler`** - ověřuje credentials v API
2. **`CredentialStore`** - ukládá credentials v localStorage
3. **`AuthenticationHeaderHandler`** - automaticky přidává Basic Auth header k HTTP požadavkům

### Flow autentizace
```
Frontend → CredentialStore → AuthenticationHeaderHandler → API → BasicAuthHandler
```

### Úrovně přístupu
- **AllowAnonymous**: Veřejné čtení dat (GET operace)
- **Authorize**: Vyžaduje autentizaci (POST, PUT, DELETE operace)

## Komunikační vzory

### 1. Přímé volání služeb (intra-process)
```csharp
// V rámci stejného procesu - přímé DI
public class PostsController : ControllerBase
{
    private readonly IPostService _postService;
    
    public PostsController(IPostService postService)
    {
        _postService = postService;
    }
}
```

### 2. HTTP komunikace přes Refit
```csharp
// Mezi procesy - HTTP API volání
@inject ITobisoWebApi Api

private async Task LoadData()
{
    var posts = await Api.GetPostLinks(); // HTTP GET /api/Posts/links
}
```

## Deployment architektura

### Projekty a jejich role:
- **`Tobiso.Web.Api`**: Samostatná API služba (zatím nepoužívána pro posty)
- **`Tobiso.Web.App`**: Hlavní veřejná aplikace s vlastními PostsController
- **`Tobiso.Web.App.Admin`**: Admin rozhraní s PostsController pro správu
- **`Tobiso.Web.Domain`**: Entity models (Post entity)
- **`Tobiso.Web.Shared`**: Sdílené DTOs a interfacy pro posty

### Datový tok pro posty:
```
Database ←→ PostService ←→ PostsController ←→ Refit/HTTP ←→ Blazor Components
```

## Klíčové soubory a jejich role

| Soubor | Role | Projekt |
|--------|------|---------|
| `ITobisoWebApi.cs` | Refit interface pro autentizované volání (Posts endpointy) | Shared |
| `ITobisoAnonymApi.cs` | Refit interface pro anonymní volání (veřejné Posts) | Shared |
| `PostResponse.cs` | Hlavní DTO pro přenos dat o příspěvku | Shared |
| `PostLinkResponse.cs` | Zjednodušené DTO pro seznamy příspěvků | Shared |
| `PostService.cs` | Business logika pro práci s příspěvky | Api |
| `PostsController.cs` | HTTP endpoints pro Posts API | App/Admin |
| `CredentialStore.cs` | Správa autentizačních údajů | App/Admin |
| `AuthenticationHeaderHandler.cs` | HTTP middleware pro auth | App/Admin |
| `Posts.razor` | UI komponenta pro správu příspěvků | Admin |

## Vývojové poznámky

- Každý projekt (App, Admin) má vlastní PostsController - umožňuje různé security policies pro příspěvky
- Refit se používá pouze pro komunikaci mezi frontend komponentami a Posts API
- PostService se registruje přímo přes DI pro lepší výkon při práci s příspěvky
- Anonymous endpointy umožňují veřejný přístup k příspěvkům (čtení)
- Authenticated endpointy pro vytváření, úpravy a mazání příspěvků
- Basic Authentication je implementována custom handlery pro flexibilitu