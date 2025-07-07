# Tobiso.Web - Product Requirements Document (PRD)

## Project Overview

**Project Name:** Tobiso.Web  
**Version:** 1.0.0  
**Technology Stack:** .NET 9, Blazor Server, Entity Framework Core, SQL Server  
**Architecture:** Clean Architecture with Domain-Driven Design  
**Authentication:** Basic HTTP Authentication with JWT-like token storage  

## Executive Summary

Tobiso.Web is a modern web application built with Blazor Server that provides a content management system for posts and categories. The application features a clean, professional interface with secure authentication and follows industry best practices for enterprise-grade applications.

## Project Structure

```
Tobiso.Web/
├── PRD.md                          # This documentation
├── README.md                       # Project readme
├── Tobiso.Web.sln                 # Solution file
├── Tobiso.Web.Api/                # API Layer (Controllers & Services)
│   ├── Authentication/            # Basic Auth implementation
│   │   ├── BasicAuthConstants.cs  # Auth constants
│   │   ├── BasicAuthHandler.cs    # Auth handler middleware
│   │   └── BasicAuthOptions.cs    # Auth configuration options
│   ├── Controllers/               # API Controllers
│   │   └── PostsController.cs     # Posts CRUD operations
│   ├── Helpers/                   # Utility classes
│   │   └── AppSettings.cs         # Application settings model
│   ├── Infrastructure/            # Data access layer
│   │   └── Data/
│   │       ├── TobisoDbContext.cs # Entity Framework context
│   │       └── Migrations/        # EF database migrations
│   └── Services/                  # Business logic services
│       └── PostService.cs         # Post business logic
├── Tobiso.Web.App/               # Blazor Server Frontend
│   ├── Authentication/           # Client-side auth components
│   │   ├── AuthenticationHeaderHandler.cs     # HTTP auth header handler
│   │   ├── BasicAuthenticationStateProvider.cs # Auth state management
│   │   └── CredentialStore.cs     # Secure credential storage
│   ├── Components/               # Blazor components
│   │   ├── Layout/               # Layout components
│   │   │   ├── MainLayout.razor   # Main app layout with navigation
│   │   │   ├── EmptyLayout.razor  # Clean layout (login page)
│   │   │   └── NavMenu.razor      # Navigation component
│   │   ├── Pages/                # Page components
│   │   │   ├── Home.razor         # Posts listing page
│   │   │   ├── Login.razor        # Authentication page
│   │   │   ├── Counter.razor      # Sample counter page
│   │   │   ├── Weather.razor      # Sample weather page
│   │   │   └── Error.razor        # Error handling page
│   │   ├── _Imports.razor         # Global using statements
│   │   ├── App.razor              # Root application component
│   │   └── Routes.razor           # Routing configuration
│   ├── Handlers/                 # HTTP message handlers
│   │   └── HttpLoggingHandler.cs  # Request/response logging
│   ├── Properties/               # Configuration files
│   │   └── launchSettings.json    # Development launch settings
│   ├── wwwroot/                  # Static web assets
│   │   ├── app.css               # Application styles
│   │   ├── favicon.png           # Site icon
│   │   └── lib/                  # Third-party libraries
│   ├── appsettings.json          # Production configuration
│   ├── appsettings.Development.json # Development configuration
│   └── Program.cs                # Application entry point
├── Tobiso.Web.Domain/            # Domain Layer (Entities)
│   └── Entities/
│       ├── Post.cs               # Post entity model
│       └── Category.cs           # Category entity model
└── Tobiso.Web.Shared/            # Shared Components
    ├── DTOs/                     # Data Transfer Objects
    │   └── PostResponse.cs       # Post API response model
    └── Interfaces/               # Shared interfaces
        └── ITobisoWebApi.cs      # API client interface (Refit)
```

## Technology Stack

### Backend
- **.NET 9**: Latest .NET framework
- **ASP.NET Core**: Web framework
- **Entity Framework Core**: ORM for database operations
- **SQL Server**: Primary database
- **Serilog**: Structured logging
- **Swagger/OpenAPI**: API documentation

### Frontend
- **Blazor Server**: Server-side rendering with SignalR
- **Bootstrap 5**: UI framework and responsive design
- **Refit**: HTTP client library for API calls
- **JavaScript Interop**: Browser storage and DOM manipulation

### Authentication & Security
- **Basic HTTP Authentication**: Simple username/password auth
- **Local Storage**: Client-side credential caching
- **HTTPS**: Secure communication
- **CORS**: Cross-origin resource sharing configuration

## Core Features

### 1. Authentication System
- **Login Page**: Clean, centered login form with no navigation
- **Credential Storage**: Secure client-side credential management
- **Session Management**: Automatic authentication state tracking
- **Unauthorized Handling**: Automatic redirects to login on 401 errors

### 2. Posts Management
- **Posts Listing**: Table view of all posts from database
- **Post Details**: ID, Title, Category, File Path, Update Date
- **Category Support**: Post categorization system
- **Responsive Design**: Mobile-friendly table layout

### 3. User Interface
- **Main Layout**: Navigation with sidebar for authenticated users
- **Empty Layout**: Clean layout for login/authentication pages
- **Responsive Design**: Bootstrap-based responsive layouts
- **Professional Styling**: Modern, clean interface design

## Database Schema

### Posts Table
```sql
Posts (
    Id int PRIMARY KEY IDENTITY,
    Title nvarchar(max) NOT NULL,
    Content nvarchar(max) NOT NULL,
    FilePath nvarchar(max) NOT NULL,
    CreatedAt datetime2 NOT NULL,
    UpdatedAt datetime2 NULL,
    CategoryId int NULL,
    FOREIGN KEY (CategoryId) REFERENCES Categories(Id)
)
```

