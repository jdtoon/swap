# NetMX Component Architecture

**Last Updated**: October 24, 2025  
**Status**: Planning Phase  
**Purpose**: Define component system for NetMX CLI

---

## 🎯 Vision

**Components are the atomic building blocks of NetMX UIs** - small, reusable, HTMX-powered patterns that developers can compose into features.

### The Hierarchy

```
Template (Project Structure)
  ├─ Module (Reusable Package)
  │   ├─ Feature (Single Entity CRUD)
  │   │   └─ Component (UI Building Block) ⭐ NEW
  │   └─ Feature
  │       └─ Component
  └─ Module
      └─ Feature
          └─ Component
```

**Each level is**:
- ✅ **Independent** - Can be developed in isolation
- ✅ **Testable** - Can be tested standalone
- ✅ **Composable** - Can be wired together
- ✅ **CLI-Generated** - Scaffolded with best practices

---

## 🧩 What Is a Component?

**Definition**: A self-contained HTMX UI pattern with:
- Razor partial view (`.cshtml`)
- Optional view model class
- Optional CSS (scoped styles)
- Optional JavaScript (minimal, HTMX-enhancing)
- Clear HTMX interaction patterns

**NOT**: A component is not a full page or feature - it's a **building block**.

---

## 📦 Component Categories

### 1. **Data Display Components**
- `DataTable` - Sortable, filterable table
- `DataCard` - Card layout with data
- `StatCard` - Metric display (count, percentage)
- `Timeline` - Event timeline
- `Badge` - Status badge

### 2. **Form Components**
- `InlineEdit` - Click-to-edit pattern
- `FileUpload` - File upload with progress
- `DatePicker` - Date selection
- `AutoComplete` - Type-ahead search
- `MultiSelect` - Multi-option selector

### 3. **Action Components**
- `DeleteConfirm` - Confirmation dialog
- `Toast` - Notification
- `Modal` - Dialog window
- `Dropdown` - Action menu
- `Pagination` - Page navigation

### 4. **Search & Filter Components**
- `SearchBox` - Debounced search
- `FilterPanel` - Multi-criteria filter
- `SortButtons` - Column sorting
- `TagFilter` - Tag-based filtering

### 5. **Loading & Feedback Components**
- `LoadingSpinner` - Loading indicator
- `ProgressBar` - Progress display
- `InfiniteScroll` - Lazy loading
- `EmptyState` - No data state
- `ErrorMessage` - Error display

---

## 🏗️ Component Structure

### File Organization

**Option 1: Shared Components** (Recommended)
```
src/MyApp.Web/
├── Components/           # ⭐ NEW: Shared components
│   ├── DataTable/
│   │   ├── _DataTable.cshtml          # Razor partial
│   │   ├── DataTableViewModel.cs      # View model
│   │   ├── DataTable.css              # Scoped styles
│   │   └── DataTable.js               # Optional enhancement
│   ├── InlineEdit/
│   │   ├── _InlineEdit.cshtml
│   │   └── InlineEditViewModel.cs
│   └── Toast/
│       ├── _Toast.cshtml
│       └── Toast.css
├── Controllers/
├── Models/
├── Services/
└── Views/
    ├── Shared/
    └── Products/
        └── Index.cshtml   # Uses components
```

**Option 2: Feature-Scoped Components**
```
src/MyApp.Web/
├── Features/
│   └── Products/
│       ├── ProductController.cs
│       ├── Product.cs
│       ├── ProductService.cs
│       ├── Views/
│       │   └── Index.cshtml
│       └── Components/         # Feature-specific components
│           └── _ProductCard.cshtml
```

**Recommendation**: Use **Option 1** for reusable components, **Option 2** for feature-specific ones.

---

## 🎨 Component Example: DataTable

### _DataTable.cshtml
```html
@model NetMX.Components.DataTableViewModel

<div class="datatable" 
     data-component="datatable"
     id="@Model.Id">
    
    <!-- Header -->
    <div class="datatable-header">
        <div class="datatable-search">
            <partial name="_SearchBox" model="new { Target = Model.Id }" />
        </div>
        <div class="datatable-actions">
            @if (Model.ShowCreate)
            {
                <button class="button is-primary" 
                        hx-get="@Model.CreateUrl"
                        hx-target="#modal"
                        hx-swap="innerHTML">
                    <i class="fas fa-plus"></i> Create
                </button>
            }
        </div>
    </div>
    
    <!-- Table -->
    <div class="datatable-body">
        <table class="table is-fullwidth is-striped is-hoverable">
            <thead>
                <tr>
                    @foreach (var column in Model.Columns)
                    {
                        <th>
                            @if (column.Sortable)
                            {
                                <a hx-get="@Model.SortUrl?column=@column.Name&direction=@GetNextDirection(column)"
                                   hx-target="#@Model.Id"
                                   hx-swap="outerHTML">
                                    @column.Label
                                    <i class="fas fa-sort"></i>
                                </a>
                            }
                            else
                            {
                                @column.Label
                            }
                        </th>
                    }
                    @if (Model.ShowActions)
                    {
                        <th>Actions</th>
                    }
                </tr>
            </thead>
            <tbody>
                @foreach (var row in Model.Rows)
                {
                    <tr id="row-@row.Id">
                        @foreach (var column in Model.Columns)
                        {
                            <td>@row.GetValue(column.Name)</td>
                        }
                        @if (Model.ShowActions)
                        {
                            <td>
                                <partial name="_RowActions" model="new { Id = row.Id, EditUrl = Model.EditUrl, DeleteUrl = Model.DeleteUrl }" />
                            </td>
                        }
                    </tr>
                }
            </tbody>
        </table>
    </div>
    
    <!-- Footer -->
    @if (Model.ShowPagination)
    {
        <div class="datatable-footer">
            <partial name="_Pagination" model="new { Total = Model.Total, Page = Model.Page, PageSize = Model.PageSize }" />
        </div>
    }
</div>
```

### DataTableViewModel.cs
```csharp
namespace NetMX.Components
{
    public class DataTableViewModel
    {
        public string Id { get; set; } = $"datatable-{Guid.NewGuid():N}";
        public List<DataTableColumn> Columns { get; set; } = new();
        public List<DataTableRow> Rows { get; set; } = new();
        
        public bool ShowCreate { get; set; } = true;
        public bool ShowActions { get; set; } = true;
        public bool ShowPagination { get; set; } = true;
        
        public string? CreateUrl { get; set; }
        public string? EditUrl { get; set; }
        public string? DeleteUrl { get; set; }
        public string? SortUrl { get; set; }
        
        public int Total { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
    
    public class DataTableColumn
    {
        public string Name { get; set; } = "";
        public string Label { get; set; } = "";
        public bool Sortable { get; set; } = true;
    }
    
    public class DataTableRow
    {
        public string Id { get; set; } = "";
        public Dictionary<string, object?> Values { get; set; } = new();
        
        public object? GetValue(string columnName) => Values.GetValueOrDefault(columnName);
    }
}
```

### Usage in Feature View
```html
@model List<Product>

<partial name="~/Components/DataTable/_DataTable.cshtml" 
         model="new DataTableViewModel 
         {
             Columns = new List<DataTableColumn>
             {
                 new() { Name = nameof(Product.Name), Label = "Product Name" },
                 new() { Name = nameof(Product.Price), Label = "Price" },
                 new() { Name = nameof(Product.Category), Label = "Category" }
             },
             Rows = Model.Select(p => new DataTableRow
             {
                 Id = p.Id.ToString(),
                 Values = new Dictionary<string, object?>
                 {
                     [nameof(Product.Name)] = p.Name,
                     [nameof(Product.Price)] = p.Price.ToString("C"),
                     [nameof(Product.Category)] = p.Category?.Name
                 }
             }).ToList(),
             CreateUrl = Url.Action("Create"),
             EditUrl = Url.Action("Edit"),
             DeleteUrl = Url.Action("Delete")
         }" />
```

---

## 🛠️ CLI Integration

### Commands

```bash
# Generate component in shared location
netmx generate component DataTable

# Generate component in feature
netmx generate component ProductCard --feature Products

# Generate component from template
netmx generate component DataTable --template datatable

# List available component templates
netmx component list

# Scaffold multiple components
netmx generate component DataTable SearchBox Pagination
```

### Generated Structure

**Command**: `netmx generate component DataTable`

**Result**:
```
src/MyApp.Web/
└── Components/
    └── DataTable/
        ├── _DataTable.cshtml              # Razor partial
        ├── DataTableViewModel.cs          # View model
        ├── DataTable.css                  # Scoped styles (optional)
        └── README.md                      # Usage instructions
```

**Generated Files**:

1. **_DataTable.cshtml** - Scaffold with HTMX patterns
2. **DataTableViewModel.cs** - Strongly-typed view model
3. **DataTable.css** - Scoped CSS (BEM naming)
4. **README.md** - Usage examples, props documentation

---

## 🧪 Component Testing

### Unit Tests (View Model)
```csharp
[Fact]
public void DataTableViewModel_Should_Generate_Unique_Id()
{
    var model1 = new DataTableViewModel();
    var model2 = new DataTableViewModel();
    
    Assert.NotEqual(model1.Id, model2.Id);
}

[Fact]
public void DataTableRow_Should_Return_Null_For_Missing_Column()
{
    var row = new DataTableRow();
    
    var value = row.GetValue("NonExistent");
    
    Assert.Null(value);
}
```

### Integration Tests (Rendering)
```csharp
[Fact]
public async Task DataTable_Should_Render_Columns()
{
    var model = new DataTableViewModel
    {
        Columns = new List<DataTableColumn>
        {
            new() { Name = "Name", Label = "Product Name" }
        }
    };
    
    var html = await RenderComponent("~/Components/DataTable/_DataTable.cshtml", model);
    
    html.Should().Contain("Product Name");
}
```

### E2E Tests (Playwright)
```csharp
[Fact]
public async Task DataTable_Should_Sort_On_Column_Click()
{
    await Page.GotoAsync("/products");
    
    await Page.ClickAsync("th:has-text('Name')");
    
    await Page.WaitForSelectorAsync(".htmx-swapping");
    
    var firstRow = await Page.TextContentAsync("tbody tr:first-child td:first-child");
    Assert.Equal("Product A", firstRow);
}
```

---

## 📚 Component Library Structure

**Location**: `src/NetMX.Components/` (NEW package)

```
src/NetMX.Components/
├── NetMX.Components.csproj
├── README.md
├── DataTable/
│   ├── _DataTable.cshtml
│   ├── DataTableViewModel.cs
│   └── DataTable.css
├── SearchBox/
│   ├── _SearchBox.cshtml
│   └── SearchBoxViewModel.cs
├── Toast/
│   ├── _Toast.cshtml
│   ├── ToastViewModel.cs
│   └── Toast.css
└── ... (20+ components)
```

**Distribution**: 
- NuGet package: `NetMX.Components`
- Razor class library
- Bundled with CLI

**Usage**:
```bash
# Add components to project
netmx add components

# Or add specific components
netmx add component DataTable SearchBox
```

---

## 🎨 Design Principles

### 1. **HTMX-First**
- Every component uses HTMX for interactivity
- No heavy JavaScript frameworks
- Progressive enhancement

### 2. **Composable**
- Components can contain other components
- Clear prop interfaces
- No tight coupling

### 3. **Testable**
- View models are POCOs (easy to unit test)
- Render logic testable via integration tests
- E2E tests via Playwright

### 4. **Documented**
- Each component has README.md
- Usage examples included
- Props documented

### 5. **Accessible**
- ARIA attributes
- Keyboard navigation
- Screen reader support

### 6. **Styled but Flexible**
- Default Bulma styles
- BEM CSS for easy overrides
- Theme-aware

---

## 🚀 Implementation Plan

### Phase 1: Foundation (1 week)
- [ ] Create `NetMX.Components` package
- [ ] Define component structure (view model, partial, CSS)
- [ ] CLI: `netmx generate component` command
- [ ] CLI: Component templates (5 core components)
- [ ] Documentation: Component guidelines

### Phase 2: Core Components (2 weeks)
- [ ] DataTable (sortable, filterable)
- [ ] SearchBox (debounced)
- [ ] InlineEdit (click-to-edit)
- [ ] FileUpload (progress)
- [ ] Toast (notifications)
- [ ] Modal (dialogs)
- [ ] Pagination
- [ ] DeleteConfirm
- [ ] LoadingSpinner
- [ ] EmptyState

### Phase 3: Advanced Components (2 weeks)
- [ ] InfiniteScroll
- [ ] AutoComplete
- [ ] MultiSelect
- [ ] DatePicker
- [ ] FilterPanel
- [ ] Timeline
- [ ] StatCard
- [ ] TagFilter
- [ ] ProgressBar
- [ ] Dropdown

### Phase 4: Testing & Documentation (1 week)
- [ ] Unit tests for all view models
- [ ] Integration tests for rendering
- [ ] E2E tests for interactions
- [ ] Comprehensive documentation
- [ ] Video tutorials

---

## 💡 Key Decisions

### Decision 1: Razor Partials vs View Components?
**Choice**: Razor Partials  
**Why**: 
- ✅ Simpler (no C# class per component)
- ✅ HTMX-friendly (pure HTML)
- ✅ Easy to customize
- ❌ View Components have better testability, but partials + view models achieve similar results

### Decision 2: Scoped CSS vs Global CSS?
**Choice**: Scoped CSS (BEM naming)  
**Why**:
- ✅ No style conflicts
- ✅ Easy to override
- ✅ Better for component library
- ❌ Requires CSS bundling, but we already do this

### Decision 3: Component Package vs Bundled?
**Choice**: Separate NuGet package (`NetMX.Components`)  
**Why**:
- ✅ Optional (not forced on users)
- ✅ Versioned independently
- ✅ Can be updated without framework changes
- ✅ Clear separation of concerns

---

## 📊 Success Metrics

### Developer Experience
- **Time to add component**: < 30 seconds (`netmx add component DataTable`)
- **Time to customize**: < 5 minutes (clear props, good docs)
- **Learning curve**: < 30 minutes (comprehensive examples)

### Component Quality
- **Test coverage**: 80%+ (unit + integration + E2E)
- **Accessibility**: WCAG 2.1 AA compliant
- **Performance**: < 100ms render time
- **Bundle size**: < 50KB (all components, minified)

### Adoption
- **Usage**: 80%+ of features use at least 1 component
- **Customization**: 50%+ of components customized by users
- **Community**: 10+ community-contributed components by Month 6

---

## 🔗 Related Documents

- [TERMINOLOGY.md](TERMINOLOGY.md) - Updated with Component definition
- [CLI-IMPLEMENTATION.md](CLI-IMPLEMENTATION.md) - CLI component commands
- [E2E-TESTING-FRAMEWORK.md](E2E-TESTING-FRAMEWORK.md) - Component testing
- [HTMX-PATTERNS.md](HTMX-PATTERNS.md) - HTMX best practices

---

**Status**: Ready for implementation  
**Next**: Update CLI to support `netmx generate component`
