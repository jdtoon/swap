# Phase 3: Complete CRUD with Pagination & Search

**Date**: October 26, 2025  
**Status**: Design Phase

---

## Objectives

Implement complete CRUD functionality with the proven HTMX patterns from our 4 production apps.

### What We're Building

1. **Full CRUD Operations**
   - ✅ Create with proper forms (not hardcoded `title`)
   - ✅ Read (List with pagination + Details view)
   - ✅ Update (Edit with modal)
   - ✅ Delete (with confirmation)

2. **Pagination System** (from Carestream pattern)
   - `PaginationDto` with HTMX properties
   - `_PaginationControls.cshtml` reusable partial
   - Page size options (10, 25, 50, 100)

3. **Search/Filter** (from Habits pattern)
   - Search with debouncing (`delay:500ms`)
   - Combined with pagination
   - Resets to page 1 on search

4. **Modal CRUD** (from Habits/Carestream pattern)
   - Modal Add/Edit forms
   - Delete confirmation
   - Form validation display

---

## Design

### 1. Enhanced Controller Template

Based on Carestream patterns:

```csharp
public class {{EntityName}}Controller : Controller
{
    private readonly AppDbContext _context;

    public {{EntityName}}Controller(AppDbContext context)
    {
        _context = context;
    }

    // LIST with Pagination & Search
    public async Task<IActionResult> Index(int pageNumber = 1, int pageSize = 25, string? searchTerm = null)
    {
        var query = _context.{{EntityNamePlural}}.AsQueryable();
        
        // Search filter
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            {{SearchLogic}} // Generated based on string fields
        }
        
        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        
        var viewModel = new {{EntityName}}ListViewModel
        {
            Items = items,
            Pagination = new PaginationDto
            {
                CurrentPage = pageNumber,
                PageSize = pageSize,
                TotalItems = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                HxGetUrl = Url.Action("Index"),
                HxTarget = "#{{EntityNameLower}}-list-container",
                HxSwap = "innerHTML"
            },
            SearchTerm = searchTerm
        };
        
        // HTMX-aware response
        if (Request.Headers.ContainsKey("HX-Request"))
            return PartialView("_{{EntityName}}List", viewModel);
        
        return View(viewModel);
    }

    // DETAILS
    public async Task<IActionResult> Details(int id)
    {
        var item = await _context.{{EntityNamePlural}}.FindAsync(id);
        if (item == null)
            return NotFound();
        
        return PartialView("_{{EntityName}}Details", item);
    }

    // CREATE (GET) - Return modal
    public IActionResult Create()
    {
        return PartialView("_{{EntityName}}CreateModal", new {{EntityName}}());
    }

    // CREATE (POST)
    [HttpPost]
    public async Task<IActionResult> Create({{EntityName}} model)
    {
        if (!ModelState.IsValid)
        {
            Response.Headers.Append("HX-Retarget", "#{{EntityNameLower}}-form-container");
            Response.Headers.Append("HX-Reswap", "innerHTML");
            return PartialView("_{{EntityName}}Form", model);
        }

        _context.{{EntityNamePlural}}.Add(model);
        await _context.SaveChangesAsync();
        
        Response.Headers.Append("HX-Trigger", "{\"showToast\": {\"type\": \"success\", \"message\": \"{{EntityName}} created successfully!\"}}");
        Response.Headers.Append("HX-Trigger", "{{EntityNameLower}}ListRefresh");
        
        return Ok();
    }

    // EDIT (GET) - Return modal
    public async Task<IActionResult> Edit(int id)
    {
        var item = await _context.{{EntityNamePlural}}.FindAsync(id);
        if (item == null)
            return NotFound();
        
        return PartialView("_{{EntityName}}EditModal", item);
    }

    // EDIT (POST)
    [HttpPost]
    public async Task<IActionResult> Edit(int id, {{EntityName}} model)
    {
        if (id != model.Id)
            return BadRequest();

        if (!ModelState.IsValid)
        {
            Response.Headers.Append("HX-Retarget", "#{{EntityNameLower}}-form-container");
            Response.Headers.Append("HX-Reswap", "innerHTML");
            return PartialView("_{{EntityName}}Form", model);
        }

        _context.Update(model);
        await _context.SaveChangesAsync();
        
        Response.Headers.Append("HX-Trigger", "{\"showToast\": {\"type\": \"success\", \"message\": \"{{EntityName}} updated successfully!\"}}");
        Response.Headers.Append("HX-Trigger", "{{EntityNameLower}}ListRefresh");
        
        return Ok();
    }

    // DELETE
    [HttpDelete]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _context.{{EntityNamePlural}}.FindAsync(id);
        if (item == null)
            return NotFound();

        _context.{{EntityNamePlural}}.Remove(item);
        await _context.SaveChangesAsync();
        
        Response.Headers.Append("HX-Trigger", "{\"showToast\": {\"type\": \"success\", \"message\": \"{{EntityName}} deleted successfully!\"}}");
        Response.Headers.Append("HX-Trigger", "{{EntityNameLower}}ListRefresh");
        
        return Ok();
    }
}
```

### 2. PaginationDto (Carestream Pattern)

```csharp
public class PaginationDto
{
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 25;
    public int TotalItems { get; set; }
    public int TotalPages { get; set; }
    
    // HTMX-specific properties (set once in controller)
    public string HxGetUrl { get; set; } = string.Empty;
    public string HxTarget { get; set; } = string.Empty;
    public string HxSwap { get; set; } = "innerHTML";
    
    public bool HasPreviousPage => CurrentPage > 1;
    public bool HasNextPage => CurrentPage < TotalPages;
}
```

### 3. View Templates

#### Index.cshtml
```cshtml
@model {{ProjectName}}.ViewModels.{{EntityName}}ListViewModel

@{
    ViewData["Title"] = "{{EntityNamePlural}}";
}

<div class="container mx-auto px-4 py-8">
    <div class="flex justify-between items-center mb-6">
        <h1 class="text-3xl font-bold">{{EntityNamePlural}}</h1>
        
        <button hx-get="@Url.Action("Create")"
                hx-target="#modal-container"
                hx-swap="innerHTML"
                class="btn btn-primary">
            Add {{EntityName}}
        </button>
    </div>

    <!-- Search Bar -->
    <div class="mb-6">
        <form hx-get="@Url.Action("Index")"
              hx-trigger="input changed delay:500ms from:#search-input, submit"
              hx-target="#{{EntityNameLower}}-list-container"
              hx-swap="innerHTML"
              hx-indicator="#search-spinner">
            <div class="flex gap-2">
                <input type="search"
                       id="search-input"
                       name="searchTerm"
                       value="@Model.SearchTerm"
                       placeholder="Search {{EntityNamePlural}}..."
                       class="input input-bordered flex-1" />
                <input type="hidden" name="pageNumber" value="1" />
                <input type="hidden" name="pageSize" value="@Model.Pagination.PageSize" />
                <span id="search-spinner" class="loading loading-spinner htmx-indicator"></span>
            </div>
        </form>
    </div>

    <!-- List Container -->
    <div id="{{EntityNameLower}}-list-container"
         hx-trigger="{{EntityNameLower}}ListRefresh from:body"
         hx-get="@Url.Action("Index")"
         hx-vals='{"pageNumber": @Model.Pagination.CurrentPage, "pageSize": @Model.Pagination.PageSize, "searchTerm": "@Model.SearchTerm"}'
         hx-swap="innerHTML">
        @await Html.PartialAsync("_{{EntityName}}List", Model)
    </div>
</div>

<!-- Modal Container -->
<div id="modal-container"></div>
```

