---
sidebar_position: 2
---

# Your First Project

Create a simple CRUD application with HTMX in under 5 minutes.

## Create a New Project

```bash
# Create with SQLite (default)
swap new BlogApp
cd BlogApp

# Or create with SQL Server
swap new BlogApp --database sqlserver
cd BlogApp

# Or create with PostgreSQL
swap new BlogApp --database postgres
cd BlogApp
```

This generates:
- ASP.NET Core MVC application
- Entity Framework Core with your chosen database
- Sample TodoItem entity with HTMX views
- DaisyUI components with Tailwind CSS
- Modal-based CRUD operations
- Toast notifications for feedback
- **Docker support** with Dockerfile and docker-compose.yml

## Run the Application

### Option 1: Run Locally

```bash
dotnet run
```

Open `http://localhost:5000` in your browser.

### Option 2: Run with Docker (Recommended)

```bash
docker-compose up --build
```

Docker will:
- Build your app with all dependencies
- Start the database (if SQL Server or PostgreSQL)
- Apply migrations automatically
- Start your app on http://localhost:5000

**Benefits of Docker:**
- ✅ No manual database setup
- ✅ Migrations run automatically
- ✅ Consistent environment across team
- ✅ Production-ready configuration

## Add a New Resource

Generate a blog post resource:

```bash
swap g r Post --fields "Title:string Content:string PublishedAt:datetime?"
```

This creates:
- `Models/Post.cs` - Entity model with properties
- `Controllers/PostController.cs` - Full CRUD controller with HTMX support
- `Views/Post/Index.cshtml` - Main list view
- `Views/Post/_List.cshtml` - Partial for HTMX updates
- `Views/Post/_AddModal.cshtml` - Add form in modal
- `Views/Post/_EditModal.cshtml` - Edit form in modal
- Updates `AppDbContext` with `DbSet<Post>`

**Features included:**
- ✅ Modal-based Create/Edit
- ✅ Inline Delete with confirmation
- ✅ Pagination (10, 25, 50, 100 items per page)
- ✅ Real-time search (500ms debounce)
- ✅ Column sorting
- ✅ Toast notifications

## Apply Database Changes

### Running Locally

```bash
dotnet ef migrations add AddPost
dotnet ef database update
```

### Running with Docker

```bash
# Just rebuild - migrations apply automatically!
docker-compose up --build
```

**Note:** Docker containers auto-apply migrations on startup, so you only need to create the migration file with `dotnet ef migrations add AddPost` on your host machine. The update happens automatically in the container.

## Add Entity Patterns (Optional)

Swap provides battle-tested entity patterns you can apply to any model:

### Soft Delete Pattern

Keep deleted records in the database for audit trails:

```bash
# Apply soft delete to Post entity
swap g pattern softdelete Post

# Configure DbContext (add to OnModelCreating)
# modelBuilder.ConfigureSoftDeleteFilter();

# Generate migration
dotnet ef migrations add AddSoftDeleteToPost
dotnet ef database update
```

Now your posts can be soft-deleted:

```csharp
// In controller
post.SoftDelete("user@example.com");
await _db.SaveChangesAsync();

// Restore if needed
post.Restore();
await _db.SaveChangesAsync();

// Normal queries automatically exclude deleted
var posts = await _db.Posts.ToListAsync();

// Query deleted posts explicitly
var deleted = await _db.Posts.OnlyDeleted().ToListAsync();
```

**When to use:**
- Compliance requirements (GDPR, HIPAA)
- Audit trails for deleted data
- Ability to undo deletions
- Data recovery scenarios

[Learn more about Entity Patterns →](../features/patterns)

## Test Your Features

Restart the app and navigate to `http://localhost:5000/Post`:

- **Create** - Click "Add New Post" to open a modal form
- **List** - View paginated posts with sorting
- **Search** - Type in the search box (auto-searches after 500ms)
- **Edit** - Click edit button to open modal
- **Delete** - Click delete with confirmation dialog
- **Sort** - Click column headers to sort
- **Pagination** - Choose page size and navigate pages
- **Details** - View single post

## How HTMX Works

The generated views use HTMX for dynamic updates without JavaScript:

```html
<!-- Index.cshtml -->
<div hx-get="/Post/List" hx-trigger="load" hx-target="#post-list">
    <div id="post-list">Loading...</div>
</div>
```

When the page loads, HTMX:
1. Makes a GET request to `/Post/List`
2. Gets back HTML fragment from `_PostList.cshtml`
3. Swaps it into `#post-list` div
4. No page reload needed

## Customize Your Views

The generated views use DaisyUI components. Edit `Views/Post/Index.cshtml` to customize:

```cshtml
@model PostListViewModel

<div class="flex justify-between items-center mb-6">
    <h1 class="text-3xl font-bold">Blog Posts</h1>
    <button class="btn btn-primary" 
            hx-get="@Url.Action("Add")" 
            hx-target="#modal-container">
        Add New Post
    </button>
</div>

<div hx-get="@Url.Action("List")" 
     hx-trigger="load" 
     hx-target="#post-list">
    <div id="post-list">
        <span class="loading loading-spinner loading-lg"></span>
    </div>
</div>
```

Edit `Views/Post/_List.cshtml` for the table:

```cshtml
@model PostListViewModel

<div class="overflow-x-auto">
    <table class="table table-zebra">
        <thead>
            <tr>
                <th>Title</th>
                <th>Content</th>
                <th>Published</th>
                <th class="text-right">Actions</th>
            </tr>
        </thead>
        <tbody>
            @foreach (var post in Model.Items)
            {
                <tr>
                    <td>@post.Title</td>
                    <td>@post.Content?.Substring(0, Math.Min(50, post.Content.Length ?? 0))...</td>
                    <td>@post.PublishedAt?.ToString("MMM dd, yyyy")</td>
                    <td class="text-right">
                        <button class="btn btn-sm btn-ghost" 
                                hx-get="@Url.Action("Edit", new { id = post.Id })"
                                hx-target="#modal-container">
                            Edit
                        </button>
                        <button class="btn btn-sm btn-error" 
                                hx-delete="@Url.Action("Delete", new { id = post.Id })"
                                hx-confirm="Delete this post?">
                            Delete
                        </button>
                    </td>
                </tr>
            }
        </tbody>
    </table>
</div>
```

## What You Get

Every generated controller includes:

**UI Components:**
- ✅ DaisyUI buttons, forms, tables, modals
- ✅ Tailwind utilities for layout and spacing
- ✅ Loading spinners and animations
- ✅ Responsive design out of the box

**Interactive Features:**
- ✅ Modal CRUD (no page reloads)
- ✅ Toast notifications (success/error)
- ✅ Search with debouncing
- ✅ Sortable columns
- ✅ Pagination controls

**Developer Experience:**
- ✅ Server-side validation
- ✅ Async/await patterns
- ✅ Type-safe models
- ✅ Clean, readable code

## Next Steps

- [CLI Overview](../cli/overview) - Complete command reference
- [Generate Controller](../cli/generate-controller) - Deep dive into controller generation
- [Pagination](../features/pagination) - Learn about pagination features
- [Sorting](../features/sorting) - Configure sortable columns
- [Filtering](../features/filtering) - Add filters to your views
