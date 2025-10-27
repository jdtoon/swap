# Swap Pattern Library

**Last Updated**: October 27, 2025  
**Source**: Analysis of 4 production ASP.NET Core + HTMX applications  
**Apps Analyzed**: TTW (Travel), Kanban (Task Management), Habits (Family Tracking), Carestream (Healthcare)

---

## Overview

This document catalogs **30+ proven HTMX patterns** extracted from real production applications. Each pattern includes implementation details, code examples, and usage guidance.

These patterns form the foundation of Swap's code generation capabilities. When you run `swap generate controller`, you're getting these battle-tested patterns automatically.

---

## 🎯 Pattern Analysis by Category

### 1️⃣ Foundation Patterns (Used in ALL 4 Apps)

#### ✅ HX-Request Detection
**Usage**: 100% of controllers check for HTMX requests

**Pattern**:
```csharp
// Simple approach (Habits)
var isHtmx = Request.Headers.ContainsKey("HX-Request");
return isHtmx ? PartialView() : View();

// With ViewBag (TTW/Kanban)
ViewBag.IsHTMXRequest = Request.IsHtmx();
return PartialView(); // Layout decides full vs partial
```

**Why This Matters**: Single controller action serves both full page and HTMX requests.

---

#### ✅ Component-Based Partials
**Usage**: Habits app has 9 reusable partials in `Views/Shared/`

**Structure**:
```
Views/Shared/
├── _UserDisplay.cshtml          // User avatar + name
├── _SearchBar.cshtml             // Global search
├── _BurgerMenu.cshtml            // Mobile navigation
├── _PageHeading.cshtml           // Dynamic page title
├── _LoginUser.cshtml             // Login/logout button
└── _GlobalSearchResults.cshtml  // Search results
```

**Controller Pattern**:
```csharp
public IActionResult GetUserDisplay() 
    => PartialView("_UserDisplay", currentUser);

public IActionResult GetSearchBar() 
    => PartialView("_SearchBar");
```

**View Usage**:
```html
<div hx-get="@Url.Action("GetUserDisplay","Home")"
     hx-trigger="load"
     hx-target="#user-display-container"
     hx-swap="innerHTML">
</div>
```

**Insight**: Small, focused partials loaded on demand = composable UIs.

---

#### ✅ Load-on-Page-Load Pattern
**Usage**: Every Index.cshtml loads components on page load

**Pattern**:
```html
<!-- Load navbar components on page load -->
<div hx-get="@Url.Action("GetBurgerMenu","Home")"
     hx-trigger="load"
     hx-target="#navbar-start-placeholder"
     hx-swap="innerHTML transition:true"
     hx-indicator="#loading-indicator">
</div>
```

**Why**: Progressive enhancement - HTML shell loads fast, components fill in.

---

### 2️⃣ Modal CRUD Patterns (Habits, Carestream)

#### ✅ Modal Add/Edit/Delete
**Pattern**:
```csharp
// ADD: Return modal partial
public IActionResult AddTaskList() 
    => PartialView("_AddTaskListModal", new TaskListDto());

// EDIT: Return modal partial with data
public IActionResult EditTaskList(int id) 
{
    var taskList = _service.GetById(id);
    return PartialView("_EditTaskListModal", taskList);
}

// DELETE: Inline delete with confirmation
[HttpDelete]
public IActionResult DeleteTaskList(int id) 
{
    var success = _service.Delete(id);
    return success ? Ok() : BadRequest();
}
```

**View Pattern**:
```html
<!-- Modal Container (empty, receives modals) -->
<div id="modal-container"></div>

<!-- Trigger modal -->
<button hx-get="@Url.Action("AddTaskList")"
        hx-target="body"
        hx-swap="beforeend">
    Add Task List
</button>

<!-- Modal Partial (_AddTaskListModal.cshtml) -->
<dialog id="add-modal" class="modal" open>
    <div class="modal-box">
        <button hx-get="@Url.Action("ClearDiv")"
                hx-target="#add-modal"
                hx-swap="delete">
            ✕
        </button>
        
        <form hx-post="@Url.Action("CreateTaskList")"
              hx-target="#task-lists-sortable"
              hx-swap="afterbegin">
            <!-- Form fields -->
        </form>
    </div>
</dialog>
```

**Insight**: Modal CRUD is a solved pattern. Always the same structure.

---

### 3️⃣ Advanced Response Header Patterns (Carestream)

