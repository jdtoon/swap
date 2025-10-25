---
sidebar_position: 2
---

# Your First Project

Learn how to create and run your first NetMX application in under 5 minutes.

## Create a New Project

Let's create a simple blog application:

```bash
swap new BlogApp --template monolith --database sqlite
```

This command creates:
- ASP.NET Core web application
- Entity Framework Core with SQLite
- Complete folder structure
- Sample models and controllers
- Ready-to-run configuration

### What Just Happened?

The CLI created a project structure:

```
BlogApp/
├── Data/
│   ├── AppDbContext.cs          # EF Core database context
│   └── Migrations/               # Database migrations
├── Models/
│   └── Todo.cs                   # Sample entity
├── Controllers/
│   ├── HomeController.cs
│   └── TodoController.cs         # CRUD controller
├── Views/
│   ├── Home/
│   ├── Todo/                     # CRUD views
│   └── Shared/
├── Program.cs                    # Application entry point
├── appsettings.json
└── BlogApp.csproj
```

## Run Your Application

Navigate to the project and start it:

```bash
cd BlogApp
dotnet run
```

You should see:

```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
```

Open your browser to `http://localhost:5000` and explore the application!

## Database Migrations

Your project includes a sample `Todo` entity. Let's apply the initial migration:

```bash
# Create the first migration
dotnet ef migrations add InitialCreate

# Apply migration to database
dotnet ef database update
```

The SQLite database file (`app.db`) is now created in your project root.

## Add Your First Model

Let's add a `Post` model for our blog:

```bash
swap generate model Post --fields Title:string,Content:string,PublishedAt:datetime?
```

This generates:

```csharp title="Models/Post.cs"
namespace BlogApp.Models;

public class Post
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public required string Content { get; set; }
    public DateTime? PublishedAt { get; set; }
}
```

The model is automatically registered in `AppDbContext`:

```csharp
public DbSet<BlogApp.Models.Post> Posts { get; set; }
```

## Generate CRUD Controller

Now create a controller with full CRUD operations:

```bash
swap generate controller Post
```

This creates:
- `Controllers/PostController.cs` - Complete CRUD actions (Index, Create, Edit, Delete, Details)
- `Views/Post/` folder with all views (Index.cshtml, Create.cshtml, Edit.cshtml, Details.cshtml)

## Apply Database Changes

Create and apply a migration for the new model:

```bash
dotnet ef migrations add AddPost
dotnet ef database update
```

## Test Your CRUD Operations

Restart your application:

```bash
dotnet run
```

Navigate to `http://localhost:5000/Post` and try:

1. **Create** - Add new blog posts
2. **List** - View all posts
3. **Edit** - Update existing posts
4. **Delete** - Remove posts
5. **Details** - View a single post

## Understanding the Generated Code

### Model (`Post.cs`)

- **Id** - Primary key (always included)
- **Title** - Required string field
- **Content** - Required string field
- **PublishedAt** - Nullable DateTime field

### Controller (`PostController.cs`)

The generated controller includes:

```csharp
// List all posts
public async Task<IActionResult> Index()
{
    var posts = await _context.Posts.ToListAsync();
    return View(posts);
}

// Show create form
public IActionResult Create()
{
    return View();
}

// Save new post
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Create(Post post)
{
    if (ModelState.IsValid)
    {
        _context.Add(post);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
    return View(post);
}

// ... Edit, Delete, Details methods
```

### Views

All views use Bootstrap 5 for styling and include:
- Form validation
- CSRF protection
- Responsive design
- Accessible markup

## Customize Your Application

### Update the Model

Add more fields to your model:

```bash
swap generate model Post --fields Title:string,Content:string,PublishedAt:datetime?,AuthorName:string,ViewCount:int
```

:::tip
This regenerates the model with the new fields. The CLI warns if the file already exists.
:::

### Modify Views

Edit `Views/Post/Index.cshtml` to customize the display:

```cshtml
<h1>Blog Posts</h1>

<table class="table">
    <thead>
        <tr>
            <th>Title</th>
            <th>Author</th>
            <th>Published</th>
            <th>Views</th>
            <th></th>
        </tr>
    </thead>
    <tbody>
        @foreach (var post in Model)
        {
            <tr>
                <td>@post.Title</td>
                <td>@post.AuthorName</td>
                <td>@post.PublishedAt?.ToString("MMM dd, yyyy")</td>
                <td>@post.ViewCount</td>
                <td>
                    <a asp-action="Edit" asp-route-id="@post.Id">Edit</a> |
                    <a asp-action="Details" asp-route-id="@post.Id">Details</a> |
                    <a asp-action="Delete" asp-route-id="@post.Id">Delete</a>
                </td>
            </tr>
        }
    </tbody>
</table>
```

### Add Business Logic

Enhance your controller with custom logic:

```csharp title="Controllers/PostController.cs"
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Create(Post post)
{
    if (ModelState.IsValid)
    {
        // Set defaults
        post.PublishedAt = DateTime.UtcNow;
        post.ViewCount = 0;
        
        _context.Add(post);
        await _context.SaveChangesAsync();
        
        return RedirectToAction(nameof(Index));
    }
    return View(post);
}

public async Task<IActionResult> Details(int? id)
{
    if (id == null) return NotFound();
    
    var post = await _context.Posts.FindAsync(id);
    if (post == null) return NotFound();
    
    // Increment view count
    post.ViewCount++;
    await _context.SaveChangesAsync();
    
    return View(post);
}
```

## Common Workflows

### Add Multiple Models

Build out your domain quickly:

```bash
# Create related models
swap generate model Author --fields Name:string,Email:string,Bio:string?
swap generate model Comment --fields PostId:int,AuthorName:string,Content:string,CreatedAt:datetime

# Generate controllers
swap generate controller Author
swap generate controller Comment
```

### Change Database Provider

Switch from SQLite to SQL Server:

1. Update `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=BlogApp;Trusted_Connection=true;MultipleActiveResultSets=true"
  }
}
```

2. Update `Program.cs`:

```csharp
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
```

3. Install SQL Server package:

```bash
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
```

4. Recreate migrations:

```bash
rm -rf Data/Migrations
dotnet ef migrations add InitialCreate
dotnet ef database update
```

## Next Steps

Now that you've built your first application, explore more advanced features:

- [Generate Command Reference](../cli/generate) - Deep dive into code generation
- [Model Generation](../cli/generate-model) - Advanced field types and relationships
- [Controller Generation](../cli/generate-controller) - Customize CRUD operations
- [Modular Architecture](../architecture/modular) - Build scalable applications

## Troubleshooting

### Port Already in Use

If port 5000 is taken, specify a different port:

```bash
dotnet run --urls "http://localhost:5001"
```

### Database Locked (SQLite)

Stop all running instances of your app before applying migrations.

### Migration Errors

Reset migrations if needed:

```bash
dotnet ef database drop --force
rm -rf Data/Migrations
dotnet ef migrations add InitialCreate
dotnet ef database update
```
