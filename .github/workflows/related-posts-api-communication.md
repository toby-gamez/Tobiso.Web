# RelatedPosts API Komunikace - Architektura a Propojení

## Přehled architektury

Projekt Tobiso.Web implementuje systém souvisejících článků (RelatedPosts) pomocí třívrstvé architektury s jasně oddělenými komponenty pro API, frontend a sdílené objekty.

## Klíčové komponenty

### 1. Shared Layer (`Tobiso.Web.Shared`)

#### DTOs (Data Transfer Objects)
- **`RelatedPostResponse`** - hlavní DTO pro přenos dat o souvisejícím článku
- **`CreateRelatedPostRequest`** - DTO pro vytváření nových souvislostí
- **`UpdateRelatedPostRequest`** - DTO pro aktualizaci existujících souvislostí

```csharp
public class RelatedPostResponse
{
    public int Id { get; set; }
    public int PostId { get; set; }
    public int RelatedPostId { get; set; }
    public string? Text { get; set; }
    
    // Navigační properties pro UI
    public string? PostTitle { get; set; }
    public string? RelatedPostTitle { get; set; }
}
```

#### Interfaces
- **`ITobisoWebApi`** - kompletní Refit interface pro autentizované API volání
- **`ITobisoAnonymApi`** - interface pro anonymní přístup k veřejným souvislostem

### 2. Domain Layer (`Tobiso.Web.Domain`)

#### Entity
**`RelatedPost`** - doménová entita reprezentující vztah mezi články
```csharp
public class RelatedPost
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public int PostId { get; set; }
    
    [ForeignKey(nameof(PostId))]
    public Post? Post { get; set; }
    
    [Required]  
    public int RelatedPostId { get; set; }
    
    [ForeignKey(nameof(RelatedPostId))]
    public Post? RelatedPostRef { get; set; }
    
    public string? Text { get; set; }
}
```

### 3. API Layer (`Tobiso.Web.Api`)

#### Controllers
Každý projekt má vlastní RelatedPostsController s různými úrovněmi autentizace:

**`Tobiso.Web.Api/Controllers/RelatedPostsController.cs`**
```csharp
[Authorize(AuthenticationSchemes = BasicAuthConstants.Scheme)]
[Route("api/[controller]")]
[ApiController]
public class RelatedPostsController : ControllerBase
{
    private readonly IRelatedPostService _relatedPostService;
    // Všechny endpointy vyžadují autentizaci
}
```

**`Tobiso.Web.App/Controllers/RelatedPostsController.cs`**
```csharp
[Authorize(AuthenticationSchemes = BasicAuthConstants.Scheme)]
[Route("api/[controller]")]
[ApiController]
public class RelatedPostsController : ControllerBase
{
    [AllowAnonymous] // Veřejný přístup ke čtení
    [HttpGet]
    public async Task<ActionResult<IList<RelatedPostResponse>>> GetAllRelatedPosts()
    
    [AllowAnonymous]
    [HttpGet("by-post/{postId}")]
    public async Task<ActionResult<IList<RelatedPostResponse>>> GetRelatedPostsByPostId(int postId)
    
    [HttpPost] // Vyžaduje autentizaci pro zápis
    public async Task<IActionResult> CreateRelatedPost()
}
```

**`Tobiso.Web.App.Admin/Controllers/RelatedPostsController.cs`**
```csharp
// Kompletní CRUD operace s plnou autentizací pro admin rozhraní
```

#### Services
**`RelatedPostService`** implementuje `IRelatedPostService` a poskytuje:
- `GetAll()` - načte všechny související články
- `GetByPostId(int postId)` - načte související články pro konkrétní článek  
- `GetById(int id)` - načte konkrétní souvislost podle ID
- `Create(CreateRelatedPostRequest request)` - vytvoří novou souvislost (s možností bidirectionality)
- `Update(int id, UpdateRelatedPostRequest request)` - aktualizuje existující souvislost
- `Delete(int id)` - smaže souvislost

```csharp
public class RelatedPostService : IRelatedPostService
{
    private readonly TobisoDbContext _context;
    
    public RelatedPostService(TobisoDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }
}
```

#### Speciální funkcionality
- **Bidirectional connections** - možnost vytvořit obousměrné souvislosti
- **Validation** - kontrola, že článek neodkazuje na sebe sama
- **Duplicate prevention** - zabránění vytvoření duplicitních souvislostí

### 4. Frontend Layer

#### Blazor komponenty
**`RelatedPosts.razor`** v Admin aplikaci:
```razor
@page "/related-posts"
@using Tobiso.Web.Shared.DTOs
@using Tobiso.Web.Shared.Interfaces
@inject ITobisoWebApi Api
@inject NavigationManager Navigation

@code {
    private IList<RelatedPostResponse>? relatedPosts;
    private string? errorMessage;

    protected override async Task OnInitializedAsync()
    {
        relatedPosts = await Api.GetAllRelatedPosts(); // Refit volání
    }
}
```

## Dependency Injection konfigurace

