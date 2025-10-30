# generate pattern

Add battle-tested entity patterns to your models with single commands.

```bash
swap generate pattern <type> <entity> [options]
swap g pattern <type> <entity>  # alias
swap g p <type> <entity>         # short alias
```

## Overview

The `generate pattern` command applies common entity patterns to your existing models. These patterns are extracted from production applications and provide robust, tested solutions for:

- **Soft Delete** - Hide deleted records instead of removing them from the database
- **Auditable** - Automatic timestamp and user tracking (coming soon)
- **Sluggable** - SEO-friendly URL generation with collision handling (coming soon)

Patterns are **opt-in by design** - they're not automatically added when generating models. This gives you full control over which entities need which patterns.

## Pattern Types

### `remove` - Remove Pattern

Removes a previously applied pattern from an entity, cleaning up interfaces, properties, and shared wiring when safe.

```bash
swap g pattern remove <entity> <pattern-type>
swap g p remove Post softdelete
```

**What it does:**
1. Removes the pattern interface (e.g., `ISoftDeletable`) from your entity
2. Removes pattern-specific properties from your entity
3. Updates `swap-config.json` to track the removal
4. Cleans up shared wiring in `DbContext` and `Program.cs` **only if no other entities use it**:
   - Removes global query filters (if no entities have SoftDelete)
   - Removes audit interceptor (if no entities have Auditable)
   - Removes timestamp interceptor (if no entities have Timestampable)
   - Removes slug unique index for the specific entity (Sluggable)

**Supported patterns for removal:**
- `softdelete` - Removes soft delete pattern
- `auditable` - Removes auditable pattern
- `timestampable` - Removes timestampable pattern
- `sluggable` - Removes sluggable pattern

**Example:**
```bash
# Remove soft delete from Post
swap g p remove Post softdelete

# Remove auditable from Product
swap g p remove Product auditable
```

**What happens to the columns:**

By default, the removal is **non-destructive** - columns remain in the database:
- Preserves existing data
- Allows manual cleanup or data migration
- You can still access the columns programmatically if needed

**To drop the columns from the database:**

1. After pattern removal, manually create a migration:
   ```bash
   dotnet ef migrations add RemoveSoftDeleteFromPost
   ```

2. Edit the generated migration to drop the columns:
   ```csharp
   protected override void Up(MigrationBuilder migrationBuilder)
   {
       migrationBuilder.DropColumn(name: "IsDeleted", table: "Posts");
       migrationBuilder.DropColumn(name: "DeletedAt", table: "Posts");
       migrationBuilder.DropColumn(name: "DeletedBy", table: "Posts");
   }
   ```

3. Apply the migration:
   ```bash
   dotnet ef database update
   ```

**When to use:**
- Pattern is no longer needed for an entity
- Simplifying entity structure
- Removing unused features during refactoring

**Important notes:**
- ⚠️ Shared wiring (global filters, interceptors) is only removed when **no entities** use that pattern
- ⚠️ The removal checks `swap-config.json` to determine if wiring is still needed
- ⚠️ Always review changes and test after removal
- ⚠️ Database columns are NOT automatically dropped - you must migrate manually if desired

### `softdelete` - Soft Delete Pattern

Implements logical deletion where records are marked as deleted but remain in the database for auditing, recovery, or compliance reasons.

```bash
swap g pattern softdelete Post
swap g p soft Post  # alias
```

**What it does:**
1. Adds `ISoftDeletable` interface to your entity
2. Adds three properties: `IsDeleted`, `DeletedAt`, `DeletedBy`
3. Adds `using Swap.Patterns.SoftDelete` statement
4. Ensures `Swap.Patterns` package reference exists

**Generated code:**
```csharp
using Swap.Patterns.SoftDelete;

namespace MyApp.Models;

public class Post : ISoftDeletable
{
    public int Id { get; set; }
    public string Title { get; set; }
    
    // ISoftDeletable properties
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}
```

**Next steps after generation:**

1. **Configure query filter** in your `DbContext`:
   ```csharp
   protected override void OnModelCreating(ModelBuilder modelBuilder)
   {
       base.OnModelCreating(modelBuilder);
       modelBuilder.ConfigureSoftDeleteFilter();
   }
   ```

2. **Generate and apply migration:**
   ```bash
   dotnet ef migrations add AddSoftDeleteToPost
   dotnet ef database update
   ```

3. **Use in your code:**
   ```csharp
   // Soft delete
   post.SoftDelete("user@example.com");
   await db.SaveChangesAsync();
   
   // Restore
   post.Restore();
   await db.SaveChangesAsync();
   
   // Query deleted records
   var deleted = await db.Posts.OnlyDeleted().ToListAsync();
   
   // Include deleted in results
   var all = await db.Posts.IncludeDeleted().ToListAsync();
   
   // Normal queries automatically exclude deleted
   var active = await db.Posts.ToListAsync();
   ```

**When to use:**
- Regulatory compliance (GDPR, HIPAA) requiring data retention
- Audit trails for deleted records
- User-initiated deletions that may need to be undone
- Cascading delete scenarios where you want to preserve relationships

**Key benefits:**
- Data is never lost - can be recovered if needed
- Query filters automatically exclude deleted records
- Audit trail of who deleted what and when
- No changes needed to existing queries

### `auditable` - Auditable Pattern *(Coming Soon)*

Automatically tracks creation and modification timestamps and users.

```bash
swap g pattern auditable Product
```

Will add:
- `CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy` properties
- EF Core interceptor for automatic timestamp/user population

### `sluggable` - Sluggable Pattern *(Coming Soon)*

Generates SEO-friendly URL slugs with collision handling.

```bash
swap g pattern sluggable BlogPost
```

Will add:
- `Slug` property with unique constraint
- Automatic slug generation from title/name
- Collision detection and resolution (e.g., `my-post`, `my-post-2`)

## Command Options

### Arguments

| Argument | Description | Required |
|----------|-------------|----------|
| `type` | Pattern type: `softdelete`, `auditable`, or `sluggable` | Yes |
| `entity` | Entity name (e.g., `Post`, `Product`) | Yes |

### Options

| Option | Alias | Description | Default |
|--------|-------|-------------|---------|
| `--force` | `-f` | Overwrite without prompting | `false` |
| `--project` | `-p` | Path to project directory | Current directory |

## Examples

### Basic Usage

```bash
# Add soft delete to Post entity
swap g pattern softdelete Post

# Use aliases for speed
swap g p soft Product
swap g p soft Order

# Remove a pattern
swap g p remove Post softdelete
```

### Pattern Removal Workflow

```bash
# 1. Remove the pattern from your entity
swap g p remove Article softdelete

# 2. Review the changes
# - Check Models/Article.cs (interface and properties removed)
# - Check Data/AppDbContext.cs (filter removed if no other entities use it)
# - Check swap-config.json (pattern entry removed)

# 3. Build to ensure no compilation errors
dotnet build

# 4. (Optional) Drop the database columns
dotnet ef migrations add RemoveSoftDeleteFromArticle
# Edit migration to drop IsDeleted, DeletedAt, DeletedBy columns
dotnet ef database update
```

### With Options

```bash
# Overwrite without prompting
swap g pattern softdelete Post --force

# Specify project directory
swap g pattern softdelete User --project path/to/MyProject
```

### Complete Workflow

```bash
# 1. Generate model
swap g m Article --fields "Title:string Content:string PublishedAt:DateTime"

# 2. Apply soft delete pattern
swap g p softdelete Article

# 3. Configure DbContext (manual step)
# Add modelBuilder.ConfigureSoftDeleteFilter() to OnModelCreating

# 4. Generate migration
dotnet ef migrations add AddArticleWithSoftDelete

# 5. Update database
dotnet ef database update

# 6. Use in your code
# Articles are now soft-deletable!
```

## Integration with Swap Workflow

Patterns integrate seamlessly with Swap's code generation workflow:

```bash
# Generate model
swap g m Product --fields "Name:string Price:decimal Stock:int"

# Add patterns as needed
swap g p softdelete Product

# Generate controller
swap g c Product --fields "Name:string Price:decimal Stock:int"

# Generate tests
swap g test Product

# Generate factory for testing
swap g factory Product
```

## Pattern Library

All patterns are provided by the `Swap.Patterns` library, which is automatically referenced when you use pattern commands.

**Library features:**
- ✅ Minimal dependencies (only EF Core)
- ✅ Well-tested and production-proven
- ✅ Fluent extension methods
- ✅ No magic - simple, understandable code
- ✅ Opt-in by design

**Package reference:**
```xml
<PackageReference Include="Swap.Patterns" Version="0.1.0" />
```

## Troubleshooting

### "Model file not found"

The entity model must exist before applying patterns. Generate it first:

```bash
swap g m Product --fields "Name:string Price:decimal"
swap g p softdelete Product  # Now this works
```

### "Already implements ISoftDeletable"

The pattern has already been applied. Use `--force` to reapply or check your entity class.

### Package reference issues

If the CLI can't automatically add the package reference, add it manually:

```bash
dotnet add package Swap.Patterns
```

### Query filter not working

Make sure you've added the filter configuration to your `DbContext`:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.ConfigureSoftDeleteFilter();
}
```

## Related Commands

- [`swap generate model`](./generate-model.md) - Generate entity models
- [`swap generate controller`](./generate-controller.md) - Generate CRUD controllers
- [`swap generate test`](./generate-test.md) - Generate integration tests
- [`swap generate factory`](./generate-factory.md) - Generate test data factories

## See Also

- [Pattern Library Documentation](../features/patterns.md)
- [Soft Delete Pattern Guide](../features/patterns.md#soft-delete)
- [Testing Patterns](../features/testing-framework.md)
- [Swap.Patterns README](https://github.com/jdtoon/swap/tree/main/framework/Swap.Patterns)
