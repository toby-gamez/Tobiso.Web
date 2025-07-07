# Tobiso.Web
This is Tobiso web.

## Migration

Run from the root of your solution (or in Tobiso.Web.App folder):

```
dotnet ef migrations add InitialCreate --project Tobiso.Web.Api --startup-project Tobiso.Web.App --output-dir Infrastructure/Data/Migrations
```

Then apply the migration:

```
dotnet ef database update --project Tobiso.Web.Api --startup-project Tobiso.Web.App
```

## Generate migration script

```
dotnet ef migrations script --project Tobiso.Web.Api --startup-project Tobiso.Web.App --output InitialCreate.sql
```