#### ✅ Dynamic Retargeting on Validation Errors
**Pattern**:
```csharp
[HttpPost]
public IActionResult RegisterPatient(PatientDto dto)
{
    if (!ModelState.IsValid)
    {
        // Change target to show errors in-place
        Response.Headers.Append("HX-Retarget", "#register-patient-form-container");
        Response.Headers.Append("HX-Reswap", "innerHTML");
        return PartialView("_RegisterPatientForm", dto);
    }
    
    // Success: Redirect to patient list
    Response.Headers.Append("HX-Redirect", "/Patient/AllPatients");
    return Ok();
}
```

**Why This Matters**: Server controls behavior dynamically. Validation errors appear in-place.

---

#### ✅ Custom Toast Events
**Pattern**:
```csharp
// Success toast
Response.Headers.Append("HX-Trigger", 
    "{\"showToastSuccess\": \"Patient registered successfully!\"}");

// Error toast
Response.Headers.Append("HX-Trigger", 
    "{\"showToastError\": \"Validation failed. Please correct errors.\"}");
```

**JavaScript Listener**:
```javascript
document.body.addEventListener('showToastSuccess', function(evt) {
    showToast('success', evt.detail.value);
});

document.body.addEventListener('showToastError', function(evt) {
    showToast('error', evt.detail.value);
});
```

**Insight**: Custom events enable loosely coupled components. Toast system is universal.

---

#### ✅ Multi-Event Triggering (Habits Calendar)
**Pattern**:
```csharp
// Trigger multiple events after operation
Response.Headers.Append("HX-Trigger-After-Settle",
    JsonSerializer.Serialize(new {
        calendarUpdated = new { date = dto.StartDate },
        eventUpdated = true,
        refreshStats = true
    }));
```

**View Listeners**:
```html
<!-- Calendar refreshes on calendarUpdated -->
<div hx-trigger="calendarUpdated from:body"
     hx-get="@Url.Action("GetCalendar")"
     hx-target="#calendar-container">
</div>

<!-- Stats refresh on refreshStats -->
<div hx-trigger="refreshStats from:body"
     hx-get="@Url.Action("GetStats")"
     hx-target="#stats-container">
</div>
```

**Insight**: One operation can update multiple UI sections without coupling.

---

### 4️⃣ Pagination Pattern ⭐ **GOLDMINE**

#### ✅ Reusable Pagination DTO (Carestream)
**This is the most valuable pattern we found.**

**DTO Structure**:
```csharp
public class PaginationDto
{
    // Standard properties
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 25;
    public int TotalItems { get; set; }
    public int TotalPages { get; set; }
    
    // ⭐ HTMX-specific properties (the key innovation)
    public string HxGetUrl { get; set; } = string.Empty;
    public string HxTarget { get; set; } = string.Empty;
    public string HxSwap { get; set; } = "innerHTML";
    
    // Computed properties
    public bool HasPreviousPage => CurrentPage > 1;
    public bool HasNextPage => CurrentPage < TotalPages;
}
```

**Controller Setup**:
```csharp
public IActionResult AllPatients(FilterAndPaginationOptions options)
{
    var (patients, totalCount) = _service.GetPaginated(options);
    
    var viewModel = new PatientListViewModel
    {
        Patients = patients,
        Pagination = new PaginationDto
        {
            CurrentPage = options.PageNumber,
            PageSize = options.PageSize,
            TotalItems = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)options.PageSize),
            
            // Set HTMX properties once in controller
            HxGetUrl = Url.Action("AllPatients", "Patient"),
            HxTarget = "#patient-list-container",
            HxSwap = "innerHTML"
        }
    };
    
    return PartialView("_PatientListPartial", viewModel);
}
```

**Reusable Partial** (`_PaginationControls.cshtml`):
```html
@model PaginationDto

@if (Model.TotalPages > 1)
{
    <div class="pagination">
        @if (Model.HasPreviousPage)
        {
            <button hx-get="@Model.HxGetUrl"
                    hx-vals='{"pageNumber": @(Model.CurrentPage - 1), "pageSize": @Model.PageSize}'
                    hx-target="@Model.HxTarget"
                    hx-swap="@Model.HxSwap">
                « Previous
            </button>
        }
        
        @for (int i = StartPage; i <= EndPage; i++)
        {
            <button hx-get="@Model.HxGetUrl"
                    hx-vals='{"pageNumber": @i, "pageSize": @Model.PageSize}'
                    hx-target="@Model.HxTarget"
                    hx-swap="@Model.HxSwap"
                    class="@(i == Model.CurrentPage ? "active" : "")">
                @i
            </button>
        }
        
        @if (Model.HasNextPage)
        {
            <button hx-get="@Model.HxGetUrl"
                    hx-vals='{"pageNumber": @(Model.CurrentPage + 1), "pageSize": @Model.PageSize}'
                    hx-target="@Model.HxTarget"
                    hx-swap="@Model.HxSwap">
                Next »
            </button>
        }
    </div>
}
```

**Why This is Brilliant**:
- **Single Partial**: One `_PaginationControls.cshtml` for entire app
- **No Duplication**: HTMX attributes set once in controller, reused in view
- **Flexible**: Works with any entity (Products, Orders, Patients)
- **Filterable**: Combines with search/filter options
- **No JavaScript**: Pure HTMX, server-side logic

**FilterAndPaginationOptions**:
```csharp
public class FilterAndPaginationOptions
{
    public string? SearchTerm { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 25;
    public string SortBy { get; set; } = "Id";
    public bool SortAscending { get; set; } = true;
}
```

---

### 5️⃣ Session State Management (Habits)

#### ✅ Storing Filter State Across Requests
**Pattern**:
```csharp
// Store event types in session for day viewer
HttpContext.Session.SetString("EventTypes", 
    JsonSerializer.Serialize(eventTypes));

// Store selected date
HttpContext.Session.SetString("SelectedSearchDate", date);

// Retrieve in subsequent requests
var storedDate = HttpContext.Session.GetString("SelectedSearchDate");
var eventTypes = JsonSerializer.Deserialize<List<EventTypeDto>>(
    HttpContext.Session.GetString("EventTypes"));
```

**Use Case**: Calendar filters, search criteria, wizard state

**Extension Method Pattern**:
```csharp
public static class SessionExtensions
{
    public static void SetObject<T>(this ISession session, string key, T value)
    {
        session.SetString(key, JsonSerializer.Serialize(value));
    }
    
    public static T? GetObject<T>(this ISession session, string key)
    {
        var json = session.GetString(key);
        return json == null ? default : JsonSerializer.Deserialize<T>(json);
    }
}
```

---

### 6️⃣ Sortable/Drag-Drop Pattern (Habits)

#### ✅ SortableJS Integration
**Pattern**:
```csharp
[HttpPost]
public async Task<IActionResult> UpdateTaskListOrder(
    [FromForm] string id, 
    [FromForm] string newPosition)
{
    var taskListId = int.Parse(id);
    var position = int.Parse(newPosition);
    
    await _service.UpdatePositionAsync(taskListId, position);
    
    return Ok();
}
```

**View**:
```html
<div id="task-lists-sortable">
    @foreach (var list in Model.TaskLists)
    {
        <div data-id="@list.Id">
            <!-- Task list content -->
        </div>
    }
</div>

<script>
    new Sortable(document.getElementById('task-lists-sortable'), {
        animation: 150,
        onEnd: function(evt) {
            htmx.ajax('POST', '/Task/UpdateTaskListOrder', {
                values: {
                    id: evt.item.dataset.id,
                    newPosition: evt.newIndex
                }
            });
        }
    });
</script>
```

---

### 7️⃣ Search Patterns

#### ✅ Global Search (Habits)
**Pattern**:
```csharp
public IActionResult GlobalSearch(string query)
{
    var results = new GlobalSearchResultsDto
    {
        Tasks = _taskService.Search(query),
        Events = _calendarService.Search(query),
        Documents = _documentService.Search(query)
    };
    
    return PartialView("_GlobalSearchResults", results);
}
```

**View**:
```html
<form hx-get="@Url.Action("GlobalSearch", "Home")"
      hx-trigger="input changed delay:500ms"
      hx-target="#search-results"
      hx-indicator="#search-spinner">
    <input type="search" name="query" placeholder="Search...">
</form>

<div id="search-results"></div>
```

**Key**: `delay:500ms` debounces search requests.

---

#### ✅ Filtered Search with Pagination (Carestream)
**Pattern**:
```html
<form hx-get="@Url.Action("AllPatients")"
      hx-target="#patient-list-container"
      hx-swap="innerHTML">
    <input type="search" name="SearchTerm1" placeholder="Search...">
    <input type="hidden" name="pageNumber" value="1">
    <input type="hidden" name="pageSize" value="25">
    <button type="submit">Search</button>
</form>
```

