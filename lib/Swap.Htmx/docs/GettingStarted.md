# Getting Started with Swap.Htmx

A step-by-step guide to building your first HTMX-powered ASP.NET Core application with Swap.Htmx.

## What is Swap.Htmx?

Swap.Htmx is a lightweight library that makes building HTMX applications with ASP.NET Core MVC easier by providing:

- **SwapController** - Base controller that automatically handles HTMX vs full page requests
- **Minimal API Support** - Return HTMX responses directly from Minimal API endpoints
- **Razor Pages Support** - Native integration with `PageModel`
- **Fluent response builder** - Build complex multi-part responses with a clean API
- **Event system** - Configure declarative UI updates that happen when events are triggered
- **SSE support** - Built-in real-time updates with Server-Sent Events
- **Type-safe APIs** - No magic strings for swap modes or event names

## Prerequisites

- .NET 8.0 or later
- Basic knowledge of ASP.NET Core MVC
- Basic knowledge of HTMX (or willingness to learn!)

## Installation

**1. Create a new ASP.NET Core MVC project:**

```bash
dotnet new mvc -n MyHtmxApp
cd MyHtmxApp
```

**2. Install Swap.Htmx:**

```bash
dotnet add package Swap.Htmx
```

**3. Add HTMX and Swap.Htmx to your layout:**

In `Views/Shared/_Layout.cshtml`, add HTMX and the Swap.Htmx client assets before the closing `</head>` tag:

```html
<head>
    <!-- ... existing content ... -->
    
    <!-- 1. Add Swap.Htmx Styles (for Toasts) -->
    <link rel="stylesheet" href="~/_content/Swap.Htmx/css/swap.css" />

    <!-- 2. Add HTMX -->
    <script src="https://unpkg.com/htmx.org@2.0.3"></script>
    
    <!-- 3. Add Swap.Htmx Script (for Toasts and Events) -->
    <script src="~/_content/Swap.Htmx/js/swap.js"></script>
</head>
```

## Basic Setup

**1. Register Swap.Htmx services in `Program.cs`:**

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// Add Swap.Htmx
builder.Services.AddSwapHtmx();

var app = builder.Build();

app.UseStaticFiles();
app.UseRouting();

// Add Swap.Htmx middleware
app.UseSwapHtmx();

app.MapControllers();
app.Run();
```

> **Tip:** Enable debug logging to see exactly what Swap is doing! Add `"Swap.Htmx": "Debug"` to the `Logging` section of your `appsettings.Development.json`.

**2. Create your first SwapController:**

```csharp
using Microsoft.AspNetCore.Mvc;
using Swap.Htmx;

namespace MyHtmxApp.Controllers;

public class TodosController : SwapController
{
    private static readonly List<string> _todos = new()
    {
        "Learn HTMX",
        "Install Swap.Htmx",
        "Build something awesome"
    };

    [HttpGet("/")]
    public IActionResult Index()
    {
        return SwapView(_todos);
    }
}
```

### Alternative: Minimal APIs

You can also use Swap with Minimal APIs. This is great for small apps or microservices.

```csharp
app.MapGet("/", () => SwapResults.Response().WithView("Index", _todos));

app.MapPost("/add", (string todo) => 
{
    _todos.Add(todo);
    return SwapResults.Response()
        .WithSuccessToast("Todo added!")
        .WithView("_TodoItem", todo);
});
```

### Alternative: Using Standard Controllers (Composition)

If you prefer not to inherit from `SwapController`, you can use standard ASP.NET Core controllers and access Swap features via extension methods. This is useful if you already have a base controller or want to keep your inheritance hierarchy clean.

```csharp
using Microsoft.AspNetCore.Mvc;
using Swap.Htmx; // Import extension methods

public class TodosController : Controller
{
    [HttpGet("/")]
    public IActionResult Index()
    {
        // Use extension method on standard Controller
        // Automatically handles partial vs full view based on HX-Request header
        return this.SwapView(_todos);
    }
    
    [HttpPost]
    public IActionResult Add(string todo)
    {
        _todos.Add(todo);
        
        // Use extension method to build response
        return this.SwapResponse()
            .WithSuccessToast("Todo added!")
            .Build();
    }
}
```

**3. Create the view:**

`Views/Todos/Index.cshtml`:

```html
@model List<string>

