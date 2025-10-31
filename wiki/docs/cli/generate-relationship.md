# Generate Relationship

Generate relationships between entities with foreign keys, navigation properties, and EF Core configuration.

## Syntax

```bash
swap generate relationship [options]
swap g rel [options]  # Short alias
```

## Overview

The `swap generate relationship` command creates relationships between two existing entities by:

1. Adding foreign key properties to the appropriate entity
2. Adding navigation properties (single or collection)
3. Configuring the relationship in DbContext using Fluent API
4. Creating an Entity Framework migration (unless skipped)

This command uses Roslyn to analyze and modify your C# model files, ensuring type-safe relationship generation.

## Required Options

### `--source` / `-s`

The source entity in the relationship.

**Type**: `string`  
**Required**: Yes

```bash
swap g rel --source Order --target Customer --type one-to-many
```

### `--target` / `-t`

The target entity in the relationship.

**Type**: `string`  
**Required**: Yes

```bash
swap g rel -s Post -t Blog --type many-to-one
```

### `--type`

The type of relationship to create.

**Type**: `string`  
**Required**: Yes  
**Values**: 
- `one-to-many` / `onetomany` / `1:n` (source has collection of target)
- `many-to-one` / `manytoone` / `n:1` (source belongs to target)
- `many-to-many` / `manytomany` / `n:n` (both have collections) ⚠️ **Coming soon**
- `one-to-one` / `onetoone` / `1:1` (both have single reference) ⚠️ **Coming soon**

```bash
# One-to-many: Category has many Products
swap g rel -s Product -t Category --type many-to-one

# Many-to-one: Order belongs to Customer
swap g rel -s Order -t Customer --type many-to-one
```

## Configuration Options

### `--required` / `-r`

Make the foreign key required (NOT NULL in database).

**Type**: `bool`  
**Default**: `false`

```bash
# Optional FK (nullable int?)
swap g rel -s Order -t Customer --type many-to-one

# Required FK (non-nullable int)
swap g rel -s Order -t Customer --type many-to-one --required
```

### `--on-delete`

Specify the delete behavior for the relationship.

**Type**: `string`  
**Default**: `restrict`  
**Values**:
- `restrict` - Prevent deletion of parent if children exist
- `cascade` - Delete children when parent is deleted
- `set-null` / `setnull` - Set FK to NULL when parent is deleted (requires nullable FK)

```bash
# Prevent deleting Customer if Orders exist
swap g rel -s Order -t Customer --type many-to-one --on-delete restrict

# Delete all Orders when Customer is deleted
swap g rel -s Order -t Customer --type many-to-one --on-delete cascade --required

# Set Order.CustomerId to NULL when Customer is deleted
swap g rel -s Order -t Customer --type many-to-one --on-delete set-null
```

:::warning
Using `set-null` with `--required` is invalid - required FKs cannot be null.
:::

### `--display`

Specify which field to display in UI dropdowns.

**Type**: `string`  
**Default**: Auto-detected (Name > Title > Description > Email > Code > Label > first string > Id)

```bash
# Use Email instead of Name in Customer dropdown
swap g rel -s Order -t Customer --type many-to-one --display Email
```

### `--fk`

Customize the foreign key property name.

**Type**: `string`  
**Default**: `{Target}Id` (e.g., `CustomerId`)

```bash
# Use RelatedCustomerId instead of CustomerId
swap g rel -s Order -t Customer --type many-to-one --fk RelatedCustomerId
```

### `--nav`

Customize the navigation property name.

**Type**: `string`  
**Default**: Depends on relationship type

**For many-to-one**: Default is target entity name (e.g., `Customer`)
**For one-to-many**: Default is pluralized source (e.g., `Products`)

```bash
# Use BelongsToCustomer instead of Customer
swap g rel -s Order -t Customer --type many-to-one --nav BelongsToCustomer
```

### `--inverse`

Customize the inverse navigation property name.

**Type**: `string`  
**Default**: Pluralized source entity name (e.g., `Orders`)

```bash
# Use CustomerOrders instead of Orders on Customer
swap g rel -s Order -t Customer --type many-to-one --inverse CustomerOrders
```

### `--skip-nav`

Skip generating navigation properties (only create FK).

**Type**: `bool`  
**Default**: `false`

```bash
# Only add OrderId to Order, no navigation properties
swap g rel -s Order -t Customer --type many-to-one --skip-nav
```

### `--skip-ui`

Skip UI generation (controller updates).

**Type**: `bool`  
**Default**: `false`

:::info
Currently, UI generation happens separately via `swap g controller`. This flag is reserved for future automatic controller updates.
:::

```bash
swap g rel -s Order -t Customer --type many-to-one --skip-ui
```

### `--skip-migrations`

Skip automatic migration creation.

**Type**: `bool`  
**Default**: `false`

Use this if you manage migrations manually or want to batch multiple changes.

```bash
swap g rel -s Order -t Customer --type many-to-one --skip-migrations

# Later, create migration manually:
dotnet ef migrations add AddOrderCustomerRelationship
```

### `--project` / `-p`

Specify project directory (if not current directory).

**Type**: `string`  
**Default**: Current directory

```bash
swap g rel -s Order -t Customer --type many-to-one --project ../MyProject
```

## Many-to-Many Options (Coming Soon)

### `--junction`

Custom junction table name for many-to-many relationships.

**Type**: `string`  
**Default**: Alphabetically sorted combination (e.g., `PostTag` for Post/Tag)

```bash
# Coming in Phase 3
swap g rel -s Post -t Tag --type many-to-many --junction PostTags
```

### `--junction-props`

Additional properties for junction table.

**Type**: `string`  
**Format**: `PropertyName:type,PropertyName:type`

```bash
# Coming in Phase 3
swap g rel -s Post -t Tag --type many-to-many --junction-props "CreatedAt:datetime,CreatedBy:string"
```

## One-to-One Options (Coming Soon)

### `--principal`

Specify the principal entity in a one-to-one relationship.

**Type**: `string`

```bash
# Coming in Phase 4
swap g rel -s User -t Profile --type one-to-one --principal User
```

### `--dependent`

Specify the dependent entity in a one-to-one relationship.

**Type**: `string`

```bash
# Coming in Phase 4
swap g rel -s User -t Profile --type one-to-one --dependent Profile
```

## Relationship Types

### One-to-Many

A one-to-many relationship means one entity (target) can have multiple related entities (source).

**Example**: One Category has many Products

```bash
# Generate Product model
swap g m Product --fields "Name:string Price:decimal"

# Generate Category model
swap g m Category --fields "Name:string"

# Add relationship (Product → Category)
swap g rel -s Product -t Category --type many-to-one

# Generate controller with relationship UI
swap g c Product --force
```

**Generated Code**:

```csharp title="Models/Product.cs"
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    
    // Added by relationship command
    public int CategoryId { get; set; }
    public Category? Category { get; set; }
}
```

```csharp title="Models/Category.cs"
public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    
    // Added by relationship command
    public ICollection<Product> Products { get; set; } = new List<Product>();
}
```

```csharp title="Data/AppDbContext.cs"
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // Added by relationship command
    // Product -> Category (Many-to-One)
    modelBuilder.Entity<Product>()
        .HasOne(e => e.Category)
        .WithMany(e => e.Products)
        .HasForeignKey(e => e.CategoryId)
        .IsRequired(false)
        .OnDelete(DeleteBehavior.Restrict);
}
```

### Many-to-One

A many-to-one relationship is the same as one-to-many, but specified from the "many" side's perspective.

**Example**: Many Orders belong to one Customer

```bash
# Generate models
swap g m Customer --fields "Name:string Email:string"
swap g m Order --fields "OrderDate:datetime Total:decimal"

# Add relationship (Order → Customer)
swap g rel -s Order -t Customer --type many-to-one --required

# Generate controller with dropdown
swap g c Order --force
```

**Generated Code**:

```csharp title="Models/Order.cs"
public class Order
{
    public int Id { get; set; }
    public DateTime OrderDate { get; set; }
    public decimal Total { get; set; }
    
    // Added by relationship command
    public int CustomerId { get; set; }  // Required FK
    public Customer? Customer { get; set; }
}
```

```csharp title="Models/Customer.cs"
public class Customer
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    
    // Added by relationship command
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}
```

### Many-to-Many (Coming Soon)

⚠️ **Not yet implemented** - Planned for Phase 3

A many-to-many relationship allows both entities to have collections of the other.

**Example**: Posts have many Tags, Tags have many Posts

```bash
# Will be supported in Phase 3
swap g rel -s Post -t Tag --type many-to-many
```

### One-to-One (Coming Soon)

⚠️ **Not yet implemented** - Planned for Phase 4

A one-to-one relationship means each entity has exactly one of the other.

**Example**: User has one Profile

```bash
# Will be supported in Phase 4
swap g rel -s User -t Profile --type one-to-one
```

## Validation Rules

The relationship command validates your input and will reject invalid configurations:

### Self-Referential Relationships

⚠️ **Currently blocked** - Self-referential relationships (e.g., Category → ParentCategory) are not supported via the CLI command.

```bash
# This will fail:
swap g rel -s Category -t Category --type many-to-one
# Error: Self-referential relationships are not currently supported
```

**Workaround**: Manually add the FK and navigation properties, then regenerate the controller:

```csharp title="Models/Category.cs"
public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    
    // Manually add these:
    public int? ParentId { get; set; }
    public Category? Parent { get; set; }
    public ICollection<Category> Children { get; set; } = new List<Category>();
}
```

```bash
# Then regenerate controller - UI generator supports self-refs!
swap g c Category --force
```

### Invalid Combinations

- ❌ `--on-delete set-null` with `--required` (can't set required FK to null)
- ❌ `--fk` with many-to-many relationships (no direct FK in models)
- ❌ Source or target entity doesn't exist in `Models/` directory

### Entity Not Found

The command checks that both entities exist before proceeding:

```bash
swap g rel -s Order -t Custmer --type many-to-one
# Error: Target entity not found: Models/Custmer.cs
# Suggestion: Did you mean 'Customer'?
```

## Migration Creation

By default, the command automatically creates an EF Core migration after modifying your models.

**Migration naming**: `Add{Source}To{Target}Relationship`

**Example**: `AddProductToCategoryRelationship`

### Applying Migrations

After generation, apply the migration:

```bash
dotnet ef database update
```

### Skip Migrations

If you prefer to manage migrations manually:

```bash
swap g rel -s Order -t Customer --type many-to-one --skip-migrations

# Later, create and apply manually:
dotnet ef migrations add AddOrderCustomerRelationship
dotnet ef database update
```

## Workflow Examples

### Basic Blog Structure

```bash
# Create entities
swap g m Blog --fields "Title:string Description:string"
swap g m Post --fields "Title:string Content:string PublishedAt:datetime?"
swap g m Comment --fields "Content:string AuthorName:string"

# Create relationships
swap g rel -s Post -t Blog --type many-to-one --required
swap g rel -s Comment -t Post --type many-to-one --required

# Generate controllers with relationship UI
swap g c Blog
swap g c Post --force  # Regenerate to pick up Blog dropdown
swap g c Comment --force  # Regenerate to pick up Post dropdown
```

### E-Commerce Product Catalog

```bash
# Create entities
swap g m Category --fields "Name:string Slug:string"
swap g m Product --fields "Name:string Price:decimal Stock:int"
swap g m Order --fields "OrderDate:datetime Total:decimal Status:string"
swap g m OrderItem --fields "Quantity:int UnitPrice:decimal"

# Create relationships
swap g rel -s Product -t Category --type many-to-one
swap g rel -s OrderItem -t Order --type many-to-one --required
swap g rel -s OrderItem -t Product --type many-to-one --required

# Generate controllers
swap g c Category
swap g c Product --force
swap g c Order --force
swap g c OrderItem --force
```

### Optional vs Required Relationships

```bash
# Optional: Product can exist without Category
swap g rel -s Product -t Category --type many-to-one

# Required: Order must have Customer
swap g rel -s Order -t Customer --type many-to-one --required
```

### Custom Names

```bash
# Default names
swap g rel -s Comment -t Post --type many-to-one
# Creates: Comment.PostId, Comment.Post, Post.Comments

# Custom names
swap g rel -s Comment -t Post --type many-to-one \
  --fk BelongsToPostId \
  --nav BelongsToPost \
  --inverse PostComments
# Creates: Comment.BelongsToPostId, Comment.BelongsToPost, Post.PostComments
```

## Idempotency

The relationship command is **idempotent** - running it multiple times won't duplicate properties.

If a property already exists, the command will skip adding it.

```bash
# First run: Adds properties
swap g rel -s Order -t Customer --type many-to-one

# Second run: Skips existing properties (no error)
swap g rel -s Order -t Customer --type many-to-one
```

## Integration with Controller Generation

After creating relationships, regenerate controllers to get UI dropdowns:

```bash
# Add relationship
swap g rel -s Product -t Category --type many-to-one

# Regenerate controller to get Category dropdown
swap g c Product --force
```

The controller generator (with `--with-relationships` enabled by default) will:
- Detect the `CategoryId` foreign key
- Generate a dropdown `<select>` instead of number input
- Populate `ViewBag.CategoryList` with available categories
- Add `.Include(e => e.Category)` to queries
- Display `Category.Name` in list views

See [Generate Controller - Relationship-Aware UI](./generate-controller.md#relationship-aware-ui) for details.

## Known Limitations

1. **No UI auto-update**: After adding a relationship, you must manually regenerate controllers with `--force` flag
2. **Self-referential blocked**: Must manually add self-referential relationships
3. **No reverse engineering**: Can't detect and configure existing FK properties (coming soon)
4. **No relationship removal**: Must manually remove relationship code and create migration
5. **No build-first gate**: Unlike controller generation, this command doesn't verify your project compiles first

## Troubleshooting

### "Source entity not found"

**Cause**: The entity file doesn't exist in `Models/` directory.

**Solution**: 
```bash
# Create the entity first
swap g m Order --fields "OrderDate:datetime Total:decimal"

# Then create relationship
swap g rel -s Order -t Customer --type many-to-one
```

### "Migration creation failed"

**Cause**: Project has compilation errors or EF tools not installed.

**Solution**:
```bash
# Ensure project compiles
dotnet build

# Install EF tools if needed
dotnet tool install --global dotnet-ef

# Create migration manually
dotnet ef migrations add AddRelationship
```

### "Can't use set-null with required FK"

**Cause**: Invalid combination of `--on-delete set-null` and `--required`.

**Solution**: Choose one:
```bash
# Option 1: Use nullable FK with set-null
swap g rel -s Order -t Customer --type many-to-one --on-delete set-null

# Option 2: Use required FK with restrict or cascade
swap g rel -s Order -t Customer --type many-to-one --required --on-delete restrict
```

### Dropdown shows Id instead of Name

**Cause**: Target entity has no suitable display field.

**Solution**: Add a `Name`, `Title`, or other string field to the target entity:
```bash
# Add Name field to Category
swap g m Category --fields "Name:string" --force
```

Or specify display field explicitly:
```bash
swap g rel -s Product -t Category --type many-to-one --display Code
```

## Next Steps

- [Generate Controller](./generate-controller.md) - Create CRUD with relationship UI
- [Relationships Feature Guide](../features/relationships.md) - Conceptual overview
- [Database Commands](./database.md) - Apply migrations

## See Also

- [Generate Model](./generate-model.md)
- [Generate Controller](./generate-controller.md)
- [Entity Patterns](./generate-pattern.md)
