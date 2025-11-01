---
sidebar_position: 8
---

# swap generate auth

Scaffold a complete ASP.NET Core Identity authentication system with login, registration, password reset, and HTMX-compatible views.

## Synopsis

```bash
swap generate auth [options]
swap g auth  # Short alias
```

## Options

- `--dry-run` - Preview what would be generated without writing files
- `--force` - Overwrite existing files without prompting
- `--project <path>` or `-p` - Path to project directory (default: current directory)

## Description

The `generate auth` command scaffolds a complete authentication system using ASP.NET Core Identity with:

- **ApplicationUser Model** - Extends `IdentityUser` with `DisplayName`, `CreatedAt`, `LastLoginAt`
- **4 ViewModels** - Login, Register, ForgotPassword, ResetPassword
- **AuthController** - Complete auth flow with login, register, logout, password reset
- **7 Views** - All auth pages styled with Tailwind CSS and DaisyUI
- **_LoginPartial** - Navbar authentication UI
- **HTMX Compatible** - All views work with HTMX navigation
- **Email Configuration** - Placeholder for email service integration
- **Password Requirements** - Configurable password validation
- **Account Lockout** - Optional lockout on failed attempts
- **Remember Me** - Persistent login support

## What Gets Generated

### Models
- `ApplicationUser.cs` - Custom Identity user with additional properties

### ViewModels
- `LoginViewModel.cs` - Email and password
- `RegisterViewModel.cs` - Registration with password confirmation
- `ForgotPasswordViewModel.cs` - Password reset request
- `ResetPasswordViewModel.cs` - Password reset with token

### Controllers
- `AuthController.cs` - Complete authentication logic

### Views
- `Login.cshtml` - Login page
- `Register.cshtml` - Registration page
- `ForgotPassword.cshtml` - Request password reset
- `ForgotPasswordConfirmation.cshtml` - Reset email sent confirmation
- `ResetPassword.cshtml` - Reset password form
- `ResetPasswordConfirmation.cshtml` - Password reset success
- `AccessDenied.cshtml` - Access denied page
- `_LoginPartial.cshtml` - Navbar user menu

### Other
- **Migration** - `AddIdentity` migration created automatically after build verification
- **Setup Instructions** - Printed to console with code snippets for `Program.cs` and `DbContext`

## Setup Steps

After running `swap g auth`, you need to configure your application:

### 1. Update DbContext

```csharp
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }
    
    // Your existing DbSets...
}
```

### 2. Update Program.cs

Add Identity services before `builder.Build()`:

```csharp
// Add Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 8;
    
    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;
    
    // User settings
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedEmail = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Configure cookie settings
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Auth/Login";
    options.LogoutPath = "/Auth/Logout";
    options.AccessDeniedPath = "/Auth/AccessDenied";
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromDays(30);
});
```

Add authentication middleware after `app.UseRouting()`:

```csharp
app.UseRouting();
app.UseAuthentication();  // Add this
app.UseAuthorization();
app.MapControllerRoute(...);
```

### 3. Add NuGet Package

```bash
dotnet add package Microsoft.AspNetCore.Identity.EntityFrameworkCore
```

### 4. Apply Migration

```bash
dotnet ef database update
```

## Examples

```bash
# Generate authentication
swap g auth

# Preview without writing files
swap g auth --dry-run

# Overwrite existing auth files
swap g auth --force
```

## Features

### User Registration
- Email and password validation
- Password strength requirements
- Automatic user creation
- Immediate login after registration

### Login
- Email and password authentication
- Remember me functionality
- Account lockout after failed attempts
- Redirect to return URL after login

### Password Reset
- Email-based token generation
- Secure reset links
- Token expiration
- Password confirmation

### UI Components
- **DaisyUI Styling** - Modern, accessible components
- **Tailwind CSS** - Utility-first styling
- **HTMX Compatible** - Works with HTMX navigation
- **Responsive Design** - Mobile-friendly layouts
- **Validation** - Client and server-side validation
- **Error Messages** - Clear, user-friendly feedback

## Protecting Routes

After setting up authentication, protect your controllers:

```csharp
using Microsoft.AspNetCore.Authorization;

[Authorize]  // Require authentication
public class ProductController : Controller
{
    // All actions require authentication
}

[Authorize(Roles = "Admin")]  // Require specific role
public class AdminController : Controller
{
    // Only admins can access
}

[AllowAnonymous]  // Public endpoint
public IActionResult PublicAction()
{
    return View();
}
```

## Email Configuration

The generated code includes placeholders for email service. To enable email:

1. **Choose an email service** (SendGrid, AWS SES, SMTP, etc.)
2. **Configure in appsettings.json**:
   ```json
   {
     "Email": {
       "Provider": "SendGrid",
       "ApiKey": "your-api-key",
       "FromEmail": "noreply@yourapp.com",
       "FromName": "Your App"
     }
   }
   ```
3. **Implement `IEmailSender`** in your project
4. **Register service** in `Program.cs`:
   ```csharp
   builder.Services.AddTransient<IEmailSender, EmailService>();
   ```

## Security Best Practices

- ✅ Use HTTPS in production
- ✅ Enable email confirmation for production
- ✅ Configure lockout settings
- ✅ Use strong password requirements
- ✅ Implement two-factor authentication (not included)
- ✅ Store secrets in environment variables or Azure Key Vault
- ✅ Regularly rotate authentication keys

## Related Commands

- [swap new](./new) - Create new project with authentication
- [swap generate controller](./generate-controller) - Generate protected controllers
- [swap generate pattern](./generate-pattern) - Add auditable pattern to track user actions

## Notes

- Authentication scaffolding follows ASP.NET Core Identity best practices
- Views are styled with DaisyUI and work with HTMX navigation
- Email confirmation is disabled by default for easier development
- Migration is automatically created after build verification
- Setup instructions are printed after generation

---

**Next Steps:**
1. Run `swap g auth`
2. Follow printed setup instructions
3. Add Identity package and configure services
4. Apply migrations
5. Test login/registration at `/Auth/Login` and `/Auth/Register`