<div class="container">
    <h1>My Todos</h1>
    
    <ul id="todo-list">
        @foreach (var todo in Model)
        {
            <li>@todo</li>
        }
    </ul>
</div>
```

**That's it!** Run your app with `dotnet run` and navigate to `http://localhost:5000`.

## Understanding SwapView

The `SwapView()` method automatically detects HTMX requests and returns the appropriate response:

- **Regular requests** (browser navigation) → Returns full view with layout
- **HTMX requests** (hx-get, hx-post, etc.) → Returns partial view without layout

This means you write **one view** and it works for both cases!

## Adding HTMX Interactivity

Let's add the ability to create new todos with HTMX.

**1. Update the controller:**

```csharp
public class TodosController : SwapController
{
    private static readonly List<string> _todos = new()
    {
        "Learn HTMX",
        "Install Swap.Htmx",
        "Build something awesome"
    };

    [HttpGet("/")]
    public IActionResult Index()
    {
        return SwapView(_todos);
    }

    [HttpPost("/todos")]
    public IActionResult Create([FromForm] string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return BadRequest("Title is required");
        }

        _todos.Add(title);
        
        // Return just the updated list
        return SwapView("_TodoList", _todos);
    }
}
```

**2. Create a partial view for the list:**

`Views/Todos/_TodoList.cshtml`:

```html
@model List<string>

@foreach (var todo in Model)
{
    <li>@todo</li>
}
```

**3. Update the main view to use HTMX:**

`Views/Todos/Index.cshtml`:

```html
@model List<string>

<div class="container">
    <h1>My Todos</h1>
    
    <form hx-post="/todos" 
          hx-target="#todo-list" 
          hx-swap="innerHTML">
        <input type="text" 
               name="title" 
               placeholder="What needs to be done?" 
               required />
        <button type="submit">Add Todo</button>
    </form>
    
    <ul id="todo-list">
        @await Html.PartialAsync("_TodoList", Model)
    </ul>
</div>
```

Now when you submit the form:
1. HTMX sends a POST request to `/todos`
2. The controller adds the todo and returns the `_TodoList` partial
3. HTMX replaces the contents of `#todo-list` with the new list
4. No page reload!

## Adding Toast Notifications

Let's add a success message when a todo is created.

**1. Update the controller to use SwapResponse:**

```csharp
[HttpPost("/todos")]
public IActionResult Create([FromForm] string title)
{
    if (string.IsNullOrWhiteSpace(title))
    {
        return SwapResponse()
            .WithErrorToast("Title is required")
            .Build();
    }

    _todos.Add(title);
    
    return SwapResponse()
        .WithView("_TodoList", _todos)
        .WithSuccessToast($"Added: {title}")
        .Build();
}
```

**2. Add toast CSS and JavaScript:**

Download the toast CSS from the Swap.Htmx repository or add this to your `wwwroot/css/site.css`:

```css
/* Toast notifications */
.toast-container {
    position: fixed;
    top: 1rem;
    right: 1rem;
    z-index: 9999;
}

.toast {
    padding: 1rem;
    margin-bottom: 0.5rem;
    border-radius: 4px;
    box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
    animation: slideIn 0.3s ease-out;
}

.toast.success { background: #10b981; color: white; }
.toast.error { background: #ef4444; color: white; }
.toast.warning { background: #f59e0b; color: white; }
.toast.info { background: #3b82f6; color: white; }

@keyframes slideIn {
    from {
        transform: translateX(100%);
        opacity: 0;
    }
    to {
        transform: translateX(0);
        opacity: 1;
    }
}
```

Add toast handler to your layout (`_Layout.cshtml`):

```html
<body>
    <!-- ... existing content ... -->
    
    <div id="toast-container" class="toast-container"></div>
    
    <script>
        document.body.addEventListener('showToast', function(evt) {
            const toast = evt.detail;
            const container = document.getElementById('toast-container');
            
            const div = document.createElement('div');
            div.className = `toast ${toast.type}`;
            div.textContent = toast.message;
            
            container.appendChild(div);
            
            setTimeout(() => {
                div.style.opacity = '0';
                setTimeout(() => div.remove(), 300);
            }, 3000);
        });
    </script>
</body>
```

