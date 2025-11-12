# TaskFlow - Comprehensive Swap.Htmx Demo# TaskFlow



> 🚀 **A complete task management application showcasing the full power of Swap.Htmx**> 🚀 **Production-ready HTMX monolith** — built with [Swap](https://github.com/jdtoon/swap)



This is a production-quality demonstration of the [Swap.Htmx](https://github.com/jdtoon/swap) framework, built entirely with **server-driven HTMX** patterns. No client-side JavaScript required.Zero config. Zero ceremony. **Pure velocity.** This is ASP.NET Core + HTMX + Bulma done right, with a real event system, production-ready patterns, and tests that actually test your HTMX flows.



![Task Board Screenshot](screenshot.png)Built for developers who want **maximum productivity** without sacrificing quality.



------



## 🎯 What This Demo Showcases## ⚡ Quickstart



### **Core Swap.Htmx Features**```bash

cd src

✅ **SwapController Pattern**libman restore

- Automatic partial vs. full page detectiondotnet run

- `SwapView()` method for seamless HTMX integration```

- Clean separation of concerns

Open **http://localhost:5000** and you're live. Everything just works.

✅ **Event System**

- Declarative event chains configured at startup---

- Domain events (`task.created`, `task.updated`, etc.)

- UI events for triggering refreshes (`ui.taskBoard.refresh`, etc.)## 🎯 What's Inside

- Automatic event-to-trigger header translation

- Zero manual header managementThis isn't just another template. You get:



✅ **Out-of-Band (OOB) Swaps**### **Core Stack**

- Update multiple DOM elements in a single response- 🎨 **ASP.NET Core 9 MVC** — Server-rendered, fast, reliable

- Task board columns refresh independently- ⚡ **HTMX 2.x** — Modern interactions without JavaScript complexity

- Stats update automatically on task changes- 🌸 **Bulma CSS** — Beautiful UI, zero build step, zero NPM

- Activity feed updates in real-time- 💾 **Entity Framework Core 9** — SQLite by default (swap to SQL Server/Postgres in seconds)



✅ **Toast Notifications**### **Developer Experience**

- Server-triggered toast messages- 🔥 **Swap Event System** — HTMX-native server events with `ui.*` triggers

- Success, error, warning, and info variants- ✅ **Real Integration Tests** — Test your HTMX partials like a user would

- Automatic HX-Trigger header integration- 🐳 **Docker Ready** — `docker compose up` and you're in production mode

- 🛠️ **CLI Generators** — Scaffold models, controllers, full CRUD in one command

✅ **Pure Server-Side Rendering**- 📦 **Zero NPM** — LibMan manages HTMX & Bulma, no node_modules bloat

- Zero JavaScript required (except for minimal UI toggles)

- All logic lives in C# controllers### **Production Ready**

- Type-safe, testable, maintainable- 📦 Containerized builds with multi-stage Dockerfile

- 🔐 Data protection keys configured for clustered deployments

---- 🗄️ Auto-migrations on startup (Development only)

- 🌱 Smart seeding system with locale support

## 🏗️ Architecture Highlights- 📊 Built-in health checks and diagnostics

- 🎨 Bulma CSS via LibMan (no build step required)

### **Event-Driven Design**

---

The entire application is built around the Swap event system. Here's how it works:

## 📁 Project Structure

```csharp

// 1. Define event chains in SwapEventChains.csClean. Organized. **No mystery folders.**

events.Chain(

    EventNames.Domain.TaskCreated,```

    EventNames.Ui.TaskBoardRefresh,TaskFlow/

    EventNames.Ui.TaskStatsRefresh,├─ src/                          # 👈 Your app lives here

    EventNames.Ui.TaskActivityRefresh,│  ├─ Controllers/               # MVC controllers (Home, Demo, Components)

    EventNames.Ui.ToastSuccess│  ├─ Data/                      # EF Core DbContext + Migrations

);│  │  └─ Seeders/                # Smart data seeding for dev

│  ├─ Dtos/                      # View models and transfer objects

// 2. Emit domain events in controllers│  ├─ Events/                    # HTMX event chains (server-side)

_events.Emit(EventNames.Domain.TaskCreated, new { id = task.Id });│  ├─ Models/                    # Domain entities

│  ├─ Views/                     # Razor views + HTMX partials

// 3. Framework automatically triggers all chained UI events│  │  ├─ Shared/                 # Layout, toasts, reusable components

// No manual header management needed!│  │  ├─ Home/                   # Todo app example

```│  │  ├─ Demo/                   # Event system playground

│  │  └─ Components/             # Generic components (counter, panels)

### **HTMX-Driven UI Updates**│  ├─ wwwroot/                   # Static files (CSS, JS, images)

│  │  ├─ css/                    # Custom styles

```html│  │  ├─ js/                     # Client-side helpers

<!-- Element listens for specific UI events -->│  │  └─ lib/                    # HTMX, Bulma (managed by LibMan)

<div id="task-stats" │  ├─ Extensions/                # Helper methods (Session, etc.)

     hx-get="/Tasks/Stats" │  ├─ Infrastructure/            # Framework plumbing (model binders, etc.)

     hx-trigger="load, ui.taskStats.refresh from:body"│  ├─ Program.cs                 # 👈 App entry point

     hx-swap="innerHTML">│  ├─ appsettings.json           # Configuration

</div>│  ├─ TaskFlow.csproj     # Project file

```│  ├─ libman.json                # LibMan (HTMX, Bulma)

│  └─ Dockerfile                 # Production container

When `task.created` is emitted server-side, the event chain triggers `ui.taskStats.refresh`, which fires the HTMX request to refresh stats. **Pure magic.**│

├─ tests/                        # 👈 Real tests

---│  ├─ Unit/                      # Fast unit tests

│  └─ Integration/               # HTMX-aware integration tests

## 🌟 Features Demonstrated│

├─ docs/                         # 👈 Project documentation

### **Task Management**│  ├─ ARCHITECTURE.md            # How everything fits together

- ✅ Kanban board with three columns (To Do, In Progress, Done)│  ├─ DEVELOPMENT.md             # Dev setup & workflows

- ✅ Create tasks with title, description, priority, assignee, due date│  ├─ DEPLOYMENT.md              # Docker, production, scaling

- ✅ Edit tasks inline with modal forms│  └─ EVENTS.md                  # Event system deep dive

- ✅ Quick status changes with buttons│

- ✅ Delete tasks with confirmation├─ docker-compose.yml            # One-command containerized setup

- ✅ Priority levels (Low, Medium, High, Critical)├─ TaskFlow.sln           # Solution file

- ✅ Due date tracking with overdue indicators└─ README.md                     # 👈 You are here

- ✅ Task assignment```



### **Real-Time Updates**---

- ✅ Statistics dashboard (total tasks, by status, high priority, overdue)

- ✅ Activity feed showing recent actions## 🎭 The HTMX Event System

- ✅ Automatic board updates when tasks change

- ✅ Column-specific refreshes (only affected columns update)This is where the magic happens. **No Redux. No state management hell. Just events.**



### **Search & Filter**### Server-side (once, in `Program.cs`):

- ✅ Real-time search as you type (debounced)

- ✅ Filter by status, priority, and assignee```csharp

- ✅ Results update instantly via HTMXbuilder.Services.AddSwapHtmx(events =>

{

### **User Experience**    // When a todo is created, refresh the list AND show a toast

- ✅ Bulma CSS for beautiful, responsive design    events.Chain(

- ✅ FontAwesome icons throughout        SwapEvents.Entity.Created("todo"),

- ✅ Smooth animations and transitions        SwapEvents.UI.RefreshList,

- ✅ Loading states for async operations        SwapEvents.UI.ShowToast);

- ✅ Modal forms for create/edit        

- ✅ Hover effects and visual feedback    // When bulk operation completes, refresh everything

    events.Chain(

---        SwapEvents.Entity.BulkUpdated("todo"),

        SwapEvents.UI.RefreshList,

## 🚀 Running the Demo        SwapEvents.UI.RefreshStats,

        SwapEvents.UI.ShowToast);

### **Prerequisites**});

- .NET 9 SDK```

- SQLite (included)

### Client-side (in your views):

### **Quick Start**

```html

```bash<!-- This list refreshes automatically when ui.refreshList fires -->

# Navigate to the demo project<div id="todo-list"

cd demo/src     hx-get="/Home/TodoList"

     hx-trigger="load, ui.refreshList from:body"

# Restore frontend libraries (if needed)     hx-swap="outerHTML">

libman restore</div>



# Run the application<!-- Stats panel listens too -->

dotnet run<div hx-get="/Demo/Stats"

     hx-trigger="load, ui.refreshStats from:body">

# Open browser to:</div>

http://localhost:5000```

```

**That's it.** One action triggers multiple updates across your UI. Zero JavaScript needed.

### **Navigate to Task Board**

Click "Task Board" in the navigation menu or go directly to:👉 **See it live:** `/Demo/Index` for a full playground with bulk operations, dynamic panels, and toast notifications.

```

http://localhost:5000/Tasks📚 **Learn more:** [`docs/EVENTS.md`](docs/EVENTS.md)

```

---

---

## 🧪 Testing That Actually Works

## 📁 Project Structure

Integration tests for HTMX apps using **Swap.Testing** — assert on partials, headers, DOM content, and HTMX responses.

```

demo/```csharp

├── src/public class TodoFlowTests : IClassFixture<HtmxTestFixture<Program>>

│   ├── Controllers/{

│   │   ├── TasksController.cs     # Main task management controller    private readonly HtmxTestClient<Program> _client;

│   │   ├── HomeController.cs      # Homepage    

│   │   └── DemoController.cs      # Original demo features    public TodoFlowTests(HtmxTestFixture<Program> fixture) 

│   ├── Models/        => _client = fixture.Client;

│   │   ├── TaskItem.cs            # Task entity with status, priority

│   │   └── ActivityLog.cs         # Activity tracking    [Fact]

│   ├── Dtos/    public async Task Creating_Todo_Updates_List()

│   │   └── TaskDtos.cs            # DTOs for forms and stats    {

│   ├── Events/        // Act: Create a new todo via HTMX POST

│   │   ├── EventNames.cs          # Strongly-typed event constants        var response = await _client.HtmxPostAsync("/Home/AddTodo", 

│   │   └── SwapEventChains.cs     # Event chain configuration            new { title = "Test HTMX Flow" });

│   ├── Views/        

│   │   ├── Tasks/        // Assert: Response is successful

│   │   │   ├── Index.cshtml       # Main task board view        response.AssertSuccess();

│   │   │   ├── _TaskCard.cshtml   # Individual task card        

│   │   │   ├── _TaskColumn.cshtml # Kanban column        // Assert: List now contains our todo

│   │   │   ├── _Stats.cshtml      # Statistics dashboard        var list = await _client.HtmxGetAsync("/Home/TodoList");

│   │   │   ├── _ActivityFeed.cshtml # Activity timeline        await list.AssertContainsAsync("Test HTMX Flow");

│   │   │   ├── _CreateTaskForm.cshtml # Task creation form        

│   │   │   ├── _EditTaskForm.cshtml   # Task edit form        // Assert: Server sent the right events

│   │   │   └── _SearchResults.cshtml  # Search results        response.AssertHasSwapEvent("entity.created:todo");

│   │   └── Shared/        response.AssertHasSwapEvent("ui.refreshList");

│   │       ├── _Layout.cshtml     # Main layout with navigation    }

│   │       └── _ToastContainer.cshtml # Toast notification handler}

│   ├── Data/```

│   │   └── AppDbContext.cs        # EF Core context with seed data

│   └── wwwroot/**Run all tests:**

│       └── css/```bash

│           └── taskboard.css      # Custom task board stylesdotnet test

└── README.md                      # This file```

```

Tests are **fast**, **reliable**, and **actually test your HTMX behavior** — not just JSON endpoints.

---

---

## 🎓 Code Tour: How It Works

## 🐳 Docker & Production

### **1. Creating a Task**

### Local Development with Docker

**Controller (`TasksController.cs`):**

```csharp```bash

[HttpPost]# From project root

public IActionResult Create(CreateTaskDto dto)docker compose up --build

{```

    var task = new TaskItem { /* ... */ };

    _context.Tasks.Add(task);Opens on **http://localhost:5000** with:

    _context.SaveChanges();- SQLite database (persisted in Docker volume)

- Auto-migrations on startup

    // Log activity- Live development mode

    _context.ActivityLogs.Add(new ActivityLog { /* ... */ });

    _context.SaveChanges();### Production Build



    // Emit domain event - event chain handles all UI updates```bash

    _events.Emit(EventNames.Domain.TaskCreated, new { id = task.Id });# Build optimized image

docker build -t taskflow ./src

    return PartialView("_CreateTaskForm"); // Return empty form

}# Run in production mode

```docker run -d \

  -p 8080:8080 \

**Event Chain (`SwapEventChains.cs`):**  -e ASPNETCORE_ENVIRONMENT=Production \

```csharp  -e ConnectionStrings__DefaultConnection="<your-db-here>" \

.Chain(EventNames.Domain.TaskCreated,  taskflow

    EventNames.Ui.TaskBoardRefresh,```

    EventNames.Ui.TaskStatsRefresh,

    EventNames.Ui.TaskActivityRefresh,**Multi-stage Dockerfile** includes:

    EventNames.Ui.ToastSuccess)- ✅ LibMan for HTMX/Bulma restoration

```- ✅ Optimized production build

- ✅ Non-root runtime user

**View (`Index.cshtml`):**- ✅ Health checks

```html

<!-- Stats listen for refresh events -->📚 **Production guide:** [`docs/DEPLOYMENT.md`](docs/DEPLOYMENT.md)

<div id="task-stats" 

     hx-get="/Tasks/Stats" ---

     hx-trigger="ui.taskStats.refresh from:body">

</div>## 🛠️ CLI Code Generators



<!-- Each column listens for board refresh -->The Swap CLI makes scaffolding blazing fast.

<div id="task-column-todo" 

     hx-get="/Tasks/TaskColumn?status=0" ### Generate a full CRUD resource:

     hx-trigger="ui.taskBoard.refresh from:body">```bash

</div>swap g r Product --fields "Name:string Price:decimal InStock:bool:f"

``````



**Result:** One event emission updates the task board, stats, activity feed, AND shows a toast. All automatically.Creates:

- ✅ `Models/Product.cs` — Entity with properties

### **2. Changing Task Status**- ✅ `Controllers/ProductsController.cs` — Full CRUD actions

- ✅ `Views/Products/*.cshtml` — HTMX-native views (Index, Create, Edit, Delete)

**Controller:**- ✅ Event chain wiring in `Program.cs`

```csharp

[HttpPatch]### Just a model:

public IActionResult ChangeStatus(int id, TaskItemStatus newStatus)```bash

{swap g m Category --fields "Name:string Slug:string Description:string?"

    task.Status = newStatus;```

    _context.SaveChanges();

### Just a controller:

    // Emit status change event```bash

    _events.Emit(EventNames.Domain.TaskStatusChanged, swap g c Orders --fields "OrderNumber:string Total:decimal Status:string"

        new { id, oldStatus, newStatus });```



    return PartialView("_TaskCard", task);**Flags:**

}- `--no-views` — Skip view generation

```- `--no-events` — Skip event wiring

- `--crud` — Full CRUD (default for `swap g r`)

**Event Chain:**

```csharp---

.Chain(EventNames.Domain.TaskStatusChanged,

    EventNames.Ui.TaskTodoRefresh,        // Refresh To Do column## 🎨 Styling with Bulma

    EventNames.Ui.TaskInProgressRefresh,  // Refresh In Progress column

    EventNames.Ui.TaskDoneRefresh,        // Refresh Done columnBeautiful UI out of the box. **No design skills required. No build step.**

    EventNames.Ui.TaskStatsRefresh,       // Update statistics

    EventNames.Ui.TaskActivityRefresh,    // Update activity feedBulma is a modern CSS framework with clean, semantic class names and responsive design built-in.

    EventNames.Ui.ToastSuccess)           // Show success message

```### Common Components



All three columns refresh (task moves between them), stats update, activity log updates, and toast appears. **All with one event emission.**```html

<!-- Buttons -->

### **3. Real-Time Search**<button class="button is-primary">Primary Action</button>

<button class="button is-success">Success</button>

**View:**

```html<!-- Cards -->

<form hx-get="/Tasks/Search" <div class="card">

      hx-target="#search-results"   <div class="card-content">

      hx-trigger="input delay:500ms, change">    <p class="title">Card Title</p>

  <input type="text" name="Search" placeholder="Search tasks...">    <p class="subtitle">Card subtitle</p>

  <select name="Status"><!-- Status filter --></select>  </div>

  <select name="Priority"><!-- Priority filter --></select></div>

  <input type="text" name="AssignedTo" placeholder="Assigned to...">

</form><!-- Forms -->

<div id="search-results"></div><div class="field">

```  <label class="label">Email</label>

  <div class="control">

**Controller:**    <input class="input" type="email" placeholder="email@example.com">

```csharp  </div>

[HttpGet]</div>

public IActionResult Search(TaskFilterDto filter)```

{

    var query = _context.Tasks.AsQueryable();**What you get:**

    - Responsive navbar and footer

    if (!string.IsNullOrWhiteSpace(filter.Search))- Form inputs with built-in validation styles

        query = query.Where(t => t.Title.Contains(filter.Search));- Buttons, cards, modals, notifications

    - Flexbox-based columns and layout

    if (filter.Status.HasValue)- Toast notifications (custom + Bulma styled)

        query = query.Where(t => t.Status == (TaskItemStatus)filter.Status.Value);

    Browse all components: [bulma.io/documentation](https://bulma.io/documentation/)

    // Apply other filters...

    ---

    return PartialView("_SearchResults", query.ToList());

}## 📖 Documentation

```

Deep dives on architecture, patterns, and workflows:

As you type or change filters, HTMX debounces input (500ms delay) and sends server requests. Results update live. **Zero JavaScript.**

| Doc | What's Inside |

---|-----|---------------|

| **[ARCHITECTURE.md](docs/ARCHITECTURE.md)** | How the event system, controllers, and views fit together |

## 🔧 Key Swap Patterns Used| **[DEVELOPMENT.md](docs/DEVELOPMENT.md)** | Dev setup, hot reload, debugging, common workflows |

| **[DEPLOYMENT.md](docs/DEPLOYMENT.md)** | Docker, databases, scaling, monitoring |

### **SwapView() Pattern**| **[EVENTS.md](docs/EVENTS.md)** | Event system deep dive with real examples |

```csharp

public IActionResult Index()---

{

    return SwapView(); // Automatically returns partial for HTMX requests## 🔥 Development Tips

}

```### View live event chains:

Visit **http://localhost:5000/_swap/dev/events** (Development only)

### **Event-Driven Updates**

```csharpInteractive dashboard showing:

_events.Emit(EventNames.Domain.TaskCreated, payload);- All registered event chains

// Framework handles all UI updates via chains- Event triggers and listeners

```- Real-time event flow visualization



### **Toast Notifications**### Seed data for testing:

```csharpConfigure seeding in `docker-compose.yml`:

Response.ShowSuccessToast("Task created successfully!");```yaml

Response.ShowErrorToast("Something went wrong");environment:

Response.ShowWarningToast("Please review this");  - SEED_COUNT=100        # Generate 100 todos

Response.ShowInfoToast("Did you know...");  - SEED_LOCALE=en        # Use English faker data

```  - SEED_IFEMPTY=true     # Only seed if DB is empty

```

### **Modal Forms**

```html### Database migrations:

<button hx-get="/Tasks/CreateForm" ```bash

        hx-target="#modal-content"# Create migration

        onclick="modal.classList.add('is-active')">dotnet ef migrations add AddProductTable

  Create Task

</button># Apply to database

```dotnet ef database update



---# Rollback

dotnet ef database update PreviousMigration

## 📊 Database Schema```



### **TaskItem**### Switch databases:

- `Id` (int, PK)

- `Title` (string)**SQL Server:**

- `Description` (string)```json

- `Status` (enum: Todo, InProgress, Done){

- `Priority` (enum: Low, Medium, High, Critical)  "ConnectionStrings": {

- `AssignedTo` (string)    "DefaultConnection": "Server=localhost;Database=TaskFlow;Trusted_Connection=True;"

- `DueDate` (DateTime?)  }

- `Tags` (List<string>)}

- `CreatedAt` (DateTime)```

- `CompletedAt` (DateTime?)

**PostgreSQL:**

### **ActivityLog**```json

- `Id` (int, PK){

- `Action` (string)  "ConnectionStrings": {

- `Details` (string)    "DefaultConnection": "Host=localhost;Database=TaskFlow;Username=postgres;Password=yourpassword"

- `Timestamp` (DateTime)  }

- `Icon` (string)}

- `ColorClass` (string)```



---Update provider in `Program.cs`:

```csharp

## 🎨 Styling// For SQL Server

builder.Services.AddDbContext<AppDbContext>(options =>

- **Framework:** Bulma CSS 1.0.4    options.UseSqlServer(connectionString));

- **Icons:** FontAwesome 6.4.0

- **Custom:** `taskboard.css` for animations and transitions// For PostgreSQL  

- **Toast:** Built-in Swap toast stylesbuilder.Services.AddDbContext<AppDbContext>(options =>

- **Responsive:** Mobile-friendly Kanban board    options.UseNpgsql(connectionString));

```

---

---

## 🧪 Testing

## 🚀 What's Next?

The demo includes:

- ✅ Seeded database with sample tasks**Start building:**

- ✅ Multiple priority levels1. Explore the Todo app at `/` to see HTMX in action

- ✅ Tasks in different statuses2. Try the event system playground at `/Demo`

- ✅ Overdue and upcoming tasks3. Check out `/Components/Generic` for reusable patterns

- ✅ Activity log with sample entries4. Run the tests: `dotnet test`

5. Generate your first resource: `swap g r Product --fields "Name:string Price:decimal"`

Just run the app and explore!

**Level up:**

---- Add authentication (Identity, Auth0, etc.)

- Integrate with an external API

## 💡 What Makes This "The Swap Way"- Add real-time updates with SignalR

- Deploy to Azure/AWS/your favorite cloud

### **1. Pure Server-Driven**

Every interaction is handled server-side. The server returns HTML, not JSON. The client just swaps it in.**Get help:**

- 📚 [Swap Documentation](https://github.com/jdtoon/swap/wiki)

### **2. Event System First**- 💬 [Discussions](https://github.com/jdtoon/swap/discussions)

Instead of manually managing HTMX headers and triggers, we emit domain events and let chains handle UI updates. Declarative and testable.- 🐛 [Issues](https://github.com/jdtoon/swap/issues)



### **3. Type-Safe**---

`EventKey` instances are strongly typed. No magic strings scattered through code.

## 💪 Built with Swap

### **4. Maintainable**

Event chains are configured in one place (`SwapEventChains.cs`). Adding a new UI update is a single line addition to a chain.This template is maintained by the Swap framework team. 



### **5. Zero Build Step****Swap** = Modern web development without the complexity.

No npm, no webpack, no bundling. Just LibMan for CSS/JS libraries. Lightning-fast development.

⭐ **Star the repo:** [github.com/jdtoon/swap](https://github.com/jdtoon/swap)

### **6. Testable**

Event chains can be unit tested. Controllers can be integration tested. HTMX responses can be verified.---



---**Happy building! 🎉**


## 🚀 Extending the Demo

Want to add features? Here's how:

### **Add a New Feature: Task Comments**

**1. Define Events in `EventNames.cs`:**
```csharp
public static readonly EventKey TaskCommented = new("task.commented");
public static readonly EventKey CommentsRefresh = new("ui.comments.refresh");
```

**2. Add Chain in `SwapEventChains.cs`:**
```csharp
.Chain(EventNames.Domain.TaskCommented,
    EventNames.Ui.CommentsRefresh,
    EventNames.Ui.TaskActivityRefresh,
    EventNames.Ui.ToastSuccess)
```

**3. Create Controller Action:**
```csharp
[HttpPost]
public IActionResult AddComment(int taskId, string text)
{
    var comment = new Comment { TaskId = taskId, Text = text };
    _context.Comments.Add(comment);
    _context.SaveChanges();
    
    _events.Emit(EventNames.Domain.TaskCommented, new { taskId, text });
    
    return PartialView("_CommentForm");
}
```

**4. Add View with HTMX:**
```html
<div hx-get="/Tasks/Comments?taskId=@Model.Id"
     hx-trigger="load, ui.comments.refresh from:body">
</div>
```

Done. Comments refresh automatically when added, activity log updates, toast appears. The Swap way.

---

## 📚 Learn More

- **Swap Framework:** https://github.com/jdtoon/swap
- **Swap Documentation:** See `docs/` folder in the main repository
- **HTMX:** https://htmx.org
- **Bulma CSS:** https://bulma.io
- **ASP.NET Core:** https://docs.microsoft.com/aspnet/core

---

## 🎉 Conclusion

This demo proves that you can build rich, interactive web applications with:

✅ **Zero client-side JavaScript** (except minimal UI helpers)  
✅ **Clean, maintainable server-side code**  
✅ **Type-safe event-driven architecture**  
✅ **Beautiful, responsive UI**  
✅ **Real-time updates without websockets**  
✅ **Excellent developer experience**  

**The future of web development is server-driven. Welcome to Swap.Htmx.**

---

## 🤝 Contributing

Want to improve this demo? PRs welcome!

## 📝 License

This demo is part of the Swap.Htmx framework. See the main repository for license details.