**Controller**:
```csharp
public IActionResult AllPatients(FilterAndPaginationOptions options)
{
    // Handles search + pagination together
    var results = _service.GetPaginated(options);
    // ...
}
```

**Insight**: Search resets to page 1, filter + pagination work together.

---

### 8️⃣ Day Viewer Pattern (Habits Calendar)

#### ✅ Date-Based Filtering with Modal
**Pattern**:
```csharp
public IActionResult GetDayViewer(string date)
{
    var parsedDate = DateTime.Parse(date);
    var events = _service.GetEventsByDate(parsedDate);
    var viewModel = new DayViewerDto { Date = parsedDate, Events = events };
    return PartialView("_DayViewer", viewModel);
}
```

**View**:
```html
<dialog id="day-viewer" class="modal" open>
    <h2>@Model.Date.ToString("d MMMM yyyy")</h2>
    
    <!-- Auto-refresh when events updated -->
    <div hx-trigger="eventUpdated from:body"
         hx-get="@Url.Action("GetDayViewer")"
         hx-vals='{"date": "@Model.Date.ToString("yyyy-MM-dd")"}'
         hx-target="#day-viewer"
         hx-swap="innerHTML">
        <partial name="_DayEvents" model="Model.Events" />
    </div>
</dialog>
```

**Insight**: Custom events coordinate calendar + day viewer updates.

---

### 9️⃣ Attribute Routing (Carestream)

#### ✅ RESTful URLs
**Pattern**:
```csharp
[HttpGet("Patient/EditPersonalInfoForm/{patientId:int}")]
public IActionResult EditPersonalInfoForm(int patientId)
{
    // Clean URL: /Patient/EditPersonalInfoForm/123
}

[HttpPost("Patient/RegisterNewPatient")]
public IActionResult RegisterNewPatient(PatientDto dto)
{
    // Clean URL: /Patient/RegisterNewPatient
}
```

**Insight**: Attribute routing = cleaner URLs, better RESTful design.

---

### 🔟 Claims-Based Identity (Carestream)

#### ✅ User Context in Controllers
**Pattern**:
```csharp
// Get user ID from claims
var userIdString = User.FindFirstValue("carestream_user_id");
var userId = int.Parse(userIdString ?? "0");

// Pass to service
var result = _service.RegisterPatient(dto, userId);
```

**Insight**: Claims flow naturally through controller → service → repository.

---

## 📚 HTMX Documentation Highlights

### Response Headers (Most Valuable)

| Header | Purpose | Example |
|--------|---------|---------|
| `HX-Trigger` | Fire custom events | `{"showToast": "Saved!"}` |
| `HX-Trigger-After-Settle` | Fire events after swap | `{"calendarUpdated": {...}}` |
| `HX-Retarget` | Change target dynamically | `#error-container` |
| `HX-Reswap` | Change swap strategy | `outerHTML` |
| `HX-Redirect` | Client-side redirect | `/login` |
| `HX-Location` | Client-side navigation | `{"path": "/products"}` |
| `HX-Refresh` | Force page refresh | `true` |

### Swap Strategies (8 Total)

| Strategy | Effect | Use Case |
|----------|--------|----------|
| `innerHTML` | Replace content | Default lists |
| `outerHTML` | Replace element | Row updates |
| `beforebegin` | Insert before | Prepend to list |
| `afterbegin` | Insert as first child | Add to top |
| `beforeend` | Insert as last child | Append to list |
| `afterend` | Insert after | Add after element |
| `delete` | Remove element | Delete confirmation |
| `none` | No swap | Fire events only |

### Trigger Modifiers (Essential)

| Modifier | Effect | Example |
|----------|--------|---------|
| `once` | Fire only once | `click once` |
| `changed` | Fire if value changed | `input changed` |
| `delay:500ms` | Debounce delay | `input delay:500ms` |
| `throttle:1s` | Throttle rate | `scroll throttle:1s` |
| `from:body` | Listen on body | `myEvent from:body` |

### Request Headers (Automatic)

| Header | Purpose | Example |
|--------|---------|---------|
| `HX-Request` | Indicates HTMX request | `true` |
| `HX-Target` | ID of target element | `user-list` |
| `HX-Trigger` | ID of triggering element | `search-button` |
| `HX-Current-URL` | Current page URL | `/products?page=2` |

---