Now you'll see toast notifications when todos are created!

## Multi-Part Updates

What if you want to update multiple parts of the page? Let's add a todo counter.

**1. Update the controller:**

```csharp
[HttpPost("/todos")]
public IActionResult Create([FromForm] string title)
{
    if (string.IsNullOrWhiteSpace(title))
    {
        return SwapResponse()
            .WithErrorToast("Title is required")
            .Build();
    }

    _todos.Add(title);
    
    return SwapResponse()
        .WithView("_TodoList", _todos)
        .AlsoUpdate("todo-count", "_TodoCount", _todos.Count)
        .WithSuccessToast($"Added: {title}")
        .Build();
}
```

**2. Create the count partial:**

`Views/Todos/_TodoCount.cshtml`:

```html
@model int

<strong>@Model</strong> todo@(Model != 1 ? "s" : "")
```

**3. Update the main view:**

```html
@model List<string>

<div class="container">
    <h1>
        My Todos 
        <small id="todo-count">
            @await Html.PartialAsync("_TodoCount", Model.Count)
        </small>
    </h1>
    
    <!-- ... rest of the view ... -->
</div>
```

Now when you add a todo:
1. The list updates (main response)
2. The counter updates (out-of-band swap)
3. A toast shows (triggered event)

All in one response!

## Next Steps

Now that you have the basics, explore:

- **[SwapController Guide](SwapController.md)** - Learn all the controller features
- **[Out-of-Band Swaps Guide](OutOfBandSwaps.md)** - Master multi-part updates
- **[Event Chains Guide](EventChains.md)** - Configure declarative UI updates
- **[Server-Sent Events Guide](ServerSentEvents.md)** - Add real-time updates
- **[Testing Guide](Testing.md)** - Write tests for your HTMX app

## Tips for Success

1. **Start simple** - Use `SwapView()` for most actions
2. **Add complexity when needed** - Move to `SwapResponse()` for multi-part updates
3. **Use constants** - Define element IDs and view names as constants to avoid typos
4. **Think in partials** - Break your UI into small, reusable partial views
5. **Test with HTMX** - Use the Swap.Testing library to write proper integration tests

## Common Patterns

### Loading States

```html
<button hx-post="/todos" 
        hx-target="#todo-list"
        hx-indicator="#spinner">
    Add Todo
</button>

<div id="spinner" class="htmx-indicator">Loading...</div>
```

### Form Reset

```html
<form hx-post="/todos" 
      hx-target="#todo-list"
      hx-on::after-request="this.reset()">
    <!-- form fields -->
</form>
```

### Confirmation

```html
<button hx-delete="/todos/1"
        hx-confirm="Are you sure?">
    Delete
</button>
```

### Optimistic UI

```html
<button hx-post="/todos"
        hx-swap="beforeend"
        hx-target="#todo-list">
    Add Todo (Optimistic)
</button>
```

## Troubleshooting

**Views not rendering as partials:**
- Make sure you inherit from `SwapController`
- Check that HTMX is sending the `HX-Request` header

**Toasts not showing:**
- Verify the toast JavaScript is included
- Check browser console for errors
- Ensure toast container element exists

**Out-of-band swaps not working:**
- Verify target element IDs match exactly
- Check that partial views exist
- Make sure element IDs are unique on the page

## Getting Help

- Check the [demo applications](../../demo/) for working examples
- Read the [documentation](../README.md)
- Review the [test files](../Swap.Htmx.Tests/) for usage patterns
- Open an issue on GitHub

---

## Next Steps

- [SwapController Guide](SwapController.md) - Deep dive into the base controller
- [Minimal APIs Guide](MinimalApis.md) - Using Swap with Minimal APIs
- [Razor Pages Guide](RazorPages.md) - Using Swap with Razor Pages
- [Event Chains](EventChains.md) - Learn about the event system
- [Server-Sent Events](ServerSentEvents.md) - Real-time updates