#### _{{EntityName}}List.cshtml
```cshtml
@model {{ProjectName}}.ViewModels.{{EntityName}}ListViewModel

<div class="card bg-base-100 shadow-xl">
    <div class="card-body">
        @if (Model.Items.Any())
        {
            <div class="overflow-x-auto">
                <table class="table">
                    <thead>
                        <tr>
                            {{GenerateTableHeaders}}
                            <th>Actions</th>
                        </tr>
                    </thead>
                    <tbody>
                        @foreach (var item in Model.Items)
                        {
                            <tr>
                                {{GenerateTableCells}}
                                <td>
                                    <div class="flex gap-2">
                                        <button hx-get="@Url.Action("Details", new { id = item.Id })"
                                                hx-target="#modal-container"
                                                hx-swap="innerHTML"
                                                class="btn btn-sm btn-info">
                                            Details
                                        </button>
                                        <button hx-get="@Url.Action("Edit", new { id = item.Id })"
                                                hx-target="#modal-container"
                                                hx-swap="innerHTML"
                                                class="btn btn-sm btn-warning">
                                            Edit
                                        </button>
                                        <button hx-delete="@Url.Action("Delete", new { id = item.Id })"
                                                hx-confirm="Are you sure you want to delete this {{EntityNameLower}}?"
                                                class="btn btn-sm btn-error">
                                            Delete
                                        </button>
                                    </div>
                                </td>
                            </tr>
                        }
                    </tbody>
                </table>
            </div>

            <!-- Pagination Controls -->
            @await Html.PartialAsync("_PaginationControls", Model.Pagination)
        }
        else
        {
            <div class="text-center py-8 text-gray-500">
                <p>No {{EntityNamePlural}} found.</p>
            </div>
        }
    </div>
</div>
```

#### _PaginationControls.cshtml (Reusable!)
```cshtml
@model {{ProjectName}}.Models.PaginationDto

@if (Model.TotalPages > 1)
{
    <div class="flex justify-between items-center mt-6">
        <div class="text-sm text-gray-600">
            Showing @((Model.CurrentPage - 1) * Model.PageSize + 1) to @Math.Min(Model.CurrentPage * Model.PageSize, Model.TotalItems) of @Model.TotalItems items
        </div>

        <div class="join">
            @if (Model.HasPreviousPage)
            {
                <button hx-get="@Model.HxGetUrl"
                        hx-vals='{"pageNumber": @(Model.CurrentPage - 1), "pageSize": @Model.PageSize}'
                        hx-target="@Model.HxTarget"
                        hx-swap="@Model.HxSwap"
                        class="join-item btn">
                    «
                </button>
            }

            @{
                var startPage = Math.Max(1, Model.CurrentPage - 2);
                var endPage = Math.Min(Model.TotalPages, Model.CurrentPage + 2);
                
                for (int i = startPage; i <= endPage; i++)
                {
                    <button hx-get="@Model.HxGetUrl"
                            hx-vals='{"pageNumber": @i, "pageSize": @Model.PageSize}'
                            hx-target="@Model.HxTarget"
                            hx-swap="@Model.HxSwap"
                            class="join-item btn @(i == Model.CurrentPage ? "btn-active" : "")">
                        @i
                    </button>
                }
            }

            @if (Model.HasNextPage)
            {
                <button hx-get="@Model.HxGetUrl"
                        hx-vals='{"pageNumber": @(Model.CurrentPage + 1), "pageSize": @Model.PageSize}'
                        hx-target="@Model.HxTarget"
                        hx-swap="@Model.HxSwap"
                        class="join-item btn">
                    »
                </button>
            }
        </div>
    </div>
}
```

#### _{{EntityName}}CreateModal.cshtml
```cshtml
@model {{ProjectName}}.Models.{{EntityName}}

<dialog class="modal modal-open">
    <div class="modal-box">
        <h3 class="font-bold text-lg mb-4">Create {{EntityName}}</h3>
        
        <div id="{{EntityNameLower}}-form-container">
            @await Html.PartialAsync("_{{EntityName}}Form", Model)
        </div>
        
        <div class="modal-action">
            <button onclick="document.querySelector('.modal').close(); document.getElementById('modal-container').innerHTML = '';"
                    class="btn">
                Cancel
            </button>
        </div>
    </div>
    <form method="dialog" class="modal-backdrop">
        <button onclick="document.getElementById('modal-container').innerHTML = '';">close</button>
    </form>
</dialog>
```

