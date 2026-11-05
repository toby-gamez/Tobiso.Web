# Tobiso.Web

A Czech educational platform (math and science) featuring markdown articles with grade-specific versions, interactive exercises, student accounts, PDF generation, and AI-powered assistance.

## Projects

| Project | Description |
|---|---|
| `Tobiso.Web.Api` | Business logic services and EF Core data access (library) |
| `Tobiso.Web.App` | Main host: REST API and Blazor Server public frontend |
| `Tobiso.Web.App.Admin` | Admin dashboard (Blazor Server, Bootstrap) |
| `Tobiso.Web.Domain` | EF Core entities |
| `Tobiso.Web.Shared` | Shared DTOs and Refit interfaces |
| `Tobiso.Web.Files` | Static file hosting for images |
| `Tobiso.Web.Tests` | Automated tests |

## Tech Stack

- **.NET 10** with Blazor Server
- **Entity Framework Core 10** + SQL Server
- **Refit** for typed API clients
- **Serilog** for logging
- **QuestPDF** and **PuppeteerSharp** for PDF generation
- **Markdig** for Markdown parsing
- **OpenAI-compatible API** for AI assistance

## Getting Started

1. Copy the `appsettings*.template.json` files and fill in the placeholders — see [CONFIGURATION.md](CONFIGURATION.md).
2. Apply database migrations (below).
3. Run the apps:

```shell
# Public frontend + API (https://localhost:7270)
dotnet run --project Tobiso.Web.App

# Admin dashboard (https://localhost:7271)
dotnet run --project Tobiso.Web.App.Admin
```

Swagger UI is available at `/swagger` in Development.

## Documentation

- [ARCHITECTURE.md](ARCHITECTURE.md) — system architecture
- [CONFIGURATION.md](CONFIGURATION.md) — configuration setup
- [docs/design/README.md](docs/design/README.md) — Organic design system spec
- [docs/SECURITY_COMPLIANCE_AUDIT_2026-11.md](docs/SECURITY_COMPLIANCE_AUDIT_2026-11.md) — security and compliance audit

## Database Migrations

Add a migration:

```shell
dotnet ef migrations add <Name> --project Tobiso.Web.Api --startup-project Tobiso.Web.App --output-dir Infrastructure/Data/Migrations
```

Apply migrations:

```shell
dotnet ef database update --project Tobiso.Web.Api --startup-project Tobiso.Web.App
```

Generate a SQL script:

```shell
dotnet ef migrations script --project Tobiso.Web.Api --startup-project Tobiso.Web.App --output InitialCreate.sql
```

## Content Import

Markdown files can be uploaded via:

```shell
curl -k -X POST "https://localhost:7270/api/posts/upload-md?directory={directory}" -u {login}:{password}
```

> Note: this upload does not include categories.

## License

MIT License — © 2026 Tobias Heneman