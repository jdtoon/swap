# Developer Experience (DX) Guide

**Last Updated**: October 29, 2025  
**Version**: 0.0.1  
**Focus**: Making Swap delightful to use

---

## 🎯 Philosophy

Swap is designed around three core DX principles:

1. **Convention over Configuration**: Sensible defaults, minimal setup
2. **Fast Feedback Loops**: See changes immediately
3. **Progressive Disclosure**: Simple to start, powerful when needed

---

## 🚀 Quick Start (2 Minutes)

### 1. Install the CLI
```bash
dotnet tool install --global Swap.CLI --add-source .nuget/local
```

### 2. Create a New Project
```bash
swap new MyApp --database sqlite
cd MyApp
```

### 3. Generate Your First Feature
```bash
swap generate controller Task --fields "Title:string Description:string? Priority:int DueDate:DateTime?"
```

### 4. Run Migrations
```bash
dotnet ef database update
```

### 5. Run the App
```bash
dotnet run
```

Visit `http://localhost:5000` and see your fully-functional CRUD interface!

**That's it. No configuration files. No boilerplate. Just working code.**

---

## 📁 Project Structure

Swap generates a clean, organized structure:

```
MyApp/
├── Controllers/               # Your controllers
│   ├── HomeController.cs     # Demo todo list
│   └── TaskController.cs     # Generated CRUD controller
├── Models/                    # Your entities
│   ├── TodoItem.cs           # Demo model
│   └── Task.cs               # Generated model
├── ViewModels/                # List view models
│   └── TaskListViewModel.cs  # Pagination, sorting, filtering
├── Views/
│   ├── Home/
│   │   ├── Index.cshtml      # Home page container
│   │   ├── _TodoList.cshtml  # Todo list component
│   │   └── _TodoItem.cshtml  # Individual todo item
│   ├── Task/
│   │   ├── Index.cshtml              # Task page container
│   │   ├── _TaskList.cshtml          # Task list component
│   │   ├── _TaskCreateModal.cshtml   # Create modal
│   │   ├── _TaskEditModal.cshtml     # Edit modal
│   │   ├── _TaskDetailsModal.cshtml  # Details modal
│   │   └── _TaskDeleteModal.cshtml   # Delete confirmation
│   └── Shared/
│       ├── _Layout.cshtml            # App shell
│       ├── _PaginationControls.cshtml # Reusable pagination
│       └── _ValidationScripts.cshtml  # Client validation
├── Data/
│   └── AppDbContext.cs        # EF Core context
├── Extensions/
│   └── SessionExtensions.cs   # Helper extensions
├── wwwroot/                   # Static files
│   ├── css/
│   │   └── app.css           # Compiled Tailwind CSS
│   └── lib/                  # Client libraries (HTMX, etc.)
├── Migrations/                # EF Core migrations
├── appsettings.json          # Configuration
├── Program.cs                # App startup
├── nuget.config              # Local package feed
└── MyApp.csproj              # Project file
```

**Every file has a purpose. No cruft.**

---

## 🎨 Development Workflow

### Local Development with Hot Reload

1. **Start the app with watch mode:**
   ```bash
   dotnet watch run
   ```

2. **Make changes to any file:**
   - Controllers: Automatic recompilation
   - Views: Instant refresh (no rebuild)
   - CSS: Rebuild Tailwind and refresh
   - Static files: Instant refresh

3. **See changes immediately in browser**

### Tailwind CSS Development

Tailwind is configured for JIT (Just-In-Time) mode:

```bash
# Watch mode (auto-rebuild on changes)
npx tailwindcss -i ./wwwroot/css/app.css -o ./wwwroot/css/app.min.css --watch

# Production build
npx tailwindcss -i ./wwwroot/css/app.css -o ./wwwroot/css/app.min.css --minify
```

### Database Workflow

```bash
# Create a new migration
dotnet ef migrations add AddTaskPriority

# Apply migrations
dotnet ef database update

# Rollback to previous migration
dotnet ef database update PreviousMigrationName

# Remove last migration (if not applied)
dotnet ef migrations remove

# Generate SQL script
dotnet ef migrations script > migration.sql
```

---

## 🛠️ CLI Commands

### Create New Project
```bash
swap new <ProjectName> [options]

Options:
  --database <type>    Database provider (sqlite, postgres, sqlserver) [default: sqlite]
  --minimal           Skip demo content (todo list)
  --docker            Include Dockerfile and docker-compose
```

**Examples:**
```bash
# SQLite with demo content
swap new Blog --database sqlite

# PostgreSQL without demo
swap new CRM --database postgres --minimal

# With Docker support
swap new ECommerce --database postgres --docker
```

---

### Generate Controller
```bash
swap generate controller <EntityName> [options]

Options:
  --fields, -f <fields>    Field definitions (e.g., "Title:string Description:string?")
  --add-nav               Add navigation link to _Layout.cshtml
  --no-migrations         Skip automatic migration creation
  --dry-run               Preview files without creating them
  --force                 Overwrite existing files
```

**Examples:**
```bash
# Basic CRUD
swap generate controller Product --fields "Name:string Price:decimal Stock:int"

# With navigation link
swap generate controller Customer --fields "Name:string Email:string Phone:string?" --add-nav

# Preview without creating files
swap generate controller Order --fields "OrderNumber:string Total:decimal" --dry-run
```

**Field Types Supported:**
- `string` - Non-nullable string (required)
- `string?` - Nullable string (optional)
- `int`, `long`, `decimal`, `double`, `float` - Numbers
- `bool` - Boolean
- `DateTime`, `DateTime?` - Dates
- `Guid` - Unique identifiers

---

### Generate Other Artifacts

```bash
# Generate model only
swap generate model Customer --fields "Name:string Email:string"

# Generate factory for testing
swap generate factory Product

# Generate seeder
swap generate seed SampleProducts
```

---

## 🧪 Testing

### Unit Tests

Swap includes `Swap.Testing` package for HTMX testing:

```csharp
public class TaskControllerTests : IClassFixture<HtmxTestFixture<Program>>
{
    private readonly HtmxTestClient _client;
    
    public TaskControllerTests(HtmxTestFixture<Program> fixture)
    {
        _client = fixture.CreateClient();
    }
    
    [Fact]
    public async Task Index_ReturnsFullView_WhenNotHtmxRequest()
    {
        // Act
        var response = await _client.GetAsync("/Task");
        
        // Assert
        response.Should().ContainHtml("<!DOCTYPE html>"); // Full page
        response.Should().ContainElement("#main-content");
    }
    
    [Fact]
    public async Task Index_ReturnsPartial_WhenHtmxRequest()
    {
        // Act
        var response = await _client.HxGet("/Task");
        
        // Assert
        response.Should().NotContainHtml("<!DOCTYPE html>"); // No layout
        response.Should().ContainElement(".hero"); // Page content
    }
    
    [Fact]
    public async Task GetTaskList_ReturnsList()
    {
        // Act
        var response = await _client.HxGet("/Task/GetTaskList");
        
        // Assert
        response.Should().ContainElement("#task-list");
        response.Should().HaveTriggered("taskListLoaded");
    }
}
```

### Integration Tests

```csharp
[Fact]
public async Task CreateTask_AddsToDatabase()
{
    // Arrange
    var task = new { Title = "Test Task", Priority = 1 };
    
    // Act
    var response = await _client.HxPost("/Task/Create", task);
    
    // Assert
    response.Should().HaveTriggered("refreshTaskList");
    
    var tasks = await _context.Tasks.ToListAsync();
    tasks.Should().Contain(t => t.Title == "Test Task");
}
```

---

## 🎯 Best Practices

### Controller Actions

**✅ DO:**
```csharp
// Use SwapView for Index actions
public IActionResult Index()
{
    return SwapView();
}

// Separate endpoints for components
[HttpGet]
public async Task<IActionResult> GetTaskList(string? searchTerm = null)
{
    var tasks = await _context.Tasks
        .Where(t => string.IsNullOrEmpty(searchTerm) || t.Title.Contains(searchTerm))
        .ToListAsync();
    
    return PartialView("_TaskList", tasks);
}

// Trigger events for refreshes
[HttpPost]
public async Task<IActionResult> Create(Task model)
{
    _context.Tasks.Add(model);
    await _context.SaveChangesAsync();
    
    Response.Headers.Append("HX-Trigger", "refreshTaskList");
    return Content("");
}
```

**❌ DON'T:**
```csharp
// Don't manually check HTMX headers
public IActionResult Index()
{
    if (Request.Headers.ContainsKey("HX-Request"))
        return PartialView();
    return View();
}

// Don't load data in Index action
public async Task<IActionResult> Index()
{
    var tasks = await _context.Tasks.ToListAsync(); // ❌ Slow!
    return SwapView(tasks);
}

// Don't return full lists in Create actions
[HttpPost]
public async Task<IActionResult> Create(Task model)
{
    _context.Tasks.Add(model);
    await _context.SaveChangesAsync();
    
    var allTasks = await _context.Tasks.ToListAsync(); // ❌ Wasteful!
    return PartialView("_TaskList", allTasks);
}
```

---

### View Architecture

**✅ DO:**
```html
<!-- Index.cshtml: Static container -->
<div class="hero">
    <h1>Tasks</h1>
</div>

<div class="card">
    <button hx-get="@Url.Action("Create")">Add Task</button>
    
    <!-- Dynamic component loads separately -->
    <div hx-get="@Url.Action("GetTaskList")" hx-trigger="load">
        Loading...
    </div>
</div>

<!-- _TaskList.cshtml: Dynamic component -->
<div id="task-list" 
     hx-get="@Url.Action("GetTaskList")" 
     hx-trigger="refreshTaskList from:body"
     hx-swap="outerHTML">
    @foreach (var task in Model)
    {
        <div>@task.Title</div>
    }
</div>
```