### Categories Table
```sql
Categories (
    Id int PRIMARY KEY IDENTITY,
    Name nvarchar(128) NOT NULL,
    Slug nvarchar(128) NOT NULL,
    ParentId int NULL,
    FOREIGN KEY (ParentId) REFERENCES Categories(Id)
)
```

## Configuration

### Application Settings

#### Development Environment
- **Database**: `server=db23040.public.databaseasp.net;user=db23040;password=6Tm_=a4JrD?3;database=db23040;Encrypt=False`
- **API Base URL**: `https://localhost:7270`
- **Logging**: Debug level with console and file output

#### Production Environment
- **Database**: `server=db23040.databaseasp.net;user=db23040;password=6Tm_=a4JrD?3;database=db23040;Encrypt=False`
- **API Base URL**: `https://localhost:7270`
- **Logging**: Information level with structured logging

#### Authentication Configuration
```json
{
  "Auth": {
    "Basic": {
      "Username": "admin",
      "Password": "secret123",
      "UserId": "11111111-1111-1111-1111-111111111111"
    }
  }
}
```

## API Endpoints

### Posts Controller
- **GET /api/Posts**: Retrieve all posts
  - **Authentication**: Required (Basic Auth)
  - **Response**: `IList<PostResponse>`
  - **Error Handling**: 401 Unauthorized on missing/invalid credentials

## Authentication Flow

1. **User visits protected page** → Redirected to `/login` if not authenticated
2. **User enters credentials** → Stored in browser localStorage
3. **API calls include Basic Auth header** → Automatically added by `AuthenticationHeaderHandler`
4. **Authentication state tracked** → `BasicAuthenticationStateProvider` manages login state
5. **401 Responses** → Automatic redirect to login page via `Refit.ApiException` handling

## Development Guidelines

### Code Organization
- **Clean Architecture**: Clear separation of concerns across layers
- **Domain-Driven Design**: Entity models in separate domain layer
- **SOLID Principles**: Dependency injection and interface segregation
- **RESTful APIs**: Standard HTTP methods and status codes

### Naming Conventions
- **Controllers**: `{EntityName}Controller.cs`
- **Services**: `{EntityName}Service.cs`
- **DTOs**: `{EntityName}Response.cs`, `{EntityName}Request.cs`
- **Entities**: `{EntityName}.cs`
- **Pages**: `{PageName}.razor`

### Error Handling
- **Global Exception Handling**: Centralized error management
- **Logging**: Comprehensive logging with Serilog
- **User-Friendly Messages**: Clear error messages for end users
- **Console Debugging**: Development-time console output

## Security Considerations

### Authentication Security
- **HTTPS Only**: All communication over secure connections
- **Credential Storage**: Local storage with automatic cleanup
- **Session Timeout**: Automatic logout on token expiration
- **CORS Configuration**: Restricted cross-origin access

### Data Protection
- **SQL Injection Prevention**: Entity Framework parameterized queries
- **XSS Protection**: Blazor's built-in protection
- **Input Validation**: Server-side validation for all inputs
- **Error Information Disclosure**: Limited error details in production

## Deployment Configuration

### Launch Settings
- **HTTPS**: `https://localhost:7270`
- **HTTP**: `http://localhost:5122`
- **Environment**: Development
- **Browser Launch**: Enabled

### Build Configuration
- **Target Framework**: net9.0
- **Nullable**: Enabled
- **Implicit Usings**: Enabled
- **Warnings as Errors**: Disabled (warnings logged)

## Testing Strategy

### Unit Testing (Recommended)
- **Services Layer**: Business logic validation
- **Controllers**: API endpoint testing
- **Authentication**: Auth flow verification
- **Database**: Repository pattern testing

### Integration Testing (Recommended)
- **API Endpoints**: Full request/response cycle
- **Authentication Flow**: End-to-end auth testing
- **Database Operations**: EF Core integration testing

## Future Enhancements

### Phase 1 Enhancements
- **Post CRUD Operations**: Create, Update, Delete posts
- **Category Management**: Category CRUD operations
- **User Management**: Multiple user accounts
- **Rich Text Editor**: Enhanced content editing

### Phase 2 Enhancements
- **File Upload**: Media management system
- **Search Functionality**: Full-text search across posts
- **Pagination**: Large dataset handling
- **Role-Based Access**: Admin/Editor/Viewer roles

### Phase 3 Enhancements
- **Real-time Updates**: SignalR integration
- **API Versioning**: Backward compatibility
- **Caching Layer**: Performance optimization
- **Analytics Dashboard**: Usage metrics and reporting

## Troubleshooting

### Common Issues

1. **Build Errors**: Check namespace references and using statements
2. **Database Connection**: Verify connection string and database availability
3. **Authentication Issues**: Check credentials in appsettings.json
4. **Port Conflicts**: Ensure ports 7270 and 5122 are available
5. **Layout Errors**: Verify _Imports.razor includes all necessary namespaces

### Development Tips

1. **Console Logging**: Check browser console for detailed error information
2. **Network Tab**: Monitor API calls and responses
3. **Entity Framework**: Use migrations for database schema changes
4. **Hot Reload**: Blazor supports hot reload for rapid development
5. **Debugging**: Use breakpoints in both client and server code

## Support and Maintenance

### Version Control
- **Git Repository**: All code versioned with Git
- **Branching Strategy**: Feature branches with main branch protection
- **Commit Standards**: Conventional commit messages

### Documentation Maintenance
- **PRD Updates**: Keep this document current with changes
- **API Documentation**: Swagger/OpenAPI automatically generated
- **Code Comments**: Inline documentation for complex logic

---

**Document Version**: 1.0  
**Last Updated**: July 7, 2025  
**Author**: AI Development Assistant  
**Review Status**: Initial Version
