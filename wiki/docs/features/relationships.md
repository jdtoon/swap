# Relationships

Build real-world applications with complex data models using Swap's relationship generation features.

## Overview

Swap CLI provides comprehensive support for generating and managing relationships between entities, including:

- **Foreign key and navigation property generation**
- **EF Core Fluent API configuration**
- **Automatic UI generation** (dropdowns, collections)
- **Eager loading with `.Include()`**
- **Self-referential relationship support**

Relationships are created using the [`swap generate relationship`](../cli/generate-relationship.md) command and automatically integrated into your controllers and views.

## Implementation Status

| Relationship Type | CLI Command | UI Generator | Status |
|-------------------|-------------|--------------|--------|
| **One-to-Many** | ✅ Working | ✅ Working | **Production Ready** |
| **Many-to-One** | ✅ Working | ✅ Working | **Production Ready** |
| **Many-to-Many** | ⏳ Phase 3 | ⏳ Phase 3 | Roadmap Q1 2025 |
| **One-to-One** | ⏳ Phase 4 | ⏳ Phase 4 | Roadmap Q2 2025 |
| **Self-Referential** | ⚠️ Manual | ✅ Working | Manual model edit required |

### Current Capabilities (v0.2.0)

✅ **Fully Supported**:
- One-to-many relationships (Category → Products)
- Many-to-one relationships (Order → Customer)
- Required and optional foreign keys
- Cascade delete behaviors (Restrict, Cascade, SetNull)
- Custom FK and navigation property names
- Display field auto-detection
- Automatic migration creation
- Relationship-aware UI in controllers

⚠️ **Partial Support**:
- Self-referential relationships (manual model editing required, UI works)

⏳ **Coming Soon**:
- Many-to-many relationships with junction tables
- One-to-one relationships
- Relationship management commands (list, remove, update)
- Reverse engineering existing relationships

## Workflow Options

Swap offers three approaches to adding relationships to your application:

### Option A: CLI-First Workflow

Generate models, add relationships via CLI, then generate controllers.

```bash
# 1. Generate models
swap g m Customer --fields "Name:string Email:string"
swap g m Order --fields "OrderDate:datetime Total:decimal"

# 2. Add relationship
swap g rel --source Order --target Customer --type many-to-one --required

# 3. Generate controller with relationship UI
swap g c Order --force
```

**Best for**: New projects, complex relationship configurations

### Option B: Inference Workflow

Generate models with FK fields, controllers auto-detect relationships.

```bash
# 1. Generate models
swap g m Category --fields "Name:string"
swap g m Product --fields "Name:string CategoryId:int Price:decimal"

# 2. Generate controller (auto-detects CategoryId → Category)
swap g c Product
```

**Best for**: Simple relationships, rapid prototyping

### Option C: Manual Model Workflow

Edit models manually (for unsupported scenarios), then generate controllers.

```bash
# 1. Generate base model
swap g m Category --fields "Name:string"

# 2. Manually edit Models/Category.cs to add:
#    public int? ParentId { get; set; }
#    public Category? Parent { get; set; }
#    public ICollection<Category> Children { get; set; } = new List<Category>();

# 3. Generate controller (UI detects self-reference)
swap g c Category
```

**Best for**: Self-referential relationships, advanced scenarios

## Relationship Types

### One-to-Many

One entity (target) has a collection of another entity (source).

**Real-World Examples**:
- Blog → Posts
- Category → Products
- Customer → Orders
- Department → Employees

**Command Syntax**:

```bash
swap g rel --source Product --target Category --type many-to-one
# Or from the other direction:
swap g rel --source Category --target Product --type one-to-many
```

**What Gets Generated**:

```csharp title="Models/Product.cs"
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    
    // Foreign key (many side)
    public int CategoryId { get; set; }
    
    // Navigation property (many → one)
    public Category? Category { get; set; }
}
```

```csharp title="Models/Category.cs"
public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    
    // Collection navigation (one → many)
    public ICollection<Product> Products { get; set; } = new List<Product>();
}
```

```csharp title="Data/AppDbContext.cs"
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Product>()
        .HasOne(e => e.Category)
        .WithMany(e => e.Products)
        .HasForeignKey(e => e.CategoryId)
        .OnDelete(DeleteBehavior.Restrict);
}
```

**UI Generated**:

- **Create/Edit Forms**: Dropdown to select Category
- **List View**: Display `Category.Name` instead of `CategoryId`
- **Details View**: Link to Category details

