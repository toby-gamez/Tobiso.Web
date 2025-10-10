# Configuration Setup

This project uses template files for configuration to avoid committing sensitive data to Git.

## Initial Setup

1. Copy the template files to create your local configuration:

```bash
# For Tobiso.Web.App
cp Tobiso.Web.App/appsettings.template.json Tobiso.Web.App/appsettings.json
cp Tobiso.Web.App/appsettings.Development.template.json Tobiso.Web.App/appsettings.Development.json

# For Tobiso.Web.App.Admin
cp Tobiso.Web.App.Admin/appsettings.template.json Tobiso.Web.App.Admin/appsettings.json
cp Tobiso.Web.App.Admin/appsettings.Development.template.json Tobiso.Web.App.Admin/appsettings.Development.json
```

2. Update the configuration values in your copied files:

Replace the following placeholders with your actual values:

- `YOUR_SERVER` - Database server address
- `YOUR_USER` - Database username
- `YOUR_PASSWORD` - Database password
- `YOUR_DATABASE` - Database name
- `YOUR_USERNAME` - Basic auth username
- `YOUR_USER_ID` - User ID (GUID format)
- `https://www.yourdomain.com` - Your production domain
- Log file paths as needed

## Important Notes

- The actual `appsettings.json` and `appsettings.Development.json` files are ignored by Git
- Never commit files containing real passwords or connection strings
- Always use the template files as a reference for the expected structure
- Template files should be kept up to date when configuration structure changes

## Configuration Structure

### ConnectionStrings
- `DefaultConnection`: Entity Framework database connection string

### Auth.Basic
- `Username`: Basic authentication username
- `Password`: Basic authentication password  
- `UserId`: User identifier (GUID)

### Api
- `BaseAddress`: Base URL for API calls

### Logging & Serilog
- Standard ASP.NET Core logging configuration
- File logging with daily rotation