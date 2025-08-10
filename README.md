# Tobiso.Web
This is Tobiso web.

## Tasks

- [x] make a categories controller with service for categories - list categories
- [ ] make post endpoint in PostsController.cs for list of links but anonymous. this version is used to list post to users
- [x] make a clickable tree of categories in CategoriesControler
- [ ] make editing, adding and removing categories like posts
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
