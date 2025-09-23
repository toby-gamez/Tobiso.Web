# Tobiso.Web Security Implementation TODO

## High Priority Security Tasks

### 1. Implement Content Security Policy (CSP) Headers
**Priority:** CRITICAL  
**Estimated Time:** 2-4 hours  
**Status:** ⏳ Pending

**Description:**
Implement comprehensive CSP headers to protect against XSS attacks, code injection, and clickjacking in the Blazor WebAssembly applications.

**Implementation Steps:**
- [ ] Add CSP middleware to `Tobiso.Web.Api/Program.cs`
- [ ] Configure CSP headers for Blazor WebAssembly requirements
- [ ] Add additional security headers (X-Frame-Options, X-Content-Type-Options)
- [ ] Test CSP implementation with both App and Admin frontends
- [ ] Document CSP configuration in security documentation

**Code Location:**
- `Tobiso.Web.Api/Program.cs` - Add security middleware
- Update startup configuration

**Acceptance Criteria:**
- [ ] CSP headers prevent script injection attacks
- [ ] Blazor WebAssembly applications function correctly with CSP
- [ ] Browser console shows no CSP violations during normal usage
- [ ] Security headers are present in all HTTP responses

**Code Implementation:**
```csharp
// Add this middleware in Tobiso.Web.Api/Program.cs
app.Use(async (context, next) =>
{
    var csp = "default-src 'self'; " +
              "script-src 'self' 'wasm-unsafe-eval'; " +
              "style-src 'self' 'unsafe-inline'; " +
              "img-src 'self' data: https:; " +
              "connect-src 'self'; " +
              "frame-ancestors 'none'; " +
              "base-uri 'self'";
              
    context.Response.Headers.Add("Content-Security-Policy", csp);
    context.Response.Headers.Add("X-Frame-Options", "DENY");
    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
    
    await next();
});
```

**Notes:**
This addresses the most critical frontend security vulnerability identified in the security analysis. Since Blazor WebAssembly code is completely exposed to clients, CSP provides essential protection against malicious script execution.

---

### 2. Enhance Input Validation on DTOs
**Priority:** HIGH  
**Estimated Time:** 4-6 hours  
**Status:** ⏳ Pending

**Description:**
Add comprehensive validation attributes to all DTOs in `Tobiso.Web.Shared` to prevent over-posting and injection attacks.

**Implementation Steps:**
- [ ] Review all DTOs in `Tobiso.Web.Shared/DTOs/`
- [ ] Add validation attributes (Required, StringLength, Range, etc.)
- [ ] Implement custom validation for complex business rules
- [ ] Add server-side validation in controllers
- [ ] Update API documentation with validation requirements

**Code Location:**
- `Tobiso.Web.Shared/DTOs/` - Add validation attributes
- `Tobiso.Web.Api/Controllers/` - Ensure ModelState validation

**Code Example:**
```csharp
public class PostDto
{
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Title { get; set; }
    
    [Required]
    [StringLength(5000, MinimumLength = 1)]
    public string Content { get; set; }
    
    [StringLength(255)]
    public string FilePath { get; set; }
}
```

---

### 3. Implement Rate Limiting
**Priority:** HIGH  
**Estimated Time:** 2-3 hours  
**Status:** ⏳ Pending

**Description:**
Add rate limiting middleware to protect API endpoints from abuse and DoS attacks.

**Implementation Steps:**
- [ ] Add rate limiting middleware to `Tobiso.Web.Api`
- [ ] Configure different limits for public vs authenticated endpoints
- [ ] Add rate limiting headers to responses
- [ ] Document rate limits in API documentation

**Code Location:**
- `Tobiso.Web.Api/Program.cs` - Rate limiting configuration

**Code Implementation:**
```csharp
services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("Api", config =>
    {
        config.Window = TimeSpan.FromMinutes(1);
        config.PermitLimit = 100;
    });
    
    options.AddFixedWindowLimiter("PublicApi", config =>
    {
        config.Window = TimeSpan.FromMinutes(1);
        config.PermitLimit = 30;
    });
});
```

---

## Medium Priority Tasks

### 4. Upgrade Authentication System
**Priority:** MEDIUM  
**Estimated Time:** 8-12 hours  
**Status:** ⏳ Pending

**Description:**
Replace custom Basic Authentication with JWT tokens for improved security and session management.

**Implementation Steps:**
- [ ] Design JWT token structure and claims
- [ ] Implement JWT authentication in `Tobiso.Web.Api`
- [ ] Update `BasicAuthenticationStateProvider.cs` to handle JWT
- [ ] Add token refresh mechanism
- [ ] Update `CredentialStore.cs` for secure token storage
- [ ] Test authentication flow in both App and Admin

**Code Location:**
- `Tobiso.Web.Api/Authentication/` - JWT implementation
- `Tobiso.Web.App/Authentication/` - Update auth state provider
- `Tobiso.Web.App.Admin/Authentication/` - Update auth state provider

---

### 5. Database Security Enhancements
**Priority:** MEDIUM  
**Estimated Time:** 3-4 hours  
**Status:** ⏳ Pending

**Implementation Steps:**
- [ ] Review all Entity Framework queries for SQL injection risks
- [ ] Implement audit logging for data modifications
- [ ] Add data encryption for sensitive fields
- [ ] Implement soft deletes for data recovery

**Code Location:**
- `Tobiso.Web.Api/Services/` - Review service layer queries
- `Tobiso.Web.Api/Infrastructure/Data/` - Add audit logging

---

### 6. Secure Configuration Management
**Priority:** MEDIUM  
**Estimated Time:** 2-3 hours  
**Status:** ⏳ Pending

**Implementation Steps:**
- [ ] Move sensitive configuration to user secrets (development)
- [ ] Document secure configuration practices for production
- [ ] Review `appsettings.json` files for sensitive data exposure
- [ ] Implement secure credential storage for production

**Code Location:**
- `Tobiso.Web.Api/appsettings.json`
- `Tobiso.Web.App/appsettings.json`
- `Tobiso.Web.App.Admin/appsettings.json`

---

## Low Priority Tasks

### 7. Security Testing Pipeline
**Priority:** LOW  
**Estimated Time:** 6-8 hours  
**Status:** ⏳ Pending

**Implementation Steps:**
- [ ] Set up automated security scanning (Semgrep, CodeQL)
- [ ] Create security test cases
- [ ] Document security testing procedures
- [ ] Integrate security tests into CI/CD pipeline

---

### 8. Dependency Security
**Priority:** LOW  
**Estimated Time:** 1-2 hours monthly  
**Status:** ⏳ Pending

**Implementation Steps:**
- [ ] Regular dependency updates and vulnerability scanning
- [ ] Monitor NuGet packages for security advisories
- [ ] Set up automated dependency update notifications
- [ ] Review and approve dependency updates

---

## Progress Tracking

**Total Tasks:** 8  
**Completed:** 0  
**In Progress:** 0  
**Pending:** 8  

**Next Action:** Start with implementing CSP headers (Task #1) as it provides the highest security impact with minimal implementation effort.

---

**Last Updated:** September 21, 2025  
**Review Schedule:** Weekly during implementation phase  
**Responsible:** Development Team