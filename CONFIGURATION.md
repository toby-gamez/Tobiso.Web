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

### App (Tobiso.Web.App)
- `PublicBaseUrl`: The site's public URL, used to build the links in verification and password-reset emails. Always taken from config, never from the request, so a forged `Host` header can't redirect those links.

### Email (Tobiso.Web.App)
Required for registration and password reset - new accounts must confirm their email before they can sign in.
- `SmtpHost`, `SmtpPort` (default 587), `SmtpUser`, `SmtpPass`: SMTP server (any provider: Seznam, Gmail app password, Brevo, …)
- `From`: sender address, e.g. `Tobiso <noreply@yourdomain.com>`
- `EnableSsl`: `true` (STARTTLS) unless your server needs otherwise
- In Development, if `SmtpHost` is empty, emails (with their links) are written to the log instead. In any other environment a missing SMTP config makes registration and reset fail with an error rather than logging live links.

### Auth.Jwt
- `Secret`: at least 32 characters; also signs the email links
- `ExpiryHours`: admin session lifetime (keep it short, e.g. 12). Student sessions last 30 days and can be revoked.

### Logging & Serilog
- Standard ASP.NET Core logging configuration
- File logging with daily rotation