# Copilot Instructions for Tobiso.Web

## Project Architecture
- **Solution Structure:**
  - `Tobiso.Web.Api`: ASP.NET Core Web API for backend logic, authentication, and data access.
  - `Tobiso.Web.App`: Blazor WebAssembly frontend for user-facing web app.
  - `Tobiso.Web.App.Admin`: Blazor WebAssembly admin interfaceC:\Users\calor\Source\Repos\Tobiso.Web\Tobiso.Web.sln.
  - `Tobiso.Web.Domain`: Domain models and entities.
  - `Tobiso.Web.Shared`: Shared DTOs and interfaces for API/frontend communication.

## Key Patterns & Conventions
- **Authentication:**
  - Uses custom Basic Authentication (`Authentication/BasicAuthHandler.cs` in API, `BasicAuthenticationStateProvider.cs` in App/Admin).
  - Credentials are managed via `CredentialStore.cs`.
- **Controllers:**
  - API endpoints are in `Controllers/` (e.g., `PostsController.cs`).
  - Anonymous endpoints for public data (see README for example).
- **Services:**
  - Business logic is in `Services/` (e.g., `PostService.cs`, `CategoryService.cs`).
- **Data Access:**
  - Entity Framework Core migrations and updates are managed via CLI (see below).
  - Migrations are stored in `Infrastructure/Data/Migrations`.
- **Frontend:**
  - Blazor components in `Components/`, routes in `Routes.razor`.
  - Shared logic between App and Admin in `Shared/`.

## Developer Workflows
- **Database Migrations:**
  - Add migration:
    ```shell
    dotnet ef migrations add <Name> --project Tobiso.Web.Api --startup-project Tobiso.Web.App --output-dir Infrastructure/Data/Migrations
    ```
  - Update database:
    ```shell
    dotnet ef database update --project Tobiso.Web.Api --startup-project Tobiso.Web.App
    ```
  - Generate SQL script:
    ```shell
    dotnet ef migrations script --project Tobiso.Web.Api --startup-project Tobiso.Web.App --output InitialCreate.sql
    ```
- **Content Migration:**
  - Use Tobiso.Migrator to convert HTML files to Markdown, then run CLI command with login, password, and directory path (see README for details).

## Integration Points
- **Shared DTOs/Interfaces:**
  - All cross-project communication uses DTOs/interfaces from `Tobiso.Web.Shared`.
- **AppSettings:**
  - Configuration via `appsettings.json` in each project.

## Project-Specific Notes
- **Anonymous Post Listing:**
  - `PostsController.cs` has a public endpoint for listing posts to users (see README checklist).
- **Custom Logging:**
  - HTTP logging via `Handlers/HttpLoggingHandler.cs` in App/Admin.

## Examples
- To add a new API endpoint, create a method in the relevant controller and expose DTOs from `Shared/DTOs`.
- To add a new Blazor page, add a `.razor` file in `Components/Pages/` and update `Routes.razor`.

---
For further details, see the README.md and referenced source files. If any section is unclear or missing, please request clarification or provide feedback for improvement.