See [One-to-Many Documentation](../cli/generate-relationship.md#one-to-many) for details.

### Many-to-One

The inverse perspective of one-to-many - many entities belong to one.

**Real-World Examples**:
- Order → Customer (many orders to one customer)
- Comment → Post (many comments on one post)
- Employee → Department (many employees in one department)

**Command Syntax**:

```bash
swap g rel --source Order --target Customer --type many-to-one --required
```

This is functionally identical to one-to-many, but expresses the relationship from the "many" side's perspective.

See [Many-to-One Documentation](../cli/generate-relationship.md#many-to-one) for details.

### Many-to-Many (Coming Soon)

⏳ **Planned for Phase 3 (Q1 2025)**

Both entities have collections of the other, connected via a junction table.

**Real-World Examples**:
- Posts ↔ Tags
- Students ↔ Courses
- Users ↔ Roles
- Products ↔ Suppliers

**Planned Syntax**:

```bash
swap g rel --source Post --target Tag --type many-to-many
```

**Will Generate**:
- Junction table entity (e.g., `PostTag`)
- DbSet for junction table
- Collection navigations on both sides
- Checkbox UI for tag selection
- Badge display in views

### One-to-One (Coming Soon)

⏳ **Planned for Phase 4 (Q2 2025)**

Each entity has exactly one of the other.

**Real-World Examples**:
- User → Profile
- Employee → EmployeeDetails
- Invoice → InvoiceMetadata

**Planned Syntax**:

```bash
swap g rel --source User --target Profile --type one-to-one --principal User
```

**Will Generate**:
- FK with unique constraint on dependent
- Single navigation on both sides
- Inline editing UI

### Self-Referential Relationships

⚠️ **Requires manual model editing**

An entity relates to itself (e.g., hierarchical data).

**Real-World Examples**:
- Category → ParentCategory
- Employee → Manager
- Comment → ParentComment

**Current Workflow**:

1. Generate the base model:
```bash
swap g m Category --fields "Name:string Slug:string"
```

2. Manually edit `Models/Category.cs`:
```csharp
public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    
    // Self-referential relationship (add manually)
    public int? ParentId { get; set; }
    public Category? Parent { get; set; }
    public ICollection<Category> Children { get; set; } = new List<Category>();
}
```

3. Generate controller - UI automatically detects self-reference:
```bash
swap g c Category
```

**UI Behavior**:
- **Create Form**: Dropdown excludes no entities (can select any parent)
- **Edit Form**: Dropdown excludes current entity (prevents circular reference)
- Navigation property uses descriptive name (`Parent`, not `Category`)

## Display Field Detection

When generating UI dropdowns, Swap automatically determines which field to display based on this priority:

1. `Name`
2. `Title`
3. `Description`
4. `Email`
5. `Code`
6. `Label`
7. First `string` property found
8. `Id` (fallback)

**Example**:

```csharp title="Models/Customer.cs"
public class Customer
{
    public int Id { get; set; }
    public string Name { get; set; }   // ← Used in dropdown
    public string Email { get; set; }
}
```

Dropdown renders as:
```html
<option value="1">John Smith</option>
<option value="2">Jane Doe</option>
```

**Override**: Use `--display` flag:
```bash
swap g rel -s Order -t Customer --type many-to-one --display Email
```

## Delete Behaviors

Configure what happens when a parent entity is deleted.

### Restrict (Default)

Prevents deletion of parent if children exist.

```bash
swap g rel -s Order -t Customer --type many-to-one --on-delete restrict
```

**Database**: Raises exception if you try to delete Customer with Orders.

**Best for**: Protecting data integrity, preventing accidental deletions.

### Cascade

Automatically deletes children when parent is deleted.

```bash
swap g rel -s Order -t Customer --type many-to-one --on-delete cascade --required
```

**Database**: Deleting Customer deletes all related Orders.

**Best for**: Dependent data (comments on posts, line items in orders).

⚠️ **Warning**: Use carefully - can delete large amounts of data.

### SetNull

Sets foreign key to NULL when parent is deleted.

```bash
swap g rel -s Order -t Customer --type many-to-one --on-delete set-null
```

**Database**: Deleting Customer sets `Order.CustomerId = NULL`.

**Requirements**: FK must be nullable (don't use `--required`).

**Best for**: Optional relationships, soft archiving.

## Eager Loading

Controllers automatically use `.Include()` for eager loading when relationships are detected.

**Without Relationships**:
```csharp
var orders = await _context.Orders.ToListAsync();
// N+1 query problem if accessing Order.Customer
```

**With Relationships**:
```csharp
var orders = await _context.Orders
    .Include(e => e.Customer)  // ← Auto-added
    .ToListAsync();
// Single query, Customer data loaded
```

This prevents N+1 query problems and improves performance.

## Validation and Safety

The relationship command validates your input to prevent common mistakes:

### Required Checks

- ✅ Source entity exists in `Models/` directory
- ✅ Target entity exists in `Models/` directory
- ✅ Valid relationship type specified
- ✅ Valid delete behavior specified

### Invalid Combinations

- ❌ `--on-delete set-null` with `--required` (can't set required FK to null)
- ❌ Self-referential relationships via CLI (use manual workflow)
- ❌ `--fk` with many-to-many (no direct FK in many-to-many)

### Idempotency

Running the relationship command multiple times is safe - it won't duplicate properties.

```bash
# First run: Adds properties
swap g rel -s Order -t Customer --type many-to-one

# Second run: Skips existing properties (no error)
swap g rel -s Order -t Customer --type many-to-one
```

## Migration Management

By default, the relationship command creates an EF Core migration automatically.

**Migration Name**: `Add{Source}To{Target}Relationship`

```bash
swap g rel -s Order -t Customer --type many-to-one
# Creates: Migrations/20250131_AddOrderToCustomerRelationship.cs
```

**Apply Migration**:
```bash
dotnet ef database update
```

**Skip Automatic Migration**:
```bash
swap g rel -s Order -t Customer --type many-to-one --skip-migrations

# Manually create later:
dotnet ef migrations add AddOrderCustomerRelationship
dotnet ef database update
```

## Complete Examples

### Blog System

Build a complete blog with posts, comments, and categories.

```bash
# Create entities
swap g m Blog --fields "Title:string Description:string"
swap g m Category --fields "Name:string Slug:string"
swap g m Post --fields "Title:string Content:string PublishedAt:datetime?"
swap g m Comment --fields "Content:string AuthorName:string AuthorEmail:string"

# Create relationships
swap g rel -s Post -t Blog --type many-to-one --required --on-delete cascade
swap g rel -s Post -t Category --type many-to-one
swap g rel -s Comment -t Post --type many-to-one --required --on-delete cascade

# Generate controllers
swap g c Blog
swap g c Category
swap g c Post --force  # Regenerate to pick up Blog and Category dropdowns
swap g c Comment --force  # Regenerate to pick up Post dropdown

# Apply migrations
dotnet ef database update

# Seed data
swap g seed Blog --count 3
swap g seed Category --count 5
swap g seed Post --count 20
swap g seed Comment --count 100
```

**Result**:
- Posts must belong to a Blog (required)
- Posts can optionally have a Category
- Comments must belong to a Post (required)
- Deleting a Blog deletes all Posts (cascade)
- Deleting a Post deletes all Comments (cascade)
- UI has dropdowns for selecting Blog, Category, and Post

### E-Commerce Catalog

Build a product catalog with categories and orders.

```bash
# Create entities
swap g m Category --fields "Name:string Description:string"
swap g m Product --fields "Name:string Price:decimal Stock:int SKU:string"
swap g m Customer --fields "Name:string Email:string Phone:string"
swap g m Order --fields "OrderDate:datetime Status:string Total:decimal"
swap g m OrderItem --fields "Quantity:int UnitPrice:decimal"

# Create relationships
swap g rel -s Product -t Category --type many-to-one
swap g rel -s Order -t Customer --type many-to-one --required
swap g rel -s OrderItem -t Order --type many-to-one --required --on-delete cascade
swap g rel -s OrderItem -t Product --type many-to-one --required

# Generate controllers
swap g c Category
swap g c Product --force
swap g c Customer
swap g c Order --force
swap g c OrderItem --force

# Apply migrations
dotnet ef database update
```

**Result**:
- Products optionally belong to Categories
- Orders must have a Customer
- OrderItems must have an Order and Product
- Deleting an Order deletes all OrderItems
- Dropdowns for selecting Category, Customer, Order, and Product

### Hierarchical Categories

Build a tree structure with self-referential relationships.

```bash
# Create entity
swap g m Category --fields "Name:string Slug:string Description:string"

# Manually edit Models/Category.cs to add:
# public int? ParentId { get; set; }
# public Category? Parent { get; set; }
# public ICollection<Category> Children { get; set; } = new List<Category>();

# Generate controller (detects self-reference)
swap g c Category

# Create migration manually
dotnet ef migrations add AddCategoryHierarchy
dotnet ef database update
```

**Result**:
- Categories can have a parent Category
- Dropdown in Edit excludes current Category (prevents circular reference)
- Create dropdown includes all Categories

## Known Limitations

Current version (0.2.0) has these limitations:

1. **No many-to-many support** - Coming in Phase 3
2. **No one-to-one support** - Coming in Phase 4
3. **Self-referential requires manual editing** - CLI validator blocks them
4. **No automatic controller update** - Must regenerate with `--force`
5. **No relationship management commands** - Can't list, remove, or update relationships via CLI
6. **No reverse engineering** - Can't detect and configure existing FK properties
7. **No build verification** - Command doesn't verify project compiles first
8. **Sorting by navigation properties** - Table headers render as sortable, but server-side sorting doesn't include navigation properties

## Troubleshooting

### Dropdown Shows Id Instead of Name

**Cause**: Target entity has no suitable display field.

**Solution**: Add a `Name`, `Title`, or other string field:
```bash
swap g m Category --fields "Name:string" --force
```

Or specify display field:
```bash
swap g rel -s Product -t Category --type many-to-one --display Code
```

### "Entity not found" Error

**Cause**: Entity file doesn't exist in `Models/` directory.

**Solution**: Generate the entity first:
```bash
swap g m Customer --fields "Name:string Email:string"
swap g rel -s Order -t Customer --type many-to-one
```

### Migration Creation Failed

**Cause**: Project has compilation errors or EF tools not installed.

**Solution**:
```bash
# Verify project compiles
dotnet build

# Install EF tools if needed
dotnet tool install --global dotnet-ef

# Skip automatic migration and create manually
swap g rel -s Order -t Customer --type many-to-one --skip-migrations
dotnet ef migrations add AddRelationship
```

### Self-Referential Blocked

**Cause**: CLI validator rejects self-referential relationships.

**Solution**: Use manual workflow (see [Self-Referential Relationships](#self-referential-relationships)).

### Can't Use SetNull with Required FK

**Cause**: Invalid combination of `--on-delete set-null` and `--required`.

**Solution**: Choose one approach:
```bash
# Option 1: Nullable FK with SetNull
swap g rel -s Order -t Customer --type many-to-one --on-delete set-null

# Option 2: Required FK with Restrict or Cascade
swap g rel -s Order -t Customer --type many-to-one --required --on-delete restrict
```

## Best Practices

### 1. Required vs Optional

Use `--required` for relationships that are essential to data integrity:

```bash
# Order MUST have a Customer
swap g rel -s Order -t Customer --type many-to-one --required

# Product MAY have a Category
swap g rel -s Product -t Category --type many-to-one
```

### 2. Choose Delete Behavior Carefully

- **Restrict**: Default, safest option, prevents accidental data loss
- **Cascade**: Use for true dependent data (comments, line items)
- **SetNull**: Use for optional relationships where history matters

### 3. Generate Models Before Relationships

Always create both entities before adding relationships:

```bash
# Good ✅
swap g m Order --fields "OrderDate:datetime"
swap g m Customer --fields "Name:string"
swap g rel -s Order -t Customer --type many-to-one

# Bad ❌
swap g m Order --fields "OrderDate:datetime"
swap g rel -s Order -t Customer --type many-to-one  # Customer doesn't exist yet!
```

### 4. Regenerate Controllers

After adding relationships, regenerate controllers to get UI updates:

```bash
swap g rel -s Product -t Category --type many-to-one
swap g c Product --force  # ← Important!
```

### 5. Batch Migrations

For multiple relationships, skip migrations and create one batch migration:

```bash
swap g rel -s Order -t Customer --type many-to-one --skip-migrations
swap g rel -s OrderItem -t Order --type many-to-one --skip-migrations
swap g rel -s OrderItem -t Product --type many-to-one --skip-migrations

# Create single migration for all changes
dotnet ef migrations add AddAllRelationships
dotnet ef database update
```

## Roadmap

### Phase 3: Many-to-Many (Q1 2025)

- Junction table generation
- Checkbox UI for tag selection
- Badge display in views
- Support for additional junction properties
- Efficient Include/ThenInclude queries

### Phase 4: One-to-One (Q2 2025)

- Principal/dependent configuration
- Unique constraint on FK
- Inline editing UI
- Optional vs required one-to-one

### Future Enhancements

- Relationship management commands (`list`, `remove`, `update`)
- Reverse engineering existing relationships
- Self-referential support via CLI
- Automatic controller updates (no `--force` needed)
- Sorting by navigation properties
- Build verification before modification

## Next Steps

- [Generate Relationship CLI Reference](../cli/generate-relationship.md)
- [Generate Controller - Relationship-Aware UI](../cli/generate-controller.md#relationship-aware-ui)
- [Database Migration Commands](../cli/database.md)
- [First Project Tutorial](../getting-started/first-project.md)

## See Also

- [Generate Model](../cli/generate-model.md)
- [Generate Controller](../cli/generate-controller.md)
- [Entity Patterns](../features/patterns.md)
- [Testing Framework](../features/testing-framework.md)