### App (`Tobiso.Web.App/Program.cs`)
```csharp
// API Services (přímá registrace pro interní použití)
services.AddScoped<Tobiso.Web.Api.Services.IRelatedPostService, Tobiso.Web.Api.Services.RelatedPostService>();

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
services.AddScoped<IRelatedPostService, RelatedPostService>();

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
- **AllowAnonymous**: Veřejné čtení souvislostí (GET operace)
- **Authorize**: Vyžaduje autentizaci (POST, PUT, DELETE operace)

## Komunikační vzory

### 1. Přímé volání služeb (intra-process)
```csharp
// V rámci stejného procesu - přímé DI
public class RelatedPostsController : ControllerBase
{
    private readonly IRelatedPostService _relatedPostService;
    
    public RelatedPostsController(IRelatedPostService relatedPostService)
    {
        _relatedPostService = relatedPostService;
    }
}
```

### 2. HTTP komunikace přes Refit
```csharp
// Mezi procesy - HTTP API volání
@inject ITobisoWebApi Api

private async Task LoadData()
{
    var relatedPosts = await Api.GetAllRelatedPosts(); // HTTP GET /api/RelatedPosts
    var byPostId = await Api.GetRelatedPostsByPostId(1); // HTTP GET /api/RelatedPosts/by-post/1
}
```

## Datový model a vztahy

### Database Schema
```sql
CREATE TABLE RelatedPosts (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    PostId INT NOT NULL,
    RelatedPostId INT NOT NULL,
    Text NVARCHAR(500),
    FOREIGN KEY (PostId) REFERENCES Posts(Id),
    FOREIGN KEY (RelatedPostId) REFERENCES Posts(Id)
);
```

### Business Logic
- **Relationship Direction**: Souvislosti mohou být jednosměrné nebo obousměrné
- **Self-Reference Prevention**: Článek nemůže odkazovat sám na sebe
- **Duplicate Prevention**: Stejná souvislost nemůže existovat vícekrát
- **Cascade Operations**: Při smazání článku se automaticky mažou i jeho souvislosti

## Deployment architektura

### Projekty a jejich role:
- **`Tobiso.Web.Api`**: Samostatná API služba s plnou autentizací pro RelatedPosts
- **`Tobiso.Web.App`**: Veřejná aplikace s anonymním přístupem ke čtení souvislostí
- **`Tobiso.Web.App.Admin`**: Admin rozhraní s plnými CRUD operacemi
- **`Tobiso.Web.Domain`**: RelatedPost entity a business rules
- **`Tobiso.Web.Shared`**: DTOs a interfacy pro RelatedPosts API

### Datový tok pro RelatedPosts:
```
Database ←→ RelatedPostService ←→ RelatedPostsController ←→ Refit/HTTP ←→ Blazor Components
```

## API Endpoints

### Veřejné endpointy (anonymní přístup):
- `GET /api/RelatedPosts` - získat všechny souvislosti
- `GET /api/RelatedPosts/by-post/{postId}` - získat souvislosti pro konkrétní článek
- `GET /api/RelatedPosts/{id}` - získat konkrétní souvislost

### Chráněné endpointy (vyžadují autentizaci):
- `POST /api/RelatedPosts` - vytvořit novou souvislost
- `PUT /api/RelatedPosts/{id}` - aktualizovat existující souvislost
- `DELETE /api/RelatedPosts/{id}` - smazat souvislost

## Klíčové soubory a jejich role

| Soubor | Role | Projekt |
|--------|------|---------|
| `ITobisoWebApi.cs` | Refit interface pro autentizované RelatedPosts volání | Shared |
| `ITobisoAnonymApi.cs` | Refit interface pro anonymní RelatedPosts volání | Shared |
| `RelatedPostResponse.cs` | Hlavní DTO pro přenos dat o souvislostech | Shared |
| `CreateRelatedPostRequest.cs` | DTO pro vytváření nových souvislostí | Shared |
| `UpdateRelatedPostRequest.cs` | DTO pro aktualizaci souvislostí | Shared |
| `RelatedPost.cs` | Doménová entita pro database mapping | Domain |
| `RelatedPostService.cs` | Business logika pro správu souvislostí | Api |
| `RelatedPostsController.cs` | HTTP endpoints pro RelatedPosts API | Api/App/Admin |
| `RelatedPosts.razor` | UI komponenta pro správu souvislostí | Admin |
| `AddRelatedPostsTable.cs` | EF Migration pro vytvoření tabulky | Api |

## Vývojové poznámky

- Každý projekt (Api, App, Admin) má vlastní RelatedPostsController s různými security policies
- App projekt umožňuje anonymní čtení souvislostí pro veřejnost
- Admin projekt má plné CRUD operace s autentizací
- RelatedPostService implementuje pokročilou business logiku (bidirectional connections, validations)
- Refit se používá pro HTTP komunikaci mezi frontend komponentami a API
- Database constraints zajišťují referenční integritu mezi články a souvislostmi
- Migration systém umožňuje verzování database schema změn
- Basic Authentication je konzistentní napříč všemi RelatedPosts operacemi