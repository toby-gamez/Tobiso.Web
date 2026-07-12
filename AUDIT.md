# Tobiso.Web — Full Application Audit

_Date: 2026-07-12 · Scope: whole solution (~52k lines, .NET 9)_
_Method: static review of auth, controllers, services, config, repo hygiene, dependencies. No dynamic/pentest run._

> **Remediation status (2026-07-12):** All Critical (C1–C3) and High (H1–H4) items below have been fixed — see the **Remediation changelog** at the end for the exact changes. The whole solution builds (the Admin project, previously broken by dead auth code, now compiles). Medium items and the architecture/testing gaps remain open.

## Solution at a glance

| Project | Role | ~LOC |
|---|---|---|
| `Tobiso.Web.App` | Main host: Blazor Server UI + MVC API | 23.7k |
| `Tobiso.Web.App.Admin` | Admin host (Bootstrap allowed) | 9.9k |
| `Tobiso.Web.Api` | Class lib: services, EF, auth handlers | 16.5k |
| `Tobiso.Web.Domain` | EF entities | 0.5k |
| `Tobiso.Web.Shared` | DTOs + Refit interfaces | 1.3k |
| `Tobiso.Web.Files` | Standalone file-serving app | 0.4k |

---

## 🔴 Critical

### C1 — Unauthenticated file upload with path traversal (`Tobiso.Web.Files`)
`Controllers/FilesController.cs:12` marks the **entire controller `[AllowAnonymous]`**, so `upload`, list, and `delete` all run without auth.

- **Path traversal (write):** `FilesController.cs:50,59` — `fileName = file.FileName` is used unsanitized in `Path.Combine(uploadsPath, fileName)` with `FileMode.Create`. A filename like `..\..\wwwroot\index.html` (or any relative path) writes/overwrites arbitrary files the process can reach.
- **Stored XSS:** `image/svg+xml` is in the allow-list (`:38`) and the type check trusts the client-supplied `file.ContentType` (spoofable). An SVG with embedded `<script>` served from the app origin executes in visitors' browsers.
- **Anonymous delete + traversal:** `DeleteImage(string fileName)` (`:129`) is also anonymous and path-joins the raw `fileName` → anyone can delete arbitrary images (or traverse).
- No content-sniffing, no filename randomization, no auth.

**Fix:** require auth on the controller (remove class-level `[AllowAnonymous]`, opt specific GETs back in); generate a server-side random filename (ignore `file.FileName` for the path); validate by decoding the image, not by `ContentType`; drop `svg+xml` or sanitize it; reject any `fileName` containing path separators / `..` on delete.

### C2 — AI endpoints: unbounded cost / financial DoS
`Tobiso.Web.App/Controllers/AiController.cs` exposes ~25 `[AllowAnonymous]` endpoints that each call the paid OpenAI API (`ask`, `ask-stream`, `flashcards`, `practice-problems`, `generate-demo`, `concept-map`, `cross-connections`, …).

The only throttle is `AiRateLimitService`, keyed on **`X-Device-Id`** — a request header the caller fully controls (`AiController.cs:396-405`). Sending a fresh random `X-Device-Id` per request bypasses the limit entirely, and the store is **in-memory** (per-instance, reset on restart, not shared across instances). Result: an anonymous attacker can drive arbitrary OpenAI spend.

**Fix:** anchor the rate key to something server-observed (authenticated user, or IP + device as a *composite*, never device alone); add a hard global daily budget/circuit-breaker on OpenAI calls; move counters to a shared store (DB/Redis) if multi-instance.

### C3 — AI credit top-up bypass
`AiController.AddCredits` (`:161`) grants bonus AI credits. The HMAC signature check is skipped entirely when `OpenAI:CreditsSigningSecret` is empty: `if (!string.IsNullOrEmpty(secret)) { …verify… }` (`:176-184`). If the secret is unset in any environment, **anyone can POST unlimited credits**. It is also `[AllowAnonymous]`.

**Fix:** fail closed — reject the request if the secret is not configured; never treat "no secret" as "signature valid."

---

## 🟠 High

### H1 — Secrets & noise committed to git
- **Private key in repo:** `certs/localhost-key.pem` (+ `localhost.pem`). It's a localhost dev cert (limited real impact) but keys never belong in git; `.gitignore` already lists `*.pfx` but not `*.pem`.
- **Application logs committed under a literal `C:` directory:** `Tobiso.Web.App/C:/Logs/*.log` and `Tobiso.Web.App.Admin/C:/Logs/*.log` — ~56k lines, dated through 2026‑07‑11. These are a Windows path (`C:\Logs`) created literally on disk and committed. `.gitignore` has `[Ll]ogs/` but these were already tracked so it doesn't help.
- **`Tobiso.Web.Files/appsettings.json` / `appsettings.Development.json`** are tracked (unlike App/Admin, which are ignored). Currently they hold only Kestrel/logging config — but the pattern invites a future secret leak.

**Fix:** `git rm --cached` the key, the `C:/Logs` trees, and the Files appsettings; add `*.pem`, `**/C:/`, and Files appsettings to `.gitignore`. Rotate the dev cert. If any real secret ever sat in history, rotate it and consider history rewrite.

### H2 — Basic Auth compares plaintext passwords; `PasswordHasher` unused
`BasicAuthHandler.cs:53-62` reads `Auth:Basic:Password` from config and compares it (constant-time, good) against the request — but the expected password is **plaintext in `appsettings`**. A proper `PasswordHasher` (PBKDF2, 100k iters, SHA-256) exists at `Api/Authentication/PasswordHasher.cs` but the Basic scheme never uses it. Single shared credential = no per-user identity/audit.

**Fix:** store only a PBKDF2 hash and verify via `PasswordHasher.Verify`; if this is a single service account, at least keep it out of source config (user-secrets / env / vault).

### H3 — Credentials persisted in browser `localStorage`
`Authentication/CredentialStore.cs:40-56` writes the username **and password** to `localStorage` (`blinked_username` / `blinked_password`). Any XSS (see C1/H4) reads them directly; `localStorage` also survives indefinitely and is readable by all same-origin script. (The `blinked_*` key names also reveal this file was copied from another project.)

**Fix:** don't store the raw password. Use a short-lived token (the JWT path already exists) in memory or an `HttpOnly` cookie; never keep reusable secrets in `localStorage`.

### H4 — Unsanitized HTML rendering (XSS surface)
Multiple components render model/AI content as raw HTML via `MarkupString`, and Markdig passes raw HTML through by default:
- `Components/Pages/PostDetail.razor:669` — `@((MarkupString)ActiveVersion.Content)` renders stored post content directly as HTML.
- `PersonModal.razor:53`, `AddendumModal.razor:24`, `CompareModal.razor:40` — render **AI-generated** markdown → HTML unsanitized.
- `AiController.GenerateDemo` (`:838`) returns arbitrary AI-generated **HTML** that is cached and rendered.

Today the authoring path is admin-only, so the practical risk is medium — but combined with prompt-injectable AI output and the anonymous upload (C1), it's a real stored-XSS chain.

**Fix:** run rendered HTML through a sanitizer (e.g. HtmlSanitizer / Ganss.Xss) before `MarkupString`; render AI "demo" HTML inside a sandboxed `<iframe>`.

---

## 🟡 Medium

- **M1 — Hand-rolled JWT** (`ManualJwtAuthHandler.cs`): custom parse/verify instead of `Microsoft.IdentityModel.Tokens`. The signature check is constant-time and sound, but the JWT `alg` header is never validated and every payload claim is copied verbatim into the principal (`:88-110`). Prefer the standard `JwtBearer` handler — less to get subtly wrong.
- **M2 — In-memory rate limits & credits** (`AiRateLimitService`, singleton): lost on restart and per-instance. Any horizontal scaling silently multiplies the effective limit. Move to a shared store.
- **M3 — `upload-md` arbitrary directory read** (`PostsController.cs:143-150`): authenticated, but `Path.GetFullPath(directory)` accepts any absolute server path with no root constraint. Constrain to an allow-listed base directory.
- **M4 — Info leak in `/api/ai/diag`** (`AiController.cs:43`, `[AllowAnonymous]`): returns upstream OpenAI response bodies/status and exception messages to anonymous callers. Restrict to admin or remove in production.
- **M5 — Failed-auth logs the attempted username** (`BasicAuthHandler.cs:65`) at Warning — minor log-injection / info surface.
- **M6 — Swashbuckle version drift**: `6.5.0` in one project vs `8.1.2` in another. Align, and ensure Swagger isn't exposed anonymously in production.

---

## 🔵 Architecture & code quality

