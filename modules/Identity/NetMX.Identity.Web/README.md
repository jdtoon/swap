# NetMX.Identity.Web

HTMX-powered web UI for the NetMX Identity module.

## Overview

Provides controllers and Razor views for user authentication, registration, and profile management using HTMX for progressive enhancement and a modern user experience.

## Features

### Authentication
- **Login/Logout** - Cookie-based authentication with ASP.NET Core Identity
- **Registration** - New user sign-up with password validation
- **Lockout Protection** - Automatic lockout after 5 failed login attempts (15 minutes)
- **2FA Detection** - Ready for two-factor authentication flows

### Profile Management
- **Profile Updates** - Edit first name, last name, and phone number
- **Password Changes** - Secure password update with current password verification
- **Account Information** - Display email verification, phone verification, 2FA status

### HTMX Integration
- **Progressive Enhancement** - Works with and without JavaScript
- **Partial Updates** - No full page reloads on form submissions
- **Event-Driven** - Components communicate via HTMX events
- **Loading Indicators** - Smooth user feedback during operations

## Project Structure

```
NetMX.Identity.Web/
├── Controllers/
│   └── AccountController.cs       # Account management (9 actions)
├── Views/
│   ├── Account/
│   │   ├── Login.cshtml           # Main login page
│   │   ├── _LoginForm.cshtml      # HTMX login form partial
│   │   ├── _LockedOut.cshtml      # Lockout notification
│   │   ├── Register.cshtml        # Main registration page
│   │   ├── _RegisterForm.cshtml   # HTMX registration form
│   │   ├── _RegisterSuccess.cshtml # Success message
│   │   ├── Profile.cshtml         # Profile management page
│   │   ├── _ProfileForm.cshtml    # Profile edit form
│   │   ├── _ProfileSuccess.cshtml # Profile update success
│   │   ├── _ChangePasswordForm.cshtml    # Password change form
│   │   └── _PasswordChangeSuccess.cshtml # Password change success
│   └── _ViewImports.cshtml        # Razor imports and tag helpers
└── NetMX.Identity.Web.csproj
```

## Controllers

### AccountController

**Routes:**
- `GET /account/login` - Display login page
- `POST /account/login` - Process login (HTMX-aware)
- `GET /account/register` - Display registration page
- `POST /account/register` - Process registration (HTMX-aware)
- `GET /account/profile` - Display user profile
- `POST /account/profile` - Update profile (HTMX-aware)
- `POST /account/change-password` - Change password (HTMX-aware)
- `POST /account/logout` - Log out user
- `GET /account/two-factor` - Two-factor authentication (placeholder)
- `GET /account/access-denied` - Access denied page

**HTMX Features:**
- Detects HTMX requests via `Request.IsHtmx()`
- Returns partials for HTMX, full views for regular requests
- Uses `HxTrigger()` for client-side events
- Uses `HxRedirect()` for client-side navigation

## HTMX Events

The Identity.Web module triggers the following events for loose coupling:

| Event | Trigger | Payload | Usage |
|-------|---------|---------|-------|
| `login:success` | Successful login | `{ userId: Guid }` | Update UI, track analytics |
| `register:success` | New user registered | `{ userId: Guid }` | Show welcome message |
| `profile:updated` | Profile saved | None | Refresh dependent components |
| `password:changed` | Password updated | None | Show confirmation |
| `logout:success` | User logged out | None | Clear client state |

**Example - Listen for events:**
```html
<div hx-get="/api/notifications" 
     hx-trigger="login:success from:body">
    <!-- Auto-refresh notifications on login -->
</div>
```

## Views

### Login Flow
1. **Login.cshtml** - Main page with form container
2. **_LoginForm.cshtml** - HTMX-powered login form
   - Username/email input
   - Password input
   - Remember me checkbox
   - Loading spinner during submission
3. **_LockedOut.cshtml** - Displayed when account is locked

### Registration Flow
1. **Register.cshtml** - Main page with form container
2. **_RegisterForm.cshtml** - HTMX registration form
   - Username, email, password inputs
   - First/last name (optional)
   - Terms acceptance checkbox
3. **_RegisterSuccess.cshtml** - Success message with login link

### Profile Flow
1. **Profile.cshtml** - Main page with two sections
   - Profile edit form
   - Password change form
   - Account information sidebar
2. **_ProfileForm.cshtml** - Edit first name, last name, phone
3. **_ProfileSuccess.cshtml** - Success notification
4. **_ChangePasswordForm.cshtml** - Current + new password
5. **_PasswordChangeSuccess.cshtml** - Success notification

## Usage

### 1. Add to Your Application

Reference the Identity.Web project in your main web application:

```xml
<ItemGroup>
  <ProjectReference Include="..\..\modules\Identity\NetMX.Identity.Web\NetMX.Identity.Web.csproj" />
</ItemGroup>
```

### 2. Configure Authentication

In `Program.cs`:

```csharp
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// Add cookie authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/account/login";
        options.LogoutPath = "/account/logout";
        options.AccessDeniedPath = "/account/access-denied";
        options.ExpireTimeSpan = TimeSpan.FromHours(1);
        options.SlidingExpiration = true;
    });

// Add Identity services
builder.Services.AddScoped<IUserAppService, UserAppService>();
builder.Services.AddScoped<IRoleAppService, RoleAppService>();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
```

### 3. Use in Layout

Add login/logout links to your `_Layout.cshtml`:

```html
@if (User.Identity?.IsAuthenticated == true)
{
    <div class="navbar-item has-dropdown is-hoverable">
        <a class="navbar-link">
            @User.Identity.Name
        </a>
        <div class="navbar-dropdown">
            <a href="/account/profile" class="navbar-item">My Profile</a>
            <hr class="navbar-divider">
            <form method="post" action="/account/logout">
                @Html.AntiForgeryToken()
                <button type="submit" class="navbar-item button is-text">
                    Logout
                </button>
            </form>
        </div>
    </div>
}
else
{
    <a href="/account/login" class="navbar-item">Login</a>
    <a href="/account/register" class="navbar-item">Sign Up</a>
}
```

### 4. Protect Controllers

Use `[Authorize]` attribute on controllers/actions:

```csharp
[Authorize]
public class DashboardController : Controller
{
    public IActionResult Index()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        // Access user's dashboard
        return View();
    }
}
```

## Security Features

### Password Requirements
- Minimum 8 characters
- Must contain uppercase letter
- Must contain lowercase letter
- Must contain number
- Must contain special character

### Lockout Policy
- Max failed attempts: 5
- Lockout duration: 15 minutes
- Auto-reset on successful login

### CSRF Protection
- Anti-forgery tokens on all forms
- Automatic validation by ASP.NET Core
- HTMX includes tokens automatically

### Cookie Security
- HttpOnly cookies (not accessible via JavaScript)
- Secure flag (HTTPS only in production)
- SameSite policy (CSRF protection)
- Sliding expiration

## Styling

Views use **Bulma CSS** classes for styling. The HTMX interactions include:

- `.htmx-indicator` - Hidden by default, shown during requests
- `.htmx-request` - Added to elements during HTMX requests
- `.notification` - Bulma notifications for success/error messages
- `.button`, `.input`, `.field` - Bulma form components

## Dependencies

- **NetMX.Identity.Application** - Application services
- **NetMX.Identity.Contracts** - DTOs and interfaces
- **NetMX.AspNetCore.Mvc** - HTMX extensions
- **Microsoft.AspNetCore.App** - ASP.NET Core framework

## Future Enhancements

- [ ] Two-factor authentication implementation
- [ ] Password reset flow via email
- [ ] Email confirmation flow
- [ ] Phone number verification
- [ ] External authentication providers (Google, Microsoft, etc.)
- [ ] Account deletion with confirmation
- [ ] Session management (view active sessions, revoke)
- [ ] Admin panel for user management

## Testing

The Identity.Web controllers can be tested using `WebApplicationFactory`:

```csharp
public class AccountControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Login_Should_RedirectToDashboard_OnSuccess()
    {
        // Arrange
        var client = _factory.CreateClient();
        
        // Act
        var response = await client.PostAsync("/account/login", 
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["UserName"] = "testuser",
                ["Password"] = "TestPass123!"
            }));
        
        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    }
}
```

## License

Part of the NetMX framework - see root LICENSE file.
