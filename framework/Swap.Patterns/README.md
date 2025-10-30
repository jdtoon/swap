# Swap.Patterns

Common patterns and utilities for ASP.NET Core applications using the Swap framework.

## Features

### 🗑️ Soft Delete Pattern

Implement soft deletion for your entities - mark records as deleted instead of physically removing them from the database.

**Key Features:**
- Simple `ISoftDeletable` interface
- Automatic query filtering to exclude deleted entities
- Restore capability
- Optional tracking of who/when deleted
- Extension methods for DbContext and entities

**Quick Start:**

```csharp
// Using Swap CLI (Recommended - auto-wires everything)
swap g pattern softdelete Post

// The CLI automatically:
// 1. Adds ISoftDeletable interface to your Post model
// 2. Adds the three required properties
// 3. Configures the global query filter in DbContext
// 4. Tracks the pattern in swap-config.json
// 5. Creates a database migration

// --- OR Manual Setup ---

// 1. Implement the interface on your entity
public class Post : ISoftDeletable
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    
    // Soft delete properties
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}

// 2. Configure in your DbContext (CLI does this automatically)
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.ConfigureSoftDeleteFilter();
    base.OnModelCreating(modelBuilder);
}

// 3. Use in your code
// Soft delete
var post = await _context.Posts.FindAsync(id);
post.SoftDelete("admin@example.com");
await _context.SaveChangesAsync();

// Restore
post.Restore();
await _context.SaveChangesAsync();

// Query only deleted
var deletedPosts = await _context.Posts
    .OnlyDeleted()
    .ToListAsync();

// Query including deleted
var allPosts = await _context.Posts
    .IncludeDeleted()
    .ToListAsync();
```

**CLI Generator:**

```bash
# Add soft delete to an existing entity (auto-wires everything)
swap generate pattern softdelete Post

# Remove pattern when no longer needed
swap g pattern remove Post softdelete

# Short alias
swap g pattern soft Post
```

**What the CLI does automatically:**
- ✅ Adds `ISoftDeletable` interface to your entity
- ✅ Adds the three required properties (`IsDeleted`, `DeletedAt`, `DeletedBy`)
- ✅ Configures `ConfigureSoftDeleteFilter()` in your DbContext
- ✅ Tracks pattern usage in `swap-config.json`
- ✅ Generates and applies database migration
- ✅ Smart cleanup on removal (only removes filter when no entities use it)

## Installation

```bash
dotnet add package Swap.Patterns
```

## API Reference

### ISoftDeletable Interface

```csharp
public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
    DateTime? DeletedAt { get; set; }
    string? DeletedBy { get; set; }
}
```

### Extension Methods

#### Entity Extensions

```csharp
// Soft delete an entity
entity.SoftDelete(deletedBy: "username");

// Restore a soft-deleted entity
entity.Restore();
```

#### Query Extensions

```csharp
// Include soft-deleted entities in query
var all = await context.Posts.IncludeDeleted().ToListAsync();

// Query only soft-deleted entities
var deleted = await context.Posts.OnlyDeleted().ToListAsync();
```

#### DbContext Configuration

```csharp
// In OnModelCreating
modelBuilder.ConfigureSoftDeleteFilter();
```

## Best Practices

1. **Apply the query filter globally** in `OnModelCreating` to ensure soft-deleted entities are excluded by default
2. **Use `IncludeDeleted()` sparingly** - only when you explicitly need to see deleted records
3. **Consider retention policies** - permanently delete old soft-deleted records after a certain period
4. **Track who deletes** - populate `DeletedBy` for audit trails
5. **Cascade considerations** - decide how soft delete affects related entities

## Examples

### Controller with Soft Delete

```csharp
[HttpDelete("{id}")]
public async Task<IActionResult> Delete(int id)
{
    var post = await _context.Posts.FindAsync(id);
    if (post == null) return NotFound();

    // Soft delete instead of Remove
    post.SoftDelete(User.Identity?.Name);
    await _context.SaveChangesAsync();

    return Ok();
}

[HttpPost("{id}/restore")]
public async Task<IActionResult> Restore(int id)
{
    // Query including deleted to find the entity
    var post = await _context.Posts
        .IncludeDeleted()
        .FirstOrDefaultAsync(p => p.Id == id);
    
    if (post == null) return NotFound();
    if (!post.IsDeleted) return BadRequest("Post is not deleted");

    post.Restore();
    await _context.SaveChangesAsync();

    return Ok();
}
```

### Admin View - Showing Deleted Items

```csharp
public async Task<IActionResult> DeletedPosts()
{
    var deleted = await _context.Posts
        .OnlyDeleted()
        .OrderByDescending(p => p.DeletedAt)
        .ToListAsync();
    
    return View(deleted);
}
```

## Philosophy

Swap.Patterns provides proven, production-ready patterns extracted from real applications. Each pattern:

- ✅ Follows .NET conventions
- ✅ Integrates seamlessly with Entity Framework Core
- ✅ Includes comprehensive documentation
- ✅ Has CLI generator support
- ✅ Works with the Swap ecosystem

## Coming Soon

- 📝 **Audit Trails** - Automatic tracking of created/updated timestamps and users
- 🔗 **Slug Generation** - SEO-friendly URLs with automatic collision handling
- 🔐 **Multi-tenancy** - Tenant isolation patterns
- 📦 **Soft Delete + Audit** - Combined pattern helpers

## License

MIT - see [LICENSE](../../LICENSE) for details.
