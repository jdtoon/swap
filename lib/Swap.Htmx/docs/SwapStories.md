# SwapStories: Component Playground

SwapStories is a built-in "Storybook-like" environment for developing, testing, and visualizing your Razor partials in isolation. It allows you to build UI components without navigating through your entire application.

---

## Quick Start

### 1. Enable in Program.cs

SwapStories is **Development-only** by default. It automatically disables itself in production.

```csharp
// Program.cs
if (app.Environment.IsDevelopment())
{
    app.UseSwapStories(); // Serves dashboard at /_swap/stories
}
```

### 2. Create a Story

Add the `[SwapStory]` attribute to any controller action that returns a PartialView (or any IActionResult).

```csharp
public class ComponentsController : SwapController
{
    [SwapStory("Primary Button", "Buttons")]
    public IActionResult PrimaryButton()
    {
        return PartialView("_Button", new ButtonModel { Text = "Click Me", Type = "Primary" });
    }
}
```

### 3. View the Dashboard

Run your app and navigate to:
`http://localhost:5000/_swap/stories`

---

## Features

- **Auto-Discovery**: Automatically finds all actions marked with `[SwapStory]`.
- **Categorization**: Group stories by category (e.g., "Cards", "Forms", "Navigation").
- **Viewport Testing**: Define width/height to test responsiveness.
- **Hot Reload**: Modify your `.cshtml` file content and refresh the iframe to see changes instantly.
- **Zero Configuration**: No separate build process or configuration files needed.

---

## detailed Usage

### The `[SwapStory]` Attribute

```csharp
[SwapStory(
    Title = "My Component", 
    Category = "Group Name", 
    Description = "Optional description",
    Width = 400,   // Optional: Iframe width
    Height = 300   // Optional: Iframe height
)]
```

### dedicated Story Controller

While you can place stories on any controller, it's often cleaner to create a dedicated controller (or controllers) for them.

```csharp
// Controllers/StoriesController.cs
public class StoriesController : SwapController
{
    // These actions are only used by the playground
    
    [SwapStory("User Profile Card", "Cards")]
    public IActionResult UserProfile() 
        => PartialView("_UserProfile", new User { Name = "Alice", Role = "Admin" });

    [SwapStory("Empty State", "Cards")]
    public IActionResult UserProfileEmpty() 
        => PartialView("_UserProfile", null);
}
```

### Isolating Components

Stories encourage you to build **pure partials** — views that rely only on their model, not on `ViewBag`, `HttpContext`, or global state.

**Bad Partial (Hard to test):**
```html
<!-- _Sidebar.cshtml -->
<div class="user">Hello @User.Identity.Name</div> <!-- Depends on HttpContext -->
```

**Good Partial (Easy to test):**
```html
<!-- _Sidebar.cshtml -->
@model SidebarViewModel
<div class="user">Hello @Model.UserName</div> <!-- Depends only on Model -->
```

---

## Troubleshooting

### 404 Not Found
- Ensure you are in the **Development** environment.
- Ensure `app.UseSwapStories()` is called in `Program.cs`.

### Styles Missing
- Your partials are rendered in an iframe. They need access to your CSS.
- By default, the iframe loads the partial directly. You may need to ensure your partial includes a reference to your CSS, or use a "Layout" partial that includes `<link rel="stylesheet">`.

**Tip:** Create a `_StoryLayout.cshtml` that includes your CSS, and have your story actions return a view that uses it.

```csharp
public IActionResult CardStory()
{
    // _StoryWrapper just renders Layout + Body
    return View("_StoryWrapper", model); 
}
```
