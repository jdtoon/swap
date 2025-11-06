# Security Policy

## Supported Versions

We actively support the following versions with security updates:

| Version | Supported          |
| ------- | ------------------ |
| 0.4.x   | :white_check_mark: |
| 0.3.x   | :white_check_mark: |
| 0.2.x   | :x:                |
| 0.1.x   | :x:                |
| < 0.1   | :x:                |

## Reporting a Vulnerability

**Please do not report security vulnerabilities through public GitHub issues.**

Instead, please report them via email to **security@swap-framework.dev** (or your configured security contact email).

You should receive a response within 48 hours. If for some reason you do not, please follow up via email to ensure we received your original message.

Please include the following information in your report:

- Type of vulnerability (e.g., SQL injection, XSS, CSRF, etc.)
- Full paths of source file(s) related to the vulnerability
- The location of the affected source code (tag/branch/commit or direct URL)
- Any special configuration required to reproduce the issue
- Step-by-step instructions to reproduce the issue
- Proof-of-concept or exploit code (if possible)
- Impact of the issue, including how an attacker might exploit it

This information will help us triage your report more quickly.

## Security Best Practices for Generated Code

### Template-Generated Applications

When using Swap CLI to generate applications, please be aware of the following security considerations:

#### 1. Database Connection Strings

**Templates include default connection strings for development:**

```json
// appsettings.json (DO NOT USE IN PRODUCTION)
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=app.db"
  }
}
```

**Production best practices:**
- Never commit `appsettings.Production.json` to version control
- Use environment variables for sensitive configuration
- Use Azure Key Vault, AWS Secrets Manager, or similar for secrets management
- Rotate database credentials regularly

**Docker deployments:**
```bash
# Good: Environment variable
docker run -e "ConnectionStrings__DefaultConnection=Server=..." myapp

# Bad: Hardcoded in docker-compose.yml
services:
  app:
    environment:
      - ConnectionStrings__DefaultConnection=Server=...;Password=hardcoded123
```

#### 2. Default Passwords in Templates

**Docker Compose templates include default passwords for local development:**

```yaml
# docker-compose.yml (DEVELOPMENT ONLY)
services:
  db:
    environment:
      - SA_PASSWORD=YourStrong@Passw0rd  # ⚠️ CHANGE IN PRODUCTION
      - POSTGRES_PASSWORD=postgres        # ⚠️ CHANGE IN PRODUCTION
```

**Always change default passwords before deploying to production.**

#### 3. HTTPS Configuration

Generated templates default to HTTP for local development:

```csharp
// Program.cs (Development)
app.UseHttpsRedirection(); // Only in Production
```

