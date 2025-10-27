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
- **Entity Framework Core** with SQLite (or your choice)
- **HTMX** for interactive UI without JavaScript
- **DaisyUI + Tailwind CSS** for modern, accessible components
- **Sample TodoItem CRUD** with modals, pagination, and search
- **Toast notifications** for user feedback
- **Production-ready patterns** from real applications
- Ready to run immediately

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
│   └── TodoItemController.cs    # Sample CRUD with all features
├── Models/
│   ├── TodoItem.cs              # Sample entity
│   └── TodoItemListViewModel.cs # View model with pagination
├── Views/
│   ├── TodoItem/
│   │   ├── Index.cshtml         # Main view with search and filters
│   │   ├── _List.cshtml         # HTMX partial with table
│   │   ├── _AddModal.cshtml     # Create form in modal
│   │   └── _EditModal.cshtml    # Edit form in modal
│   ├── Home/
│   │   └── Index.cshtml         # Welcome page
│   └── Shared/
│       ├── _Layout.cshtml       # DaisyUI layout with navbar
│       └── _Pagination.cshtml   # Reusable pagination component
├── Data/
│   ├── AppDbContext.cs
│   └── Migrations/
├── wwwroot/
│   ├── lib/
│   │   ├── htmx/                # HTMX library
│   │   └── toastify-js/         # Toast notifications
│   └── css/
│       ├── tailwind.css         # Generated Tailwind CSS
│       └── site.css             # Custom styles
├── tailwind.config.js           # Tailwind + DaisyUI configuration
├── appsettings.json             # Connection strings
├── Program.cs                   # App configuration
└── MyApp.csproj
```

## Sample Code

The generated TodoItem controller demonstrates all patterns:

```csharp
public async Task<IActionResult> Index(
    int pageNumber = 1, 
    int pageSize = 10,
    string? searchTerm = null,
    string? sortBy = null,
    string? sortOrder = "asc")
{
    var query = _context.TodoItems.AsQueryable();
    
    // Search
    if (!string.IsNullOrWhiteSpace(searchTerm))
        query = query.Where(t => t.Title.Contains(searchTerm));
    
    // Sort
    query = sortBy switch {
        "Title" => sortOrder == "desc" ? query.OrderByDescending(t => t.Title) : query.OrderBy(t => t.Title),
        "IsComplete" => sortOrder == "desc" ? query.OrderByDescending(t => t.IsComplete) : query.OrderBy(t => t.IsComplete),
        _ => query.OrderBy(t => t.Id)
    };
    
    // Pagination
    var totalItems = await query.CountAsync();
    var items = await query
        .Skip((pageNumber - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();
    
    var viewModel = new TodoItemListViewModel {
        Items = items,
        Pagination = new PaginationDto { /* ... */ }
    };
    
    return Request.Headers.ContainsKey("HX-Request")
        ? PartialView("_List", viewModel)
        : View(viewModel);
}
```

The Index view uses HTMX for zero-reload interactions:

```html
<!-- Search bar -->
<input type="text" 
       class="input input-bordered"
       hx-get="@Url.Action("Index")" 
       hx-trigger="keyup changed delay:500ms"
       hx-target="#todo-list"
       placeholder="Search..." />

<!-- Todo list container -->
<div id="todo-list" 
     hx-get="@Url.Action("Index")" 
     hx-trigger="load"
     hx-include="[name='searchTerm']">
    <div class="flex justify-center p-8">
        <span class="loading loading-spinner loading-lg"></span>
    </div>
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
