---
sidebar_position: 8
---

# HTMX Navigation & Safety

Swap projects are HTMX-first by default, providing SPA-like navigation without JavaScript frameworks.

## HTMX-First Layout

Every new Swap project includes an HTMX-optimized layout:

### Global Boost

```html
<body hx-boost="true">
```

**What it does:** Converts all link clicks and form submissions into AJAX requests automatically.

**Benefits:**
- No page reloads
- Faster navigation (only content changes)
- Maintains scroll position
- Browser history just works

### Content Target

```html
<main id="main-content" class="container mx-auto p-4">
    @RenderBody()
</main>
```

**What it does:** Defines the swap target for HTMX requests.

**Usage in links:**
```html
<a href="/Article" hx-target="#main-content" hx-push-url="true">Articles</a>
```

**Attributes:**
- `hx-target="#main-content"` - Swaps only the main content area (not full page)
- `hx-push-url="true"` - Updates browser URL and maintains history

### Navigation Links

Swap automatically generates navigation links with HTMX attributes when using `--add-nav`:

```bash
swap g c Article --fields "Title:string Content:string" --add-nav
```

**Generates:**
```html
<ul class="menu menu-horizontal px-1 ml-2">
    <li><a href="/" hx-target="#main-content" hx-push-url="true">Home</a></li>
    <li><a href="/Article" hx-target="#main-content" hx-push-url="true">Articles</a></li>
</ul>
```

## HTMX Shell Middleware

Swap includes middleware to enforce partial view responses and prevent full page reloads.

### How It Works

The middleware:
1. Detects `HX-Request` headers on incoming requests
2. Verifies response HTML doesn't contain `<html>` tags
3. Throws exception with view name if full page detected
4. Helps catch layout rendering bugs during development

**Location:** `Middleware/HtmxShellMiddleware.cs`

### Configuration

Configure the allowlist to permit full page rendering for specific paths:

```csharp
private static readonly HashSet<string> AllowFullPagePaths = new(StringComparer.OrdinalIgnoreCase)
{
    "/",              // Home page (initial load)
    "/auth/login",    // Login page
    "/auth/register", // Registration page
    "/error"          // Error pages
};
```

### Partial vs Full Page

Controllers check for HTMX requests and return appropriate views:

```csharp
public async Task<IActionResult> Index()
{
    var viewModel = await GetDataAsync();
    
    return Request.Headers.ContainsKey("HX-Request")
        ? PartialView("_List", viewModel)  // HTMX partial (no layout)
        : View(viewModel);                  // Full page (with layout)
}
```

**HTMX Request:**
- Returns: `_List.cshtml` (just the content)
- Contains: Table, modals, no `<html>` tags
- Swapped into: `#main-content`

**Initial Request (no HX-Request header):**
- Returns: `Index.cshtml` (full page)
- Contains: Layout + content
- Renders: Complete page with navbar, footer, etc.

### Disabling the Middleware

For advanced scenarios (e.g., hybrid apps, API endpoints), disable the middleware:

```bash
swap new MyApp --no-htmx-shell
```

## Build-Before-Migration Safety

Swap uses a **build-first approach** for all migration operations to prevent cryptic errors.

### How It Works

When you generate:
- A controller (`swap g controller`)
- A pattern (`swap g pattern`)
- Auth scaffolding (`swap g auth`)

The CLI:
1. Generates model and controller code
2. **Builds the project** (`dotnet build`)
3. If build **succeeds** → Creates migration (e.g., `AddProduct`)
4. If build **fails** → Shows compiler errors, stops (no migration)

### Why This Matters

**Without building first:** EF Core can fail with cryptic errors like:
- "The entity type X cannot be mapped to a table because it is derived from Y"
- "No suitable constructor found for entity type X"
- "Unable to determine the relationship represented by navigation X"

These often mask simple issues like:
- Nullable reference warnings (`DateTime?` without null-conditional operators)
- Missing using statements
- Typos in property names

**With building first:** You see clear C# compiler messages:
```
Models/Article.cs(15,19): error CS8618: Non-nullable property 'Title' must contain a non-null value when exiting constructor.
```

Much easier to fix!

### Migration Workflow

```bash
# Generate controller - migration auto-created after build
swap g c Product --fields "Name:string Price:decimal"
# Output:
# ✓ Controller generated
# ✓ Migration created: AddProduct

# Review migration (optional)
cat Migrations/20250129063505_AddProduct.cs

# Apply when ready
dotnet ef database update
```