**Production deployment checklist:**
- Enable HTTPS redirection
- Configure SSL certificates (Let's Encrypt, cloud provider certs)
- Enable HSTS headers
- Set secure cookie flags

#### 4. CORS Configuration

Templates include permissive CORS for development:

```csharp
// DO NOT USE IN PRODUCTION
app.UseCors(policy => policy
    .AllowAnyOrigin()
    .AllowAnyMethod()
    .AllowAnyHeader());
```

**Production CORS:**
```csharp
app.UseCors(policy => policy
    .WithOrigins("https://yourdomain.com")
    .AllowAnyMethod()
    .AllowAnyHeader());
```

#### 5. Data Protection Keys

Templates configure data protection keys for Docker:

```csharp
// Program.cs
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo("/app/keys"));
```

**Production considerations:**
- Use Azure Blob Storage, AWS S3, or Redis for key storage
- Ensure keys directory has proper permissions (700)
- Back up data protection keys
- Implement key rotation policies

#### 6. Input Validation

Generated controllers include basic model binding but minimal validation:

```csharp
[HttpPost]
public async Task<IActionResult> Create(string title)
{
    // ⚠️ Add validation before production
    await _service.CreateAsync(title);
    return Ok();
}
```

**Add comprehensive validation:**
```csharp
[HttpPost]
public async Task<IActionResult> Create([FromForm] CreateTodoRequest request)
{
    if (!ModelState.IsValid)
        return BadRequest(ModelState);
    
    await _service.CreateAsync(request.Title);
    return Ok();
}

public class CreateTodoRequest
{
    [Required]
    [StringLength(500, MinimumLength = 1)]
    [RegularExpression(@"^[a-zA-Z0-9\s\-_]+$")]
    public string Title { get; set; } = string.Empty;
}
```

#### 7. SQL Injection Protection

EF Core protects against SQL injection by default through parameterized queries:

```csharp
// Safe (parameterized)
var todos = await _db.TodoItems
    .Where(t => t.Title.Contains(searchTerm))
    .ToListAsync();
```

**Never use raw SQL with user input:**
```csharp
// DANGEROUS - SQL Injection vulnerability
var sql = $"SELECT * FROM TodoItems WHERE Title LIKE '%{searchTerm}%'";
var todos = await _db.TodoItems.FromSqlRaw(sql).ToListAsync();
```

#### 8. CSRF Protection

ASP.NET Core provides automatic CSRF protection for forms:

```html
<!-- CSRF token automatically included -->
<form method="post">
    @Html.AntiForgeryToken()
</form>
```

**Ensure CSRF tokens are validated:**
```csharp
// Automatically validated by [ValidateAntiForgeryToken]
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Create(CreateTodoRequest request)
{
    // ...
}
```

#### 9. Authentication & Authorization

Templates do not include authentication by default. Add using:

```bash
swap generate auth
```

**Security checklist:**
- Use strong password policies
- Implement account lockout after failed attempts
- Enable two-factor authentication
- Use secure session management
- Implement proper role-based access control

#### 10. Dependency Vulnerabilities

**Regularly scan for vulnerable packages:**

```bash
# Check for outdated packages with known vulnerabilities
dotnet list package --vulnerable

# Update packages
dotnet add package Microsoft.AspNetCore.Mvc --version 9.0.1
```

**Enable GitHub Dependabot:**
- Automatic pull requests for security updates
- Vulnerability alerts

---

## Security Audit Checklist

Before deploying to production:

- [ ] Changed all default passwords
- [ ] Configured HTTPS with valid certificates
- [ ] Restricted CORS to known origins
- [ ] Moved secrets to environment variables or secret management service
- [ ] Enabled HSTS and secure cookie flags
- [ ] Added comprehensive input validation
- [ ] Implemented authentication and authorization
- [ ] Configured data protection key storage
- [ ] Reviewed and removed development-only code
- [ ] Scanned dependencies for vulnerabilities
- [ ] Configured proper error handling (no stack traces in production)
- [ ] Enabled request rate limiting
- [ ] Configured proper logging (no sensitive data)
- [ ] Reviewed database migration scripts
- [ ] Implemented security headers (CSP, X-Frame-Options, etc.)

---

## Known Security Considerations

### Development Event Dashboard

The event dashboard (`/_swap/dev/events`) is **Development-only** and automatically disabled in production:

```csharp
// Web/Program.cs
if (!app.Environment.IsDevelopment())
{
    // app.MapSwapDevDashboard(); // Commented out in production
}
```

**Verify this is disabled before deploying.**

### SQLite in Production

Templates default to SQLite for simplicity. **SQLite is not recommended for production:**

- No native multi-user support
- File locking issues
- Limited scalability

**Use SQL Server, PostgreSQL, or MySQL in production.**

### Session Storage

Session state is stored in-memory by default:

```csharp
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession();
```

**For multi-instance deployments, use Redis or SQL Server:**

```csharp
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = "localhost:6379";
});
```

---

## Security Resources

- [OWASP Top 10](https://owasp.org/www-project-top-ten/)
- [ASP.NET Core Security Best Practices](https://docs.microsoft.com/en-us/aspnet/core/security/)
- [EF Core Security](https://docs.microsoft.com/en-us/ef/core/miscellaneous/security)
- [Docker Security](https://docs.docker.com/engine/security/)

---

## Contact

For security concerns or questions:
- **Email:** security@swap-framework.dev
- **GitHub Issues:** For non-security bugs only

---

**Last Updated:** November 6, 2025