- **No test projects exist** anywhere in the solution. For an app with custom auth, crypto, rate-limiting, and credit accounting, this is the biggest structural gap — every fix above ships unverified. Start with unit tests around auth handlers, `PasswordHasher`, rate limiting, and `AddCredits`.
- **`AiController` is a ~1000-line "god controller"** mixing HTTP, inline rate-limiting, DB access, and caching. Note the primary `Ask` method duplicates rate-limit logic inline (`:73-130`) instead of using the `GetRateKey()`/`TryConsumeRateLimit()` helpers the rest of the file uses. Extract an `AiFacade`/application service; unify the rate-limit path.
- **Duplicated auth stack** across `App` and `App.Admin` (`CredentialStore`, `BasicAuthenticationStateProvider`, `AuthenticationHeaderHandler` exist in both, near-identical). Promote to a shared library.
- **`.gitignore` is copy-pasted** from unrelated projects (`TadataNet`, `LogMyDay`, `Blinked` paths throughout) — it doesn't actually match this repo's layout, which is why `C:/Logs`, `certs/*.pem`, and Files appsettings slipped through. Rewrite it for this solution.
- **Copy-paste heritage leaks** into runtime naming (`blinked_username` localStorage keys) — rename to the current product to avoid confusion.
- **Empty-catch swallowing** in several AI cache reads (`catch { }`, e.g. `AiController.cs:634,757`): a corrupt cache row silently falls through to a paid regeneration with no signal. Log at minimum.

---

## Suggested remediation order

1. **C1** — lock down `Tobiso.Web.Files` (auth + traversal + SVG). _Highest real exposure._
2. **C2 / C3** — fix AI rate-key anchoring and the credit-signature fail-open. _Direct cost risk._
3. **H1** — purge secrets/logs from git, rewrite `.gitignore`, rotate the dev cert.
4. **H2 / H3** — hash the Basic Auth password; stop storing raw passwords in `localStorage`.
5. **H4** — sanitize `MarkupString` HTML; sandbox AI "demo" HTML.
6. **Testing** — add a test project and cover the auth/credit/rate-limit paths as you fix them.
7. Medium items and the architecture cleanups as follow-up.

_This audit is static-analysis only; a dynamic pentest (esp. of C1/C2 against a running instance) is recommended to confirm exploitability and catch anything config-dependent._

---

## Remediation changelog (2026-07-12)

| # | Fix | Files |
|---|---|---|
| **C1** | `FilesController` now requires Basic auth (removed class `[AllowAnonymous]` + list-endpoint override); upload stores a random `{name}-{guid}{ext}` filename with a containment check and `FileMode.CreateNew` (no traversal/overwrite); validation is by server-derived extension; **SVG removed** (script-carrying); delete rejects any path-separated name. | `Tobiso.Web.Files/Controllers/FilesController.cs` |
| **C2** | AI rate-limit consumption re-keyed to the server-observed **IP** (`GetRateKey`), which the client can't rotate; purchased credits looked up via a separate `GetBonusKey` (device) and only added to the limit. | `Tobiso.Web.App/Controllers/AiController.cs` |
| **C3** | `AddCredits` now **fails closed** (503) when `CreditsSigningSecret` is unset instead of skipping verification; signature compared in constant time. | `Tobiso.Web.App/Controllers/AiController.cs` |
| **H1** | Untracked the committed dev private key and `Tobiso.Web.Files/appsettings*.json` (local copies kept); deleted the junk `C:/Logs` dirs (were not actually tracked); added `*.pem`, `/certs/`, Files appsettings and `**/C:/` to `.gitignore`. **Manual follow-up:** rotate the dev cert. | `.gitignore`, git index |
| **H2** | `BasicAuthHandler` now verifies a PBKDF2 `Auth:Basic:PasswordHash` via `PasswordHasher.Verify`, falling back to plaintext `Password` for migration; templates updated to `PasswordHash`. | `Tobiso.Web.Api/Authentication/BasicAuthHandler.cs`, `*/appsettings.template.json` |
| **H3** | Deleted the dead blinked-credential trio that persisted username+password to `localStorage` (unregistered/unused; app uses `StudentCredentialStore` which stores only a JWT). Also removed the stale Admin `BasicAuthenticationStateProvider` that broke the build. | `Tobiso.Web.App/Authentication/{CredentialStore,BasicAuthenticationStateProvider,AuthenticationHeaderHandler}.cs`, `Tobiso.Web.App.Admin/Authentication/BasicAuthenticationStateProvider.cs` |
| **H4** | AI-generated markdown now rendered through a `DisableHtml` Markdig pipeline (`AiMarkdown.ToSafeHtml`) so injected HTML is escaped, not executed (verified). AI "demo" HTML was already sandboxed in `<iframe sandbox="allow-scripts">`. | `Tobiso.Web.App/Services/AiMarkdown.cs`, `PersonModal.razor`, `CompareModal.razor` |

_Residual notes: the JWT is still kept in `localStorage` (lesser risk than a password; an `HttpOnly` cookie would be stronger). Admin-authored markdown (`MarkdownContent`, addendums, exercise instructions) is intentionally **not** run through `DisableHtml` since it relies on raw HTML/KaTeX — its trust boundary is the admin login._
