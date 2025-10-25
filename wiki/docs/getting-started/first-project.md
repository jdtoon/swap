---
sidebar_position: 2
---

# Your First Project

Create a simple CRUD application with HTMX in under 5 minutes.

## Create a New Project

```bash
swap new BlogApp
cd BlogApp
```

This generates:
- ASP.NET Core MVC application
- Entity Framework Core with SQLite
- Sample Todo entity with HTMX views
- Bootstrap 5 UI

## Run the Application

```bash
dotnet run
```

Open `http://localhost:5000` in your browser. You'll see the Todo CRUD interface powered by HTMX.

## Add a New Resource

Generate a blog post resource:

```bash
swap g r Post --fields Title:string,Content:string,PublishedAt:datetime?
```

This creates:
- `Models/Post.cs` - Entity model
- `Controllers/PostController.cs` - CRUD controller
- `Views/Post/` - HTMX views (Index, Create, Edit, Delete, Details)
- Updates `AppDbContext` with `DbSet<Post>`

## Apply Database Changes

```bash
dotnet ef migrations add AddPost
dotnet ef database update
```

## Test Your CRUD

Restart the app and navigate to `http://localhost:5000/Post`:

- **Create** - Add new posts
- **List** - View all posts (HTMX-powered table)
- **Edit** - Update posts inline
- **Delete** - Remove posts with confirmation
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

Edit `Views/Post/Index.cshtml` to change the layout:

```cshtml
@model IEnumerable<BlogApp.Models.Post>

<h1>Blog Posts</h1>

<a asp-action="Create" class="btn btn-primary mb-3">New Post</a>

<div hx-get="@Url.Action("List")" hx-trigger="load" hx-target="#post-list">
    <div id="post-list">
        <p>Loading posts...</p>
    </div>
</div>
```

Edit `Views/Post/_PostList.cshtml` for the table:

```cshtml
@model IEnumerable<BlogApp.Models.Post>

<table class="table table-striped">
    <thead>
        <tr>
            <th>Title</th>
            <th>Published</th>
            <th></th>
        </tr>
    </thead>
    <tbody>
        @foreach (var post in Model)
        {
            <tr>
                <td>@post.Title</td>
                <td>@post.PublishedAt?.ToString("MMM dd, yyyy")</td>
                <td>
                    <a asp-action="Edit" asp-route-id="@post.Id">Edit</a>
                    <a asp-action="Delete" asp-route-id="@post.Id">Delete</a>
                </td>
            </tr>
        }
    </tbody>
</table>
```

## Next Steps

- [swap generate model](../cli/generate-model) - Learn about field types and options
- [swap generate controller](../cli/generate-controller) - Understand generated controllers
- [HTMX Patterns](../cli/overview) - See common HTMX patterns in generated views
