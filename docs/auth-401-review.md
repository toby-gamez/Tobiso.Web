# Code Review: Production 401 on Auth-Required API Calls

## Architecture

- **`Tobiso.Web.App`** (`www.tobiso.com`) — public Blazor Server site, hosts all API controllers (PostsController, CategoriesController, etc.)
- **`Tobiso.Web.App.Admin`** (`admin.tobiso.com`) — admin Blazor Server panel, calls the main app via Refit (`ITobisoWebApi`)
- Custom JWT auth (`ManualJwtAuthHandler`) + custom Basic auth (`BasicAuthHandler`), selected by a `SmartAuth` policy scheme based on `Authorization` header prefix
- `Tobiso.Web.Files` — separate file service (Basic auth only, not relevant)

## Authentication Flow

```
Login.razor
  │ POST https://www.tobiso.com/api/auth/login
  │ (raw HttpClient)
  ▼
AuthController.Login()
  │ JwtTokenService.GenerateToken() — HMAC-SHA256, claims: sub, unique_name, name, exp, iss, aud
  │ Secret from config: Auth:Jwt:Secret
  ▼
JWT returned → CredentialStore.SetAsync(token, JSRuntime)
  │ stores in: _token (in-memory, Singleton) + localStorage ("tobiso_jwt")
  │ NotifyAuthenticationStateChanged()
  ▼
Subsequent Refit calls via ITobisoWebApi
  │ AuthenticationHeaderHandler.SendAsync()
  │   → _credentialStore.GetToken() → attaches Authorization: Bearer <token>
  ▼
Main app receives request
  │ SmartAuth policy → "Bearer" prefix → ManualJwtAuthHandler
  │   → ValidateToken(): HMAC verify, iss/aud/exp checks
  │   → AuthenticateResult.Success() → [Authorize] passes
  ▼
Controller executes
```

## Root Cause

**`CredentialStore._token` lives only in server memory (Singleton).** After any app pool recycle (common on shared hosting like `databaseasp.net` free tier, can happen every few hours), `_token` is null. The JWT still exists in `localStorage` on the browser, but:

1. The method `CredentialStore.InitializeAsync(IJSRuntime)` exists to restore it from `localStorage`
2. **It's never called at the right time** — the existing code never invokes it after the SignalR circuit connects

### Why `OnInitializedAsync` Doesn't Work

In Blazor Server InteractiveServer rendering mode:

| Phase | `OnInitializedAsync` runs? | `IJSRuntime` available? |
|-------|---------------------------|------------------------|
| Prerendering | Yes (once) | **No** |
| After circuit connects | **No** (same component instance) | Yes |

Any fix placed in `OnInitializedAsync` (across all 3 versions of attempted fix) fails because:
- During prerendering → `IJSRuntime` throws `InvalidOperationException`
- After circuit connects → `OnInitializedAsync` doesn't run again

### The Redirect Loop

```
Server restart → _token = null
  │
User navigates to admin.tobiso.com/posts/add
  │
Prerendering:
  ├─ MainLayout.OnInitializedAsync → InitializeAsync() → FAILS (no IJSRuntime)
  └─ AddPost.OnInitializedAsync → auth check → _token is null → NavigateTo("/login")
  │
Circuit connects → navigation intent to /login executes
  │
Login page loads (full page navigation)
  ├─ Prerendering of login: OnInitializedAsync → same problem → no token restored
  └─ Circuit connects → user sees login page
  │
User is confused — was "logged in" moments ago
  │
If user re-logs in → works (token stored again)
If user navigates to another page → still no token → redirected to login again
```

## Affected Files

| File | Lines | Issue |
|------|-------|-------|
| `Tobiso.Web.App.Admin/Authentication/CredentialStore.cs` | 34-50 | `InitializeAsync()` exists but is never wired to the correct lifecycle event |
| `Tobiso.Web.App.Admin/Authentication/AuthenticationHeaderHandler.cs` | 21 | `GetToken()` returns null → no auth header → 401 |
| `Tobiso.Web.App.Admin/Components/Pages/Login.razor` | 82-91 | `OnInitializedAsync` → `InitializeAsync` fails silently during prerendering |
| `Tobiso.Web.App.Admin/Components/Layout/MainLayout.razor` | 9-35 | Same pattern — `OnInitializedAsync` / `OnAfterRenderAsync` attempts don't help because pages hard-redirect during prerendering |
| `Tobiso.Web.App.Admin/Controllers/PostsController.cs` | 10 | `[Authorize]` on class — all POST/PUT/DELETE require valid JWT |
| `Tobiso.Web.App/Program.cs` | 66 | `ValidateLifetime = true` — token expiry checked against server clock |

## What Would Fix This

### Option A: `OnAfterRenderAsync` in Login.razor (minimal change)

The only lifecycle hook that runs BOTH during prerendering AND after the circuit connects (in interactive mode) is `OnAfterRenderAsync`. It's the correct place to restore the token:

```razor
@code {
    private bool _tokenRestored;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_tokenRestored) return;
        try
        {
            await CredentialStore.InitializeAsync(JSRuntime);
        }
        catch (InvalidOperationException)
        {
            return; // Prerendering — IJSRuntime not available yet
        }

        _tokenRestored = true;

        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        if (authState.User.Identity?.IsAuthenticated == true)
        {
            Navigation.NavigateTo("/");
        }
    }
}
```

Flow after fix:
1. Prerendering → `OnAfterRenderAsync(true)` → `InitializeAsync` fails → `_tokenRestored` stays false → returns
2. Circuit connects → render → `OnAfterRenderAsync(false)` → `InitializeAsync` **succeeds** → token restored from localStorage
3. Auth check → authenticated → `NavigateTo("/")` → user is on home page without re-login

### Option B: Cookie Instead of localStorage (more robust)

Replace `localStorage` with an `HttpOnly` cookie. Cookies are sent with every HTTP request, including the initial prerendering request. A middleware reads the JWT from the cookie on every request and populates `CredentialStore._token` before any component lifecycle runs.

Changes:
1. On login: set `HttpOnly` cookie with the JWT (server-side, in `AuthController.Login`)
2. Middleware in `Program.cs`: read cookie → populate `CredentialStore._token`
3. On logout: clear the cookie
4. Remove all `localStorage` usage

This works during prerendering because cookies are part of the HTTP request headers, not a browser API.

## Impact Assessment

- **Read operations (GET)**: Work — controllers use `[AllowAnonymous]` on all GET endpoints
- **Write operations (POST/PUT/DELETE)**: Fail with 401 after any server restart
- **After manual re-login**: Everything works until next restart
- **Public site (`www.tobiso.com`)**: Unaffected — uses services directly, not Refit calls

## Security Notes (deployment)

| Concern | Detail |
|---------|--------|
| JWT secret placeholder | `appsettings.json` uses `"CHANGE-ME-use-env-var-AUTH__JWT__SECRET-min-32-chars"` — set `AUTH__JWT__SECRET` env var on the server |
| Admin credentials | `admin`/`secret123` in config — set `AUTH__BASIC__PASSWORD` env var |
| OpenAI API key | Hardcoded in `Tobiso.Web.App/appsettings.json:51` — set `OPENAI__APIKEY` env var |
| DB connection string | Contains password in config — set `CONNECTIONSTRINGS__DEFAULTCONNECTION` env var |
| Custom JWT instead of library | The `ManualJwtAuthHandler` reimplements JWT validation manually instead of using `Microsoft.AspNetCore.Authentication.JwtBearer` — higher risk of implementation bugs |