**❌ DON'T:**
```html
<!-- Don't mix concerns -->
<div id="task-list">
    <div class="hero"><!-- ❌ Static content in dynamic component --></div>
    <button hx-get="@Url.Action("Create")">Add</button>
    
    @foreach (var task in Model)
    {
        <div>@task.Title</div>
    }
</div>

<!-- Don't nest list directly without container -->
@foreach (var task in Model)  <!-- ❌ No container! -->
{
    <div hx-trigger="refreshTaskList from:body">@task.Title</div>
}
```

---

### HTMX Patterns

**✅ DO:**
```html
<!-- Use outerHTML for component self-replacement -->
<div id="task-list" hx-swap="outerHTML">

<!-- Trigger custom events for decoupled updates -->
hx-trigger="refreshTaskList from:body"

<!-- Include form state for searches -->
hx-include="[name='searchTerm'], [name='sortBy']"

<!-- Use hx-push-url for navigation -->
<a hx-get="/Tasks" hx-target="#main-content" hx-push-url="true">Tasks</a>

<!-- Debounce search inputs -->
<input hx-trigger="input changed delay:500ms, search">

<!-- Show loading indicators -->
<div hx-indicator=".htmx-indicator">
```

**❌ DON'T:**
```html
<!-- Don't use innerHTML when replacing entire component -->
<div id="task-list" hx-swap="innerHTML">  <!-- ❌ Loses container! -->

<!-- Don't create circular dependencies -->
<div hx-get="/Task/Index" hx-trigger="load">  <!-- ❌ Calls page that calls itself -->

<!-- Don't forget to preserve state -->
<input hx-get="/Search">  <!-- ❌ Loses sort/filter state -->

<!-- Don't skip history management -->
<a hx-get="/Tasks" hx-target="#main-content">  <!-- ❌ Browser back button broken -->
```

---

## 🐛 Debugging Tips

### HTMX Debugging

Enable HTMX logging:
```html
<script>
    htmx.logAll();
</script>
```

### View Browser Network Tab

Look for:
- `HX-Request: true` header on HTMX requests
- `HX-Trigger` headers in responses
- Response type (HTML vs redirect)

### Common Issues

**Issue**: List doesn't refresh after create
- ✅ Check controller returns `HX-Trigger` header
- ✅ Verify list has `hx-trigger="refresh{Entity}List from:body"`
- ✅ Ensure list container has unique `id`

**Issue**: Navigation shows full page instead of swapping
- ✅ Confirm `SwapController` base class
- ✅ Check `SwapView()` is used (not `View()`)
- ✅ Verify menu link has `hx-target="#main-content"`

**Issue**: Form doesn't clear after submit
- ✅ Add `hx-on::after-request="this.reset()"`
- ✅ Or trigger form reset in response: `HX-Trigger: resetForm`

---

## 📦 Package Management

### Local Development Workflow

Use the included scripts for local package development:

```powershell
# Pack all packages to local NuGet feed
.\scripts\pack-local.ps1

# Reinstall CLI from local feed
.\scripts\reinstall-cli.ps1
```

### Using Local Packages in Your App

Your generated apps automatically include `nuget.config`:

```xml
<configuration>
  <packageSources>
    <add key="local-dev" value="../../.nuget/local" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
  
  <packageSourceMapping>
    <packageSource key="local-dev">
      <package pattern="Swap.*" />
    </packageSource>
  </packageSourceMapping>
</configuration>
```

This ensures Swap packages are loaded from your local feed, allowing rapid iteration.

---

## 🎓 Learning Path

### Day 1: Basics
1. Create a new project: `swap new MyApp`
2. Explore the generated code
3. Run the app and play with the todo list
4. Generate a simple controller: `swap generate controller Note --fields "Title:string Content:string?"`

### Day 2: CRUD Operations
1. Study the generated CRUD controller
2. Customize the Index page styling
3. Add custom validation rules
4. Test create, edit, delete operations

### Day 3: Advanced Features
1. Add search functionality
2. Implement sorting
3. Add bulk operations
4. Create custom modals

### Day 4: Testing
1. Write unit tests for controllers
2. Test HTMX interactions
3. Integration tests with database
4. Snapshot testing for views

### Week 2: Production
1. Deploy with Docker
2. Set up CI/CD
3. Performance optimization
4. Monitoring and logging

---

## 🔗 Resources

- **Documentation**: `/docs` folder
- **Examples**: `/testApps` folder
- **Framework Source**: `/framework` folder
- **CLI Source**: `/tools/Swap.CLI` folder
- **Templates**: `/templates` folder

---

## 🤝 Getting Help

1. **Check the docs**: Start with `CONTAINER-ARCHITECTURE.md`
2. **Look at examples**: Study the generated code
3. **Read the tests**: See patterns in action
4. **Ask questions**: Open an issue on GitHub

---

**Remember: Swap is designed to be simple. If something feels complex, there's probably a better way. Don't fight the framework - embrace the patterns.**