## 🏗️ Architecture Insights

### Monolithic vs Clean Architecture

**Habits (Monolithic)**:
- ✅ Simpler structure (all in one project)
- ✅ Faster development for small apps
- ✅ EF Core migrations (preferred)
- ✅ Services in same project
- ✅ Perfect for startups/MVPs

**Carestream (Clean Architecture)**:
- ✅ Better separation of concerns
- ✅ Core/Persistence/Web layers
- ❌ DbUp migrations (we prefer EF Core)
- ✅ Better for enterprise apps
- ✅ Easier to test in isolation

**Swap Approach**:
- **Default**: Monolithic (simpler, faster)
- **Allow**: Raw SQL via `FromSqlRaw()` when needed
- **Support**: Clean architecture for advanced users
- **Dogma**: None. Use what works.

---

## 🎓 Key Learnings

### What Makes HTMX Apps Great

1. **Server controls everything** - No complex client state
2. **Partial views are king** - Compose UIs from partials
3. **Response headers are powerful** - Control behavior from server
4. **Custom events enable coordination** - Loosely coupled components
5. **Pagination is a solved problem** - Reusable DTO + partial
6. **Modals are predictable** - Same pattern everywhere
7. **Validation is server-side** - Simpler than client validation
8. **Search is debounced** - `hx-trigger="input changed delay:500ms"`
9. **Loading indicators are automatic** - `hx-indicator` attribute
10. **Transitions are free** - `transition:true` in views

### Patterns That Appear Everywhere

**Top 10 Most Common Patterns**:
1. ✅ HX-Request detection (100% of apps)
2. ✅ Partial view returns (100% of apps)
3. ✅ Modal CRUD (75% of apps)
4. ✅ Inline delete (100% of apps)
5. ✅ Load-on-page-load (100% of apps)
6. ✅ Component partials (75% of apps)
7. ✅ Custom toast events (50% of apps - enterprise)
8. ✅ Pagination with DTO (50% of apps - enterprise)
9. ✅ Session state management (50% of apps)
10. ✅ Multi-event triggering (25% of apps - advanced)

### What Swap Should Generate

**Essential (Every App Needs)**:
- HX-Request detection in controllers
- Partial view return logic
- Modal CRUD scaffolding
- Inline delete with confirmation
- Component partial structure
- Loading indicators

**Valuable (Most Apps Need)**:
- Pagination DTO + partial
- Toast notification system
- Response header helpers
- Session state helpers
- Global search

**Advanced (Some Apps Need)**:
- Multi-event coordination
- Dynamic retargeting
- Sortable lists
- Day viewer / date filtering
- Claims-based helpers

---

## Pattern Index

### Foundation Patterns (Used in all apps)
1. **HX-Request Detection** - Serve full pages or partials from same endpoint
2. **Component-Based Partials** - Small, focused reusable UI components
3. **Load-on-Page-Load** - Progressive enhancement pattern

### CRUD Patterns
4. **Modal CRUD** - Add/Edit/Delete in modals
5. **Dynamic Retargeting** - Change target based on validation

### Data Display Patterns
6. **Pagination** ⭐ - Reusable DTO with HTMX properties
7. **Sorting** - Column-based sorting with visual indicators
8. **Filtering** - Boolean and search filters

### Interaction Patterns
9. **Toast Notifications** - Success/error feedback via HX-Trigger
10. **Drag & Drop** - SortableJS integration for list reordering
11. **Search** - Global search with debouncing

### State Management
12. **Session Storage** - Persist filter state across requests
13. **Multi-Event Coordination** - Complex UI interactions

---

## Using These Patterns

All patterns in this library are automatically generated when you use Swap CLI:

```bash
# Generate a controller with these patterns built-in
swap g c Product

# Full CRUD with all patterns
swap g r Product --fields Name:string,Price:decimal
```

The generated code is yours to modify and extend. These patterns serve as a starting point, not a framework limitation.

---

## Contributing Patterns

Found a useful pattern in your own application? Consider contributing it back to Swap:

1. Document the pattern with examples
2. Show where it's used in production
3. Explain why it's valuable
4. Submit a PR to add it to this library

---

**Related Documentation**:
- [THE-PRODUCT.md](THE-PRODUCT.md) - Product vision and philosophy
- [CLI Documentation](../wiki/docs/cli/) - Command reference
- [Getting Started Guide](../wiki/docs/getting-started/) - Your first Swap project