**Key principle:** Swap **never** applies migrations automatically. You always control when schema changes hit your database.

### Migration Names

Swap uses semantic migration names:

| Generator | Migration Name |
|-----------|----------------|
| `swap new` | `InitialCreate` |
| `swap g auth` | `AddIdentity` |
| `swap g c Article` | `AddArticle` |
| `swap g pattern sluggable Article` | `AddArticleSlug` |
| `swap g pattern auditable Article` | `AddAuditableToArticle` |

These names are descriptive and follow Entity Framework conventions.

## Best Practices

### 1. Always Use HTMX Attributes on Navigation

```html
<!-- ✅ Good: Uses hx-target and hx-push-url -->
<a href="/products" hx-target="#main-content" hx-push-url="true">Products</a>

<!-- ❌ Bad: No HTMX attributes (full page reload) -->
<a href="/products">Products</a>
```

### 2. Check HX-Request in Controllers

```csharp
// ✅ Good: Returns partial for HTMX, full page for initial load
public IActionResult Index()
{
    return Request.Headers.ContainsKey("HX-Request")
        ? PartialView("_List", model)
        : View(model);
}

// ❌ Bad: Always returns full page (breaks HTMX navigation)
public IActionResult Index()
{
    return View(model);
}
```

### 3. Review Migrations Before Applying

```bash
# ✅ Good: Review then apply
swap g c Product --fields "Name:string Price:decimal"
cat Migrations/*_AddProduct.cs  # Review migration
dotnet ef database update       # Apply when ready

# ⚠️ Risky: Apply without reviewing
swap g c Product --fields "Name:string"
dotnet ef database update  # Haven't seen what's changing!
```

### 4. Test HTMX Navigation

```csharp
[Fact]
public async Task NavigationLinks_UseHtmxAttributes()
{
    var response = await _client.GetAsync("/");
    response.AssertSuccess();
    
    // Verify navigation links have HTMX attributes
    await response.AssertAttributeAsync("a[href='/articles']", "hx-target", "#main-content");
    await response.AssertAttributeAsync("a[href='/articles']", "hx-push-url", "true");
}

[Fact]
public async Task Index_ReturnsPartial_ForHtmxRequest()
{
    var response = await _client.HtmxGetAsync("/articles");
    response.AssertSuccess();
    await response.AssertPartialViewAsync();  // No <html> tag
}
```

## Troubleshooting

### Full Page Returned Instead of Partial

**Symptom:** Clicking a link replaces the entire page, including navbar.

**Cause:** Controller not checking for `HX-Request` header.

**Fix:**
```csharp
public IActionResult Index()
{
    return Request.Headers.ContainsKey("HX-Request")
        ? PartialView("_List", model)  // Add this
        : View(model);
}
```

### HTMX Shell Middleware Throwing Exceptions

**Symptom:** Exception: "HTMX request returned full page with <html> tag in view: Index"

**Cause:** Controller returning full view instead of partial.

**Fix:** Check `HX-Request` header as shown above.

**Workaround:** Add path to allowlist in `HtmxShellMiddleware.cs`:
```csharp
private static readonly HashSet<string> AllowFullPagePaths = new(StringComparer.OrdinalIgnoreCase)
{
    "/your/path/here"
};
```

### Build Errors After Generating Controller

**Symptom:** Migration not created, build errors shown.

**Cause:** Compilation error in generated code (this is intentional - prevents bad migrations!).

**Fix:** Read the compiler error and fix the issue:
```
Models/Article.cs(10,19): error CS8618: Non-nullable property 'Title' must contain a non-null value when exiting constructor.
```

Add `= "";` or `= null!;` to the property:
```csharp
public string Title { get; set; } = "";  // ✅ Fixed
```

Then run `dotnet build` to verify.

### Browser Back Button Not Working

**Symptom:** Back button doesn't navigate to previous content.

**Cause:** Missing `hx-push-url="true"` on navigation links.

**Fix:**
```html
<!-- ✅ Good: Maintains history -->
<a href="/products" hx-target="#main-content" hx-push-url="true">Products</a>

<!-- ❌ Bad: No history updates -->
<a href="/products" hx-target="#main-content">Products</a>
```

## See Also

- [HTMX Documentation](https://htmx.org/docs/)
- [DaisyUI Navbar Components](https://daisyui.com/components/navbar/)
- [Entity Framework Core Migrations](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/)
- [Swap Testing Framework](testing-framework.md)
