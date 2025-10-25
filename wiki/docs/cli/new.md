---
sidebar_position: 2
---

# swap new

Create a new ASP.NET Core project with HTMX.

## Synopsis

```bash
swap new <name>
```

## Description

Generates an ASP.NET Core MVC project with:
- Entity Framework Core (SQLite)
- HTMX library included
- Bootstrap 5 UI
- Sample Todo CRUD with HTMX views
- Ready to run

## Example

```bash
swap new MyApp
cd MyApp
dotnet run
```

Navigate to `http://localhost:5000` to see the Todo CRUD interface.

## Generated Structure

```
MyApp/
├── Controllers/
│   ├── HomeController.cs
│   └── TodoController.cs        # Sample CRUD controller
├── Models/
│   └── TodoItem.cs              # Sample entity
├── Views/
│   ├── Todo/
│   │   ├── Index.cshtml         # Main view with HTMX
│   │   ├── _TodoList.cshtml     # HTMX partial
│   │   ├── Create.cshtml
│   │   ├── Edit.cshtml
│   │   └── Delete.cshtml
│   └── Shared/
│       └── _Layout.cshtml       # Includes HTMX script
├── Data/
│   ├── AppDbContext.cs
│   └── Migrations/
├── wwwroot/
│   └── lib/
│       ├── bootstrap/
│       └── htmx/                # HTMX library
├── Program.cs
└── MyApp.csproj
```

## Sample Code

The generated Todo controller demonstrates HTMX patterns:

```csharp
public async Task<IActionResult> Index()
{
    return View();
}

public async Task<IActionResult> List()
{
    var todos = await _context.TodoItems.ToListAsync();
    return PartialView("_TodoList", todos);
}
```

The Index view loads data via HTMX:

```html
<div hx-get="@Url.Action("List")" 
     hx-trigger="load" 
     hx-target="#todo-list">
    <div id="todo-list">Loading...</div>
</div>
```

## Next Steps

After creating your project:

```bash
# Create initial migration
dotnet ef migrations add InitialCreate

# Apply migration
dotnet ef database update

# Run the app
dotnet run
```

Then start generating your own resources:

```bash
swap g r Product --fields Name:string,Price:decimal
```

## See Also

- [Your First Project](../getting-started/first-project) - Complete tutorial
- [swap generate resource](./generate-resource) - Generate models and controllers
