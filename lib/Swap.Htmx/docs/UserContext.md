# User Context & Identity

Swap.Htmx uses the `ISwapUserContext` abstraction to identify the current user. This is primarily used for:
1.  **Server-Sent Events (SSE)**: Determining which "User" channel a connection belongs to.
2.  **State Management**: Ensuring consistent identification across requests.

## Default Behavior: Session

By default, Swap uses `SessionSwapUserContext`. This implementation relies on ASP.NET Core Session state.

- It automatically ensures a session cookie is sent to the client by writing a `_swap_session_initialized` value if one doesn't exist.
- It returns `HttpContext.Session.Id` as the user identifier.

**Prerequisites for Default Behavior:**
You must enable Session middleware in your application:

```csharp
// Program.cs
builder.Services.AddSession();

var app = builder.Build();

app.UseSession(); // Must be before UseSwapHtmx()
app.UseSwapHtmx();
```

## Custom User Context

If your application uses Authentication (Cookies, JWT, Identity) and you want to use the authenticated User ID instead of a Session ID, you can implement your own `ISwapUserContext`.

### 1. Implement the Interface

```csharp
using Swap.Htmx.Services;
using System.Security.Claims;

public class AuthenticatedUserContext : ISwapUserContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuthenticatedUserContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string GetSessionId()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        
        // Return the User's ID (Subject claim) or Name
        if (user?.Identity?.IsAuthenticated == true)
        {
            return user.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                ?? user.Identity.Name 
                ?? throw new InvalidOperationException("User is authenticated but has no ID.");
        }

        // Fallback for anonymous users (optional, or throw)
        return "anonymous-" + _httpContextAccessor.HttpContext?.TraceIdentifier;
    }
}
```

### 2. Register the Service

Register your custom implementation **after** calling `AddSwapHtmx()`. This overrides the default registration.

```csharp
// Program.cs
builder.Services.AddSwapHtmx();

// Override the default Session-based context
builder.Services.AddScoped<ISwapUserContext, AuthenticatedUserContext>();
```

## Accessing the Context

You can access the current ID in your controllers via the `GetOrInitializeSessionId()` helper method (which is now a wrapper around this service) or by injecting `ISwapUserContext` directly.

```csharp
public class MyController : SwapController
{
    public IActionResult Index()
    {
        var userId = GetOrInitializeSessionId(); // Uses your custom implementation
        return SwapView();
    }
}
```
