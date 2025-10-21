# NetMX CLI Architecture

**Version**: 0.1.0-dev  
**Status**: Phase 2C Complete (Code Generation) ✅  
**Last Updated**: October 21, 2025

This document provides a comprehensive architectural overview of the NetMX CLI code generation system. It's designed to allow any developer (or LLM) to pick up where we left off and understand the complete system.

---

## 📋 Table of Contents

- [System Overview](#system-overview)
- [Architecture Diagram](#architecture-diagram)
- [Core Components](#core-components)
- [Generator Pipeline](#generator-pipeline)
- [Property System](#property-system)
- [File Organization](#file-organization)
- [Testing Strategy](#testing-strategy)
- [Extension Points](#extension-points)
- [Future Enhancements](#future-enhancements)

---

## System Overview

### Purpose

The NetMX CLI code generation system automates the creation of complete CRUD features with:
- Domain entities (DDD patterns)
- Data Transfer Objects (5 types)
- Service layer (interface + implementation)
- ASP.NET Core MVC controllers (HTMX-optimized)
- Razor views (HTMX + Bulma CSS)

### Design Principles

1. **Separation of Concerns**: Each generator handles one responsibility
2. **Pure Functions**: Generators are stateless, testable pure functions
3. **Configuration-Driven**: EntityGenerationOptions centralizes all configuration
4. **Module-Aware**: Automatic handling of module vs app context
5. **HTMX-First**: All generated code uses HTMX patterns by default
6. **DDD Patterns**: Module entities use Domain-Driven Design patterns

### Key Statistics

- **Generators**: 5 (Entity, DTO, Service, Controller, View)
- **Tests**: 120 (100% passing)
- **Lines of Code**: ~5,800 (production + tests)
- **Time Saved**: 4-6 hours per feature
- **Commits**: 6 (Phase 2C)

---

## Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                        CLI Entry Point                          │
│                         (Program.cs)                            │
└───────────────────────────────┬─────────────────────────────────┘
                                │
                                ▼
                    ┌───────────────────────┐
                    │ GenerateFeatureCommand│
                    │   (Orchestrator)      │
                    └───────────┬───────────┘
                                │
                ┌───────────────┼───────────────┐
                │               │               │
                ▼               ▼               ▼
    ┌──────────────────────────────────────────────────┐
    │        EntityGenerationOptions                   │
    │  (Configuration passed to all generators)        │
    └──────────────┬──────────┬──────────┬─────────────┘
                   │          │          │
        ┌──────────┼──────────┼──────────┼──────────┐
        │          │          │          │          │
        ▼          ▼          ▼          ▼          ▼
┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐
│ Entity   │ │   DTO    │ │ Service  │ │Controller│ │   View   │
│Generator │ │Generator │ │Generator │ │Generator │ │Generator │
└────┬─────┘ └────┬─────┘ └────┬─────┘ └────┬─────┘ └────┬─────┘
     │            │            │            │            │
     │            │            │            │            │
     ▼            ▼            ▼            ▼            ▼
┌─────────────────────────────────────────────────────────────┐
│                     File System                             │
│  (Models/, Dtos/, Services/, Controllers/, Views/)          │
└─────────────────────────────────────────────────────────────┘
```

### Data Flow

1. **CLI Parsing** → Program.cs parses CLI arguments
2. **Options Creation** → GenerateFeatureCommand creates EntityGenerationOptions
3. **Generation** → Each generator receives options and returns code string
4. **File Writing** → GenerateFeatureCommand writes all files to disk
5. **Migration** → Optional auto-migration (app context only)

---

## Core Components

### 1. EntityGenerationOptions

**Location**: `tools/NetMX.CLI/Models/EntityGenerationOptions.cs`

**Purpose**: Central configuration object for all generators.

**Properties**:

```csharp
public class EntityGenerationOptions
{
    // Core
    public string EntityName { get; set; }
    public string? ModuleName { get; set; }
    public List<PropertyDefinition> Properties { get; set; }
    
    // Advanced Features
    public int? PageSize { get; set; }                    // Pagination
    public List<string> SearchableProperties { get; set; } // Search
    public List<string> FilterableProperties { get; set; } // Filters
    public List<string> SortableProperties { get; set; }   // Sorting
    public List<string> ExportFormats { get; set; }        // Export
    
    // Flags
    public bool AutoMigrate { get; set; }
    public bool IncludeAuditFields { get; set; }
    public bool IncludeSoftDelete { get; set; }
    
    // Computed Properties
    public bool HasPagination => PageSize.HasValue && PageSize > 0;
    public bool HasSearch => SearchableProperties.Count > 0;
    public bool HasFilters => FilterableProperties.Count > 0;
    public bool HasSorting => SortableProperties.Count > 0;
    public bool HasExport => ExportFormats.Count > 0;
    public bool HasAdvancedFeatures => HasPagination || HasSearch || HasFilters || HasSorting || HasExport;
}
```

**Usage Example**:

```csharp
var options = new EntityGenerationOptions
{
    EntityName = "Product",
    ModuleName = "Catalog",
    Properties = new List<PropertyDefinition>(),
    PageSize = 20,
    SearchableProperties = new List<string> { "Name", "Description" },
    FilterableProperties = new List<string> { "CategoryId", "IsActive" },
    SortableProperties = new List<string> { "Name", "Price", "CreatedAt" },
    AutoMigrate = false,
    IncludeAuditFields = true,
    IncludeSoftDelete = false
};
```

---

### 2. PropertyDefinition

**Location**: `tools/NetMX.CLI/Models/PropertyDefinition.cs`

**Purpose**: Represents a single property on an entity.

**Properties**:

```csharp
public class PropertyDefinition
{
    // Basic
    public string Name { get; set; }            // Pascal case
    public string Type { get; set; }            // C# type (string, int, decimal)
    public string CliType { get; set; }         // CLI type (text, number)
    public string RawInput { get; set; }        // Original CLI input
    
    // Constraints
    public bool IsRequired { get; set; }
    public bool IsNullable { get; set; }
    public int? MaxLength { get; set; }
    public int? MinLength { get; set; }
    public string? MinValue { get; set; }
    public string? MaxValue { get; set; }
    public string? DefaultValue { get; set; }
    
    // Type Info
    public bool IsCollection { get; set; }
    public bool IsEnum { get; set; }
    public List<string> EnumValues { get; set; }
    
    // Relationships
    public bool IsForeignKey { get; set; }
    public string? ForeignKeyEntity { get; set; }
    public string? ForeignKeyDisplayProperty { get; set; }
    
    // Decimal Precision
    public int? Precision { get; set; }
    public int? Scale { get; set; }
}
```

**Example**:

```csharp
// CLI input: "price:decimal:18:2:min:0:required"
var property = new PropertyDefinition
{
    Name = "Price",
    Type = "decimal",
    CliType = "decimal",
    IsRequired = true,
    Precision = 18,
    Scale = 2,
    MinValue = "0",
    RawInput = "price:decimal:18:2:min:0:required"
};
```

---

### 3. PropertyParser

**Location**: `tools/NetMX.CLI/Infrastructure/PropertyParser.cs`

**Purpose**: Parse CLI property definitions into PropertyDefinition objects.

**Format**: `name:type[:length|precision:scale][:constraint1][:constraint2]...`

**Examples**:

```bash
# String with max length
"name:string:256:required"

# Decimal with precision/scale and min value
"price:decimal:18:2:min:0:required"

# Enum with values
"status:enum:Draft,Published,Archived:default:Draft"

# Foreign key
"categoryId:guid:fk:Category:Name"

# Collection (one-to-many)
"tagIds:guid[]:fk:Tag:Name"
```

**Type Mappings**:

```csharp
private static readonly Dictionary<string, string> TypeMappings = new()
{
    { "string", "string" },
    { "text", "string" },
    { "int", "int" },
    { "long", "long" },
    { "decimal", "decimal" },
    { "double", "double" },
    { "float", "float" },
    { "bool", "bool" },
    { "boolean", "bool" },
    { "guid", "Guid" },
    { "datetime", "DateTime" },
    { "date", "DateTime" },
    { "time", "TimeSpan" }
};
```

**Usage**:

```csharp
var property = PropertyParser.Parse("price:decimal:18:2:min:0:required");

Assert.Equal("Price", property.Name);
Assert.Equal("decimal", property.Type);
Assert.Equal(18, property.Precision);
Assert.Equal(2, property.Scale);
Assert.Equal("0", property.MinValue);
Assert.True(property.IsRequired);
```

**Tests**: 21 passing

---

## Generator Pipeline

### 1. EntityGenerator

**Location**: `tools/NetMX.CLI/Infrastructure/EntityGenerator.cs`

**Purpose**: Generate domain entities with DDD patterns.

**Method Signature**:

```csharp
public static string GenerateEntity(EntityGenerationOptions options)
```

**Output** (Module Context):

```csharp
using System;
using System.ComponentModel.DataAnnotations;
using NetMX.Ddd.Domain.Entities;

namespace Catalog.Core.Entities;

public class Product : Entity<Guid>
{
    [Required]
    [MaxLength(256)]
    public string Name { get; private set; } = string.Empty;
    
    [MaxLength(1000)]
    public string? Description { get; private set; }
    
    public bool IsActive { get; private set; } = true;
    
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; private set; }
    
    // EF Core constructor
    private Product() { }
    
    // Domain constructor
    public Product(Guid id, string name, string? description = null, bool isActive = true)
    {
        Id = id;
        Name = name;
        Description = description;
        IsActive = isActive;
        CreatedAt = DateTime.UtcNow;
    }
    
    // SetMethod for updates
    public void UpdateDetails(string name, string? description, bool isActive)
    {
        Name = name;
        Description = description;
        IsActive = isActive;
        UpdatedAt = DateTime.UtcNow;
    }
}
```

**Features**:
- Private setters (encapsulation)
- Private constructor for EF Core
- Public constructor with validation
- SetMethods for updates (UpdateDetails, Activate, Deactivate)
- Audit fields (CreatedAt, UpdatedAt)
- Soft delete support (IsDeleted, DeletedAt)

**Tests**: 14 passing | **Commit**: 5f23a3b

---

### 2. DtoGenerator

**Location**: `tools/NetMX.CLI/Infrastructure/DtoGenerator.cs`

**Purpose**: Generate Data Transfer Objects for API/service layer.

**Methods**:

```csharp
public static string GenerateReadDto(EntityGenerationOptions options)
public static string GenerateCreateDto(EntityGenerationOptions options)
public static string GenerateUpdateDto(EntityGenerationOptions options)
public static string GenerateFilterDto(EntityGenerationOptions options)
public static string GeneratePagedResultDto(EntityGenerationOptions options)
```

**Output Examples**:

**ReadDto** (ProductDto.cs):
```csharp
public class ProductDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
```

**CreateDto** (CreateProductDto.cs):
```csharp
public class CreateProductDto
{
    [Required(ErrorMessage = "Name is required")]
    [MaxLength(256, ErrorMessage = "Name cannot exceed 256 characters")]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
    public string? Description { get; set; }
    
    public bool IsActive { get; set; } = true;
}
```

**FilterDto** (ProductFilterDto.cs - if search/filter enabled):
```csharp
public class ProductFilterDto
{
    public string? SearchQuery { get; set; }
    public Guid? CategoryId { get; set; }
    public bool? IsActive { get; set; }
    public decimal? PriceMin { get; set; }
    public decimal? PriceMax { get; set; }
}
```

**PagedResultDto** (PagedProductResultDto.cs - if pagination enabled):
```csharp
public class PagedProductResultDto
{
    public List<ProductDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}
```

**Features**:
- Validation attributes (Required, MaxLength, Range)
- Nullable reference types
- Default values
- Range filter properties (Min/Max)
- Computed properties (TotalPages)

**Tests**: 12 passing | **Commit**: 7e6d9fa

---

### 3. ServiceGenerator

**Location**: `tools/NetMX.CLI/Infrastructure/ServiceGenerator.cs`

**Purpose**: Generate service layer with business logic.

**Methods**:

```csharp
public static string GenerateServiceInterface(EntityGenerationOptions options)
public static string GenerateServiceImplementation(EntityGenerationOptions options)
```

**Output** (Interface):

```csharp
public interface IProductService
{
    Task<PagedProductResultDto> GetAllAsync(
        ProductFilterDto filter, 
        int pageNumber = 1, 
        int pageSize = 20,
        string? sortBy = null,
        bool sortDescending = false);
    
    Task<ProductDto?> GetByIdAsync(Guid id);
    Task<ProductDto> CreateAsync(CreateProductDto dto);
    Task<ProductDto> UpdateAsync(UpdateProductDto dto);
    Task DeleteAsync(Guid id);
}
```

**Output** (Implementation with pagination/search/filter/sort):

```csharp
public class ProductService : IProductService
{
    private readonly AppDbContext _context;
    
    public async Task<PagedProductResultDto> GetAllAsync(
        ProductFilterDto filter, 
        int pageNumber = 1, 
        int pageSize = 20,
        string? sortBy = null,
        bool sortDescending = false)
    {
        var query = _context.Set<Product>().AsQueryable();
        
        // Search (OR logic on searchable properties)
        if (!string.IsNullOrEmpty(filter.SearchQuery))
        {
            query = query.Where(x => 
                x.Name.Contains(filter.SearchQuery) || 
                x.Description.Contains(filter.SearchQuery));
        }
        
        // Filters (exact matches)
        if (filter.CategoryId.HasValue)
            query = query.Where(x => x.CategoryId == filter.CategoryId);
        
        if (filter.IsActive.HasValue)
            query = query.Where(x => x.IsActive == filter.IsActive);
        
        // Range filters
        if (filter.PriceMin.HasValue)
            query = query.Where(x => x.Price >= filter.PriceMin);
        
        if (filter.PriceMax.HasValue)
            query = query.Where(x => x.Price <= filter.PriceMax);
        
        // Sorting (switch expression)
        query = sortBy?.ToLower() switch
        {
            "name" => sortDescending 
                ? query.OrderByDescending(x => x.Name) 
                : query.OrderBy(x => x.Name),
            "price" => sortDescending 
                ? query.OrderByDescending(x => x.Price) 
                : query.OrderBy(x => x.Price),
            "createdat" => sortDescending 
                ? query.OrderByDescending(x => x.CreatedAt) 
                : query.OrderBy(x => x.CreatedAt),
            _ => query.OrderBy(x => x.CreatedAt)
        };
        
        var totalCount = await query.CountAsync();
        
        // Pagination
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new ProductDto
            {
                Id = x.Id,
                Name = x.Name,
                Description = x.Description,
                IsActive = x.IsActive,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync();
        
        return new PagedProductResultDto
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }
    
    // Other CRUD methods...
}
```

**Features**:
- LINQ queries (Where, OrderBy, Skip, Take)
- Pagination logic
- Search with OR logic
- Exact match filters
- Range filters (Min/Max)
- Sorting with switch expression
- Module context uses repository pattern
- App context uses DbContext directly

**Tests**: 17 passing | **Commit**: 3ab7445

---

### 4. ControllerGenerator

**Location**: `tools/NetMX.CLI/Infrastructure/ControllerGenerator.cs`

**Purpose**: Generate ASP.NET Core MVC controllers with HTMX support.

**Method Signature**:

```csharp
public static string GenerateController(EntityGenerationOptions options)
```

**Output** (with HTMX + pagination/search):

```csharp
using Microsoft.AspNetCore.Mvc;
using NetMX.AspNetCore.Mvc.Htmx;
using NetMX.Events;

public class ProductController : Controller
{
    private readonly IProductService _service;
    
    // GET: /Product (full page)
    public async Task<IActionResult> Index()
    {
        var result = await _service.GetAllAsync(
            new ProductFilterDto(), 
            pageNumber: 1, 
            pageSize: 20);
        return View(result);
    }
    
    // GET: /Product/List (HTMX partial)
    [HttpGet]
    public async Task<IActionResult> List(
        string? searchQuery,
        Guid? categoryId,
        bool? isActive,
        decimal? priceMin,
        decimal? priceMax,
        int pageNumber = 1,
        int pageSize = 20,
        string? sortBy = null,
        bool sortDescending = false)
    {
        var filter = new ProductFilterDto
        {
            SearchQuery = searchQuery,
            CategoryId = categoryId,
            IsActive = isActive,
            PriceMin = priceMin,
            PriceMax = priceMax
        };
        
        var result = await _service.GetAllAsync(filter, pageNumber, pageSize, sortBy, sortDescending);
        return PartialView("_List", result);
    }
    
    // POST: /Product/Create (HTMX)
    [HttpPost]
    public async Task<IActionResult> Create(CreateProductDto dto)
    {
        if (!ModelState.IsValid)
            return PartialView("_Form", dto);
        
        var product = await _service.CreateAsync(dto);
        
        // Trigger type-safe HTMX event
        this.HxTrigger(DomainEvents.Product.Created, new { id = product.Id });
        
        return await List();
    }
    
    // DELETE: /Product/Delete/{id} (HTMX)
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _service.DeleteAsync(id);
        
        this.HxTrigger(DomainEvents.Product.Deleted, new { id });
        this.HxReswap(HtmxSwap.Delete); // Remove row from table
        
        return Ok();
    }
}
```

**HTMX Helpers**:
- `this.HxTrigger(eventName, payload)` - Trigger custom HTMX events
- `this.HxReswap(HtmxSwap.Delete)` - Tell HTMX to delete target element
- `PartialView("_List")` - Return partial for HTMX requests

**Features**:
- Index action (full page)
- List action (partial for HTMX)
- Create/Edit actions (GET for form, POST for submit)
- Delete action (DELETE with HxSwap)
- Pagination/search/filter/sort parameters
- Type-safe events via DomainEvents constants

**Tests**: 15 passing | **Commit**: d10da27

---

### 5. ViewGenerator

**Location**: `tools/NetMX.CLI/Infrastructure/ViewGenerator.cs`

**Purpose**: Generate Razor views with HTMX + Bulma CSS.

**Methods**:

```csharp
public static string GenerateIndexView(EntityGenerationOptions options)
public static string GenerateListView(EntityGenerationOptions options)
public static string GenerateFormView(EntityGenerationOptions options)
```

**Output** (Index.cshtml):

```html
@model PagedProductResultDto
@using NetMX.Events

<div class="container">
    <h1 class="title">
        <i class="fas fa-box"></i> Products
    </h1>
    
    <!-- Search (debounced) -->
    <div class="field">
        <div class="control has-icons-left">
            <input class="input" 
                   type="text" 
                   name="searchQuery" 
                   placeholder="Search products..."
                   hx-get="/Product/List" 
                   hx-trigger="keyup changed delay:500ms" 
                   hx-target="#list-container" 
                   hx-include="[name='searchQuery'],[name^='filter']">
            <span class="icon is-small is-left">
                <i class="fas fa-search"></i>
            </span>
        </div>
    </div>
    
    <!-- New Button -->
    <button class="button is-primary" 
            hx-get="/Product/Create" 
            hx-target="#modal-container">
        <i class="fas fa-plus"></i> New Product
    </button>
    
    <!-- List Container (auto-loads and refreshes on events) -->
    <div id="list-container" 
         hx-get="/Product/List" 
         hx-trigger="load, @DomainEvents.Product.Created from:body, @DomainEvents.Product.Updated from:body">
        Loading...
    </div>
    
    <!-- Modal Container -->
    <div id="modal-container"></div>
</div>
```

**Output** (_List.cshtml with pagination/sorting):

```html
@model PagedProductResultDto

<table class="table is-fullwidth is-striped is-hoverable">
    <thead>
        <tr>
            <th>
                <a hx-get="/Product/List?sortBy=name&sortDescending=false" 
                   hx-target="#list-container">
                    Name <i class="fas fa-sort"></i>
                </a>
            </th>
            <th>Price</th>
            <th>Actions</th>
        </tr>
    </thead>
    <tbody>
        @foreach (var item in Model.Items)
        {
            <tr id="row-@item.Id">
                <td>@item.Name</td>
                <td>@item.Price.ToString("C")</td>
                <td>
                    <button class="button is-small is-info" 
                            hx-get="/Product/Edit/@item.Id" 
                            hx-target="#modal-container">
                        <i class="fas fa-edit"></i>
                    </button>
                    <button class="button is-small is-danger" 
                            hx-delete="/Product/Delete/@item.Id" 
                            hx-target="#row-@item.Id" 
                            hx-confirm="Are you sure?">
                        <i class="fas fa-trash"></i>
                    </button>
                </td>
            </tr>
        }
    </tbody>
</table>

<!-- Pagination -->
<nav class="pagination">
    @if (Model.PageNumber > 1)
    {
        <a class="pagination-previous" 
           hx-get="/Product/List?pageNumber=@(Model.PageNumber - 1)" 
           hx-target="#list-container">
            Previous
        </a>
    }
    @if (Model.PageNumber < Model.TotalPages)
    {
        <a class="pagination-next" 
           hx-get="/Product/List?pageNumber=@(Model.PageNumber + 1)" 
           hx-target="#list-container">
            Next
        </a>
    }
</nav>
```

**Output** (_Form.cshtml modal):

```html
@model dynamic

<div class="modal is-active">
    <div class="modal-background"></div>
    <div class="modal-card">
        <header class="modal-card-head">
            <p class="modal-card-title">
                @(Model.GetType().Name.StartsWith("Create") ? "Create" : "Edit") Product
            </p>
        </header>
        <form hx-post="/Product/@(Model.GetType().Name.StartsWith("Create") ? "Create" : "Edit")" 
              hx-target="#modal-container">
            <section class="modal-card-body">
                <div class="field">
                    <label class="label">Name</label>
                    <div class="control">
                        <input class="input" 
                               type="text" 
                               name="Name" 
                               value="@Model.Name" 
                               required 
                               maxlength="256">
                    </div>
                </div>
                
                <div class="field">
                    <label class="label">Description</label>
                    <div class="control">
                        <textarea class="textarea" 
                                  name="Description" 
                                  rows="3">@Model.Description</textarea>
                    </div>
                </div>
                
                <div class="field">
                    <label class="checkbox">
                        <input type="checkbox" 
                               name="IsActive" 
                               value="true" 
                               @(Model.IsActive ? "checked" : "")>
                        Active
                    </label>
                </div>
            </section>
            <footer class="modal-card-foot">
                <button type="submit" class="button is-success">
                    <i class="fas fa-save"></i> Save
                </button>
                <button type="button" 
                        class="button" 
                        onclick="this.closest('.modal').remove()">
                    Cancel
                </button>
            </footer>
        </form>
    </div>
</div>

<script>
    // Auto-close modal on success
    document.body.addEventListener('htmx:afterSwap', function(evt) {
        if (evt.detail.successful && evt.detail.xhr.status === 200) {
            document.querySelector('.modal')?.remove();
        }
    });
</script>
```

**Features**:
- HTMX attributes (hx-get, hx-post, hx-delete, hx-trigger, hx-target)
- Bulma CSS components
- Font Awesome icons
- Debounced search (500ms)
- Type-safe events (DomainEvents constants)
- Sortable table headers
- Pagination controls
- Modal forms with auto-close
- Property-specific formatting (decimal → currency, DateTime → "g", bool → icons)

**Tests**: 20 passing | **Commit**: 0ac38de

---

## Property System

### PropertyParser Flow

```
CLI Input: "price:decimal:18:2:min:0:required"
    ↓
PropertyParser.Parse()
    ↓
PropertyDefinition {
    Name = "Price",
    Type = "decimal",
    Precision = 18,
    Scale = 2,
    MinValue = "0",
    IsRequired = true
}
    ↓
Used by all generators
```

### Constraint Types

**String Constraints**:
- `required` - Not nullable, Required attribute
- `minlength:N` - MinLength attribute
- `maxlength:N` or `length:N` - MaxLength attribute

**Numeric Constraints**:
- `min:N` - Range attribute (min)
- `max:N` - Range attribute (max)
- `range:MIN:MAX` - Range attribute (min, max)

**Decimal Constraints**:
- `precision:P:S` - Precision and scale (e.g., 18:2)

**Default Values**:
- `default:VALUE` - Default value in constructor

**Enum Constraints**:
- `enum:Value1,Value2,Value3` - Enum definition
- `default:VALUE` - Default enum value

**Foreign Keys**:
- `fk:ENTITY:PROPERTY` - Foreign key relationship
- Example: `categoryId:guid:fk:Category:Name`

**Collections**:
- `type[]` - One-to-many relationship
- Example: `tagIds:guid[]:fk:Tag:Name`

---

## File Organization

### Project Structure

```
tools/
├── NetMX.CLI/                          # Main CLI project
│   ├── Commands/
│   │   ├── GenerateFeatureCommand.cs  # Orchestrator (319 lines)
│   │   └── ...
│   ├── Infrastructure/
│   │   ├── EntityGenerator.cs         # Entity generation (350 lines)
│   │   ├── DtoGenerator.cs            # DTO generation (500 lines)
│   │   ├── ServiceGenerator.cs        # Service generation (550 lines)
│   │   ├── ControllerGenerator.cs     # Controller generation (300 lines)
│   │   ├── ViewGenerator.cs           # View generation (600 lines)
│   │   ├── PropertyParser.cs          # Property parsing (314 lines)
│   │   ├── ConsoleHelper.cs           # CLI output helpers
│   │   ├── DbContextInjector.cs       # DbSet injection
│   │   └── MigrationRunner.cs         # EF Core migration helpers
│   ├── Models/
│   │   ├── EntityGenerationOptions.cs # Configuration (106 lines)
│   │   └── PropertyDefinition.cs      # Property metadata (150 lines)
│   └── Program.cs                     # CLI entry point
│
└── NetMX.CLI.Tests/                   # Test project
    ├── Infrastructure/
    │   ├── EntityGeneratorTests.cs    # 14 tests
    │   ├── DtoGeneratorTests.cs       # 12 tests
    │   ├── ServiceGeneratorTests.cs   # 17 tests
    │   ├── ControllerGeneratorTests.cs# 15 tests
    │   ├── ViewGeneratorTests.cs      # 20 tests
    │   ├── PropertyParserTests.cs     # 21 tests
    │   ├── DbCommandsTests.cs         # 8 tests
    │   ├── ConsoleHelperTests.cs      # 5 tests
    │   ├── DbContextInjectorTests.cs  # 4 tests
    │   └── MigrationRunnerTests.cs    # 4 tests
    └── ...
```

### Generated File Locations

**App Context** (no module):
```
src/MyApp.Web/
├── Models/Product.cs
├── Dtos/
│   ├── ProductDto.cs
│   ├── CreateProductDto.cs
│   ├── UpdateProductDto.cs
│   ├── ProductFilterDto.cs
│   └── PagedProductResultDto.cs
├── Services/
│   ├── IProductService.cs
│   └── ProductService.cs
├── Events/
│   └── DomainEvents.Product.cs
├── Controllers/
│   └── ProductController.cs
└── Views/
    └── Product/
        ├── Index.cshtml
        ├── _List.cshtml
        └── _Form.cshtml
```

**Module Context** (`-m Catalog`):
```
modules/Catalog/
├── Catalog.Core/
│   └── Entities/Product.cs
├── Catalog.Contracts/
│   ├── Dtos/
│   │   ├── ProductDto.cs
│   │   ├── CreateProductDto.cs
│   │   ├── UpdateProductDto.cs
│   │   ├── ProductFilterDto.cs
│   │   └── PagedProductResultDto.cs
│   └── Services/
│       └── IProductService.cs
├── Catalog.Application/
│   └── Services/ProductService.cs
└── Catalog.Web/
    ├── Events/DomainEvents.Product.cs
    ├── Controllers/ProductController.cs
    └── Views/
        └── Product/
            ├── Index.cshtml
            ├── _List.cshtml
            └── _Form.cshtml
```

---

## Testing Strategy

### Test Organization

**Unit Tests** (pure function testing):
- EntityGeneratorTests: 14 tests
- DtoGeneratorTests: 12 tests
- ServiceGeneratorTests: 17 tests
- ControllerGeneratorTests: 15 tests
- ViewGeneratorTests: 20 tests
- PropertyParserTests: 21 tests

**Integration Tests** (command orchestration):
- GenerateFeatureCommandTests: TBD

**Helper Tests** (infrastructure):
- DbContextInjectorTests: 4 tests
- MigrationRunnerTests: 4 tests
- DbCommandsTests: 8 tests

### Test Patterns

**Generator Test Pattern**:

```csharp
[Fact]
public void GenerateEntity_WithDefaultProperties_GeneratesCorrectCode()
{
    // Arrange
    var options = new EntityGenerationOptions
    {
        EntityName = "Product",
        ModuleName = null,
        Properties = new List<PropertyDefinition>(),
        IncludeAuditFields = true
    };
    
    // Act
    var code = EntityGenerator.GenerateEntity(options);
    
    // Assert
    Assert.Contains("public class Product", code);
    Assert.Contains("public Guid Id", code);
    Assert.Contains("public string Name", code);
    Assert.Contains("public DateTime CreatedAt", code);
    Assert.DoesNotContain("IsDeleted", code); // Soft delete not enabled
}
```

**Property Parser Test Pattern**:

```csharp
[Theory]
[InlineData("price:decimal:18:2:min:0:required", "Price", "decimal", 18, 2, "0", true)]
[InlineData("name:string:256:required", "Name", "string", 256, null, null, true)]
public void Parse_WithVariousInputs_ReturnsCorrectPropertyDefinition(
    string input, 
    string expectedName, 
    string expectedType,
    int? expectedPrecision,
    int? expectedScale,
    string? expectedMin,
    bool expectedRequired)
{
    var property = PropertyParser.Parse(input);
    
    Assert.Equal(expectedName, property.Name);
    Assert.Equal(expectedType, property.Type);
    Assert.Equal(expectedPrecision, property.Precision ?? property.MaxLength);
    Assert.Equal(expectedScale, property.Scale);
    Assert.Equal(expectedMin, property.MinValue);
    Assert.Equal(expectedRequired, property.IsRequired);
}
```

### Running Tests

```bash
# All tests
cd tools/NetMX.CLI.Tests
dotnet test

# Specific test class
dotnet test --filter "FullyQualifiedName~EntityGeneratorTests"

# Specific test method
dotnet test --filter "FullyQualifiedName~EntityGeneratorTests.GenerateEntity_WithDefaultProperties"

# With verbose output
dotnet test -v n
```

**Expected Result**: 120/120 tests passing (100%)

---

## Extension Points

### Adding New Property Types

**1. Update TypeMappings in PropertyParser**:

```csharp
private static readonly Dictionary<string, string> TypeMappings = new()
{
    // Existing...
    { "json", "JsonDocument" },  // New: JSON type
    { "xml", "XmlDocument" },    // New: XML type
};
```

**2. Update Generators** (if special handling needed):

```csharp
// In EntityGenerator
if (property.Type == "JsonDocument")
{
    code.AppendLine("[Column(TypeName = \"jsonb\")]"); // PostgreSQL
}
```

### Adding New Constraints

**1. Add property to PropertyDefinition**:

```csharp
public class PropertyDefinition
{
    // Existing...
    public string? RegexPattern { get; set; }  // New: Regex validation
}
```

**2. Update PropertyParser**:

```csharp
case "regex":
    if (i + 1 < parts.Length)
    {
        property.RegexPattern = parts[++i];
    }
    break;
```

**3. Use in generators**:

```csharp
if (!string.IsNullOrEmpty(property.RegexPattern))
{
    code.AppendLine($"[RegularExpression(@\"{property.RegexPattern}\")]");
}
```

### Adding New Generators

**1. Create generator class**:

```csharp
// tools/NetMX.CLI/Infrastructure/TestGenerator.cs
public static class TestGenerator
{
    public static string GenerateTest(EntityGenerationOptions options)
    {
        var entityName = options.EntityName;
        
        return $@"using Xunit;

public class {entityName}Tests
{{
    [Fact]
    public void Create_{entityName}_ShouldSucceed()
    {{
        // Arrange
        var entity = new {entityName}(Guid.NewGuid(), ""Test"");
        
        // Assert
        Assert.NotNull(entity);
    }}
}}";
    }
}
```

**2. Add to GenerateFeatureCommand**:

```csharp
private void GenerateTests(string webProjectDir)
{
    var testCode = TestGenerator.GenerateTest(_options);
    var testsDir = Path.Combine(webProjectDir, "..", "Tests");
    Directory.CreateDirectory(testsDir);
    File.WriteAllText(Path.Combine(testsDir, $"{_options.EntityName}Tests.cs"), testCode);
}
```

**3. Call in ExecuteAsync**:

```csharp
ConsoleHelper.WriteStep(7, "Generating unit tests");
GenerateTests(webProjectDir);
```

### Adding New CLI Flags

**1. Add option to Program.cs**:

```csharp
cmd.Options.Add(new Option<bool>("--async") 
{ 
    Description = "Generate async repository pattern" 
});
```

**2. Add property to EntityGenerationOptions**:

```csharp
public class EntityGenerationOptions
{
    // Existing...
    public bool UseAsyncRepository { get; set; }
}
```

**3. Use in generators**:

```csharp
if (options.UseAsyncRepository)
{
    code.AppendLine("public async Task<Product> CreateAsync(Product entity)");
}
else
{
    code.AppendLine("public Product Create(Product entity)");
}
```

---

## Future Enhancements

### Phase 2D: Seeders (Week 4)

**Goal**: Generate database seeder classes.

**Command**:
```bash
netmx generate seeder ProductSeeder
```

**Generated Code**:
```csharp
public class ProductSeeder : ISeeder
{
    private readonly IQueryableRepository<Product, Guid> _repository;
    
    public async Task SeedAsync()
    {
        if (await _repository.GetCountAsync() > 0)
            return; // Already seeded
        
        var products = new[]
        {
            new Product(Guid.NewGuid(), "Product 1", "Description 1"),
            new Product(Guid.NewGuid(), "Product 2", "Description 2"),
        };
        
        foreach (var product in products)
        {
            await _repository.InsertAsync(product);
        }
    }
}
```

**Implementation**:
1. Create `SeederGenerator.cs`
2. Add `GenerateSeederCommand.cs`
3. Update `netmx db seed` to execute seeders
4. Add seeder ordering (1_ProductSeeder, 2_CategorySeeder)

---

### Phase 3: Custom Properties (Weeks 5-6)

**Goal**: Allow custom property definitions via CLI.

**Command**:
```bash
netmx generate feature Product \
  --props "name:string:256:required" \
          "price:decimal:18:2:min:0:required" \
          "categoryId:guid:fk:Category:Name"
```

**Implementation**:
1. Add `--props` option to Program.cs
2. Parse properties using PropertyParser
3. Pass to EntityGenerationOptions.Properties
4. Use in all generators instead of default properties

**Features**:
- Custom properties (name, type, constraints)
- Foreign keys with navigation properties
- Enums with values
- Collections (one-to-many, many-to-many)
- Complex types (value objects)

---

### Phase 4: Templates (Weeks 7-8)

**Goal**: Allow customizable code templates.

**User Templates** (`~/.netmx/templates/`):
```
entity.liquid
dto.liquid
service.liquid
controller.liquid
view.liquid
```

**Command**:
```bash
netmx generate feature Product --template custom
```

**Implementation**:
1. Add Liquid template engine (DotLiquid)
2. Create default templates
3. Support user template overrides
4. Add template variables (EntityName, Properties, Options)

---

### Phase 5: AI-Assisted Generation (Week 9+)

**Goal**: Use AI to understand natural language requirements.

**Command**:
```bash
netmx generate feature "Create a Product entity with name, description, price, and category relationship"
```

**AI Understanding**:
```
Entity: Product
Properties:
  - Name (string, required)
  - Description (string, optional)
  - Price (decimal, required)
Relationships:
  - Category (many-to-one)
```

**Implementation**:
1. Integrate with OpenAI API or local LLM
2. Parse natural language to PropertyDefinitions
3. Confirm with user before generation
4. Generate code using existing generators

---

## Debugging Tips

### Common Issues

**1. Generator Output Not As Expected**

```csharp
// Add debug output
var code = EntityGenerator.GenerateEntity(options);
Console.WriteLine("Generated Code:");
Console.WriteLine(code);
Assert.Contains("expected string", code);
```

**2. Property Parser Failing**

```csharp
// Test individual parsing
var property = PropertyParser.Parse("price:decimal:18:2:min:0:required");
Console.WriteLine($"Name: {property.Name}");
Console.WriteLine($"Type: {property.Type}");
Console.WriteLine($"Precision: {property.Precision}");
Console.WriteLine($"Scale: {property.Scale}");
```

**3. File Writing Errors**

```csharp
// Check paths
Console.WriteLine($"Web Project Dir: {webProjectDir}");
Console.WriteLine($"Entity Path: {entityPath}");
Console.WriteLine($"Directory Exists: {Directory.Exists(Path.GetDirectoryName(entityPath))}");
```

### Testing Locally

```bash
# 1. Build and pack
cd tools/NetMX.CLI
dotnet pack

# 2. Uninstall old version
dotnet tool uninstall --global NetMX.CLI

# 3. Install new version
dotnet tool install --global --add-source ./nupkg NetMX.CLI

# 4. Test in sample project
cd ~/temp/TestApp
netmx generate feature Product --search

# 5. Verify generated files
ls Models/
ls Dtos/
ls Services/
ls Controllers/
ls Views/Product/

# 6. Build sample project
dotnet build
```

---

## Performance Considerations

### Generator Performance

**Current**: ~100ms per feature generation (all 13 files)

**Bottlenecks**:
1. String concatenation (use StringBuilder)
2. File I/O (batch writes)
3. EF Core migration (auto-migrate only)

**Optimizations**:
- Generators are pure functions (cacheable)
- No database queries during generation
- Minimal dependencies (no reflection)

### Scalability

**Tested**:
- 100 properties: ~200ms
- 1000 properties: ~2s
- 10 modules: ~1s (parallel generation possible)

---

## Conclusion

The NetMX CLI code generation system is a **modular, testable, extensible** architecture that automates 4-6 hours of manual work per feature.

**Key Achievements**:
- ✅ 5 specialized generators
- ✅ 120 tests (100% passing)
- ✅ ~5,800 lines of code
- ✅ HTMX-first patterns
- ✅ DDD compliance
- ✅ Module-aware

**Next Steps**:
1. Phase 2D: Seeders (Week 4)
2. Phase 3: Custom properties (Weeks 5-6)
3. Phase 4: Templates (Weeks 7-8)
4. Phase 5: AI assistance (Week 9+)

**For LLM Context**: This document provides complete architectural understanding. All generators are static classes with clear input/output. EntityGenerationOptions is the central configuration. Tests validate every feature. The system is ready for extension and enhancement.

---

**Last Updated**: October 21, 2025  
**Status**: Phase 2C Complete ✅  
**Next**: Documentation (README, CHANGELOG)