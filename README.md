# Tobiso.Web

An educational web platform for learning content management, featuring markdown articles, interactive exercises, PDF generation, and AI-powered assistance.

## Projects

| Project | Description |
|---|---|
| `Tobiso.Web.Api` | Core business logic and services |
| `Tobiso.Web.App` | Blazor WebAssembly frontend |
| `Tobiso.Web.App.Admin` | Admin dashboard (Blazor WebAssembly) |
| `Tobiso.Web.Domain` | Domain entities |
| `Tobiso.Web.Shared` | Shared DTOs and interfaces |
| `Tobiso.Web.Files` | File serving service |

## Tech Stack

- **.NET 9** with Blazor WebAssembly
- **Entity Framework Core 9** + SQL Server
- **Serilog** for logging
- **QuestPDF** for PDF generation
- **MailKit** for email
- **Markdig** for Markdown parsing
- **OpenAI API** for AI assistance

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

## Content Migration

To import HTML content, place HTML files in a directory and run them through `Tobiso.Migrator` to produce Markdown files. Then upload the Markdown files via:

```shell
curl -k -X POST "https://localhost:7270/api/posts/upload-md?directory={directory}" -u {login}:{password}
```

> Note: this upload does not include categories.

## License

MIT License — © 2026 Tobias Heneman