---
sidebar_position: 9
---

# swap generate htmx-shell

:::info Advanced Use Case
**Note:** New projects already include HTMX shell middleware via the `Swap.Htmx` package and `app.UseSwapHtmxShell()` in `Program.cs`. 

This command is only needed if you want to generate a **local copy** of the middleware for customization purposes.
:::

## Synopsis

```bash
swap generate htmx-shell [options]
swap g htmx-shell  # Short alias
swap g hx  # Shorter alias
```

## Options

- `--add-boost` - Inject `hx-boost="true"` into `_Layout.cshtml` `<body>` tag
- `--project <path>` or `-p` - Path to project directory (default: current directory)

## Description

The `generate htmx-shell` command adds middleware to your project that enforces partial view responses for HTMX requests. This is a **development aid** that helps catch layout rendering bugs early.

**What it does:**
- Detects `HX-Request` headers on incoming requests
- Verifies response HTML doesn't contain full `<html>` tags
- Throws descriptive exception if full page is detected
- Helps prevent "full page in modal" bugs
- Available as middleware generation or via `Swap.Htmx` package

## What Gets Generated

### Middleware
- `SwapHtmxShellMiddleware.cs` - Middleware class with allowlist support

### Configuration
- Wires middleware in `Program.cs` after `UseRouting()`
- Optionally adds `hx-boost="true"` to layout (if `--add-boost` used)

## How It Works

The middleware intercepts responses to HTMX requests and validates they are partials:

```csharp
if (context.Request.Headers.ContainsKey("HX-Request"))
{
    // Check if response contains <html> tag
    if (responseBody.Contains("<html"))
    {
        throw new InvalidOperationException(
            $"Full page returned for HTMX request: {context.Request.Path}"
        );
    }
}
```

## Allowlist Configuration

The middleware includes an allowlist for paths that should return full pages:

```csharp
private static readonly HashSet<string> AllowFullPagePaths = new(StringComparer.OrdinalIgnoreCase)
{
    "/",              // Home page
    "/auth/login",    // Login page
    "/auth/register"  // Registration page
};
```

You can edit `SwapHtmxShellMiddleware.cs` to add more paths.

## Examples

```bash
# Add middleware only
swap g htmx-shell

# Add middleware and enable global hx-boost
swap g htmx-shell --add-boost

# Add to specific project
swap g htmx-shell --project path/to/project
```

## Using the Swap.Htmx Package Instead

Instead of generating the middleware, you can use the `Swap.Htmx` NuGet package:

```bash
# Install package
dotnet add package Swap.Htmx
```

Then configure in `Program.cs`:

```csharp
using Swap.Htmx;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllersWithViews();
builder.Services.AddSwapHtmx();  // Add this

var app = builder.Build();

// Add middleware
app.UseRouting();
app.UseSwapHtmxShell();  // Add this (after UseRouting, before MapControllers)
app.UseAuthorization();
app.MapControllerRoute(...);
```

**Benefits of the package:**
- Includes `SwapController` base class
- HTMX extension methods
- More middleware features
- Easier updates

See [Swap.Htmx documentation](https://www.nuget.org/packages/Swap.Htmx) for details.

## When to Use

**Use middleware when:**
- ✅ Debugging layout issues
- ✅ Catching "full page in modal" bugs
- ✅ Development environment only
- ✅ Learning HTMX patterns

**Skip middleware when:**
- ❌ Production environment (adds overhead)
- ❌ Need full pages for some HTMX requests
- ❌ Using non-standard HTMX patterns

## Common Issues

### "Full page returned" exception

**Problem:** HTMX request returns full page with `<html>` tag

**Causes:**
1. Controller returns `View()` instead of checking `HX-Request` header
2. `_ViewStart.cshtml` assigns layout to partial view
3. Using `SwapView()` incorrectly

**Solutions:**

**Option 1:** Use `SwapController` from `Swap.Htmx`:
```csharp
public class ProductController : SwapController
{
    public IActionResult Index()
    {
        return SwapView();  // Automatically returns partial for HTMX
    }
}
```

**Option 2:** Check headers manually:
```csharp
public IActionResult Index()
{
    if (Request.Headers.ContainsKey("HX-Request"))
    {
        return PartialView("_List", model);
    }
    return View(model);
}
```

**Option 3:** Set `Layout = null` in partial views:
```cshtml
@{
    Layout = null;  // Prevent layout from being applied
}
<div>Partial content here</div>
```

### Path needs to return full page

Add the path to the allowlist in `SwapHtmxShellMiddleware.cs`:

```csharp
private static readonly HashSet<string> AllowFullPagePaths = new(StringComparer.OrdinalIgnoreCase)
{
    "/",
    "/auth/login",
    "/auth/register",
    "/your/custom/path"  // Add your path here
};
```

## Disabling in Production

The middleware is intended for development. To disable in production:

```csharp
var app = builder.Build();

app.UseRouting();

// Only use in development
if (app.Environment.IsDevelopment())
{
    app.UseSwapHtmxShell();
}

app.UseAuthorization();
```

## Related Commands

- [swap new](./new) - Create new project (middleware not included by default)
- [swap events](./events) - Analyze event chains in your app

## Related Packages

- [Swap.Htmx](https://www.nuget.org/packages/Swap.Htmx) - Full HTMX framework with middleware, base controller, and extensions

## Notes

- Middleware is a **development tool**, not required for production
- Alternative to generating: Install `Swap.Htmx` package
- Catches layout bugs early in development
- Zero impact when HTMX headers not present
- Allowlist provides flexibility for exceptions

---

**Quick Start:**
```bash
swap g htmx-shell --add-boost
dotnet run
```