#### _{{EntityName}}Form.cshtml
```cshtml
@model {{ProjectName}}.Models.{{EntityName}}

<form hx-post="@Url.Action(Model.Id == 0 ? "Create" : "Edit", new { id = Model.Id })"
      hx-target="#modal-container"
      hx-swap="innerHTML">
    
    <div asp-validation-summary="ModelOnly" class="text-error mb-4"></div>
    
    {{GenerateFormFields}}
    
    <div class="form-control mt-6">
        <button type="submit" class="btn btn-primary">
            @(Model.Id == 0 ? "Create" : "Update")
        </button>
    </div>
</form>
```

#### _{{EntityName}}Details.cshtml
```cshtml
@model {{ProjectName}}.Models.{{EntityName}}

<dialog class="modal modal-open">
    <div class="modal-box">
        <h3 class="font-bold text-lg mb-4">{{EntityName}} Details</h3>
        
        <div class="space-y-4">
            {{GenerateDetailFields}}
        </div>
        
        <div class="modal-action">
            <button onclick="document.querySelector('.modal').close(); document.getElementById('modal-container').innerHTML = '';"
                    class="btn">
                Close
            </button>
        </div>
    </div>
    <form method="dialog" class="modal-backdrop">
        <button onclick="document.getElementById('modal-container').innerHTML = '';">close</button>
    </form>
</dialog>
```

### 4. Toast Notification System

#### _Layout.cshtml addition
```cshtml
<!-- Toast Container -->
<div id="toast-container" class="toast toast-end"></div>

<script>
    // Toast notification system
    document.body.addEventListener('showToast', function(event) {
        const { type, message } = event.detail;
        const toast = document.createElement('div');
        toast.className = `alert alert-${type}`;
        toast.innerHTML = `<span>${message}</span>`;
        
        const container = document.getElementById('toast-container');
        container.appendChild(toast);
        
        setTimeout(() => toast.remove(), 3000);
    });
</script>
```

---

## Implementation Tasks

### Task 1: Create Supporting Classes
- [ ] Create `PaginationDto.cs`
- [ ] Create `{{EntityName}}ListViewModel.cs`
- [ ] Update existing templates

### Task 2: Update Controller Template
- [ ] Replace basic controller with full CRUD
- [ ] Add pagination logic
- [ ] Add search logic (generated based on string fields)
- [ ] Add HTMX response headers

### Task 3: Create View Templates
- [ ] Create `Index.cshtml` with search bar
- [ ] Create `_{{EntityName}}List.cshtml` with table + pagination
- [ ] Create `_PaginationControls.cshtml` (reusable)
- [ ] Create `_{{EntityName}}CreateModal.cshtml`
- [ ] Create `_{{EntityName}}EditModal.cshtml`
- [ ] Create `_{{EntityName}}Details.cshtml`
- [ ] Create `_{{EntityName}}Form.cshtml` (shared by create/edit)

### Task 4: Add Toast System
- [ ] Add toast container to layout
- [ ] Add toast JavaScript
- [ ] Update controllers to trigger toasts

### Task 5: Testing
- [ ] Test pagination (forward/backward)
- [ ] Test search with debouncing
- [ ] Test create modal
- [ ] Test edit modal
- [ ] Test delete with confirmation
- [ ] Test validation errors
- [ ] Test toast notifications

---

## Success Criteria

✅ Full CRUD operations work  
✅ Pagination with page size options  
✅ Search with debouncing  
✅ Modal create/edit forms  
✅ Delete confirmation  
✅ Toast notifications  
✅ Form validation display  
✅ All HTMX patterns from learned apps  
✅ Reusable pagination partial  
✅ Generated code uses field definitions  

---

## Next Phase

**Phase 4 Options:**
1. Relationships (foreign keys, navigation properties)
2. Sorting (click column headers to sort)
3. Bulk operations (select multiple, bulk delete)
4. Export (CSV, Excel, PDF)
5. Import (CSV upload)
