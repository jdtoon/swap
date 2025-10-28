# Entity Patterns

Common, battle-tested patterns for your entities that solve real production problems.

## Overview

Swap.Patterns provides opt-in entity patterns that handle common requirements like soft deletion, audit trails, and SEO-friendly URLs. These patterns are extracted from production applications and designed for maximum developer experience.

**Key principles:**
- ✅ **Opt-in by design** - Only add patterns where you need them
- ✅ **CLI-driven** - Single command applies complete pattern
- ✅ **Production-proven** - Every pattern used in real applications
- ✅ **Minimal magic** - Simple, understandable code
- ✅ **Well-tested** - Comprehensive test coverage

## Available Patterns

### Soft Delete

Hide deleted records instead of removing them from the database. Essential for audit trails, compliance, and data recovery.

```bash
swap g pattern softdelete Post
```

**When to use:**
- Regulatory compliance (GDPR, HIPAA, SOX)
- Audit trails for deleted records
- User-initiated deletions that may need undoing
- Cascading delete scenarios where relationships matter

**Quick start:**

1. Apply pattern:
```bash
swap g p softdelete Article
```

2. Configure DbContext:
```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.ConfigureSoftDeleteFilter();
}
```

3. Use in code:
```csharp
// Soft delete
article.SoftDelete("user@example.com");
await db.SaveChangesAsync();

// Restore
article.Restore();
await db.SaveChangesAsync();

// Normal queries exclude deleted automatically
var active = await db.Articles.ToListAsync();

// Query only deleted
var deleted = await db.Articles.OnlyDeleted().ToListAsync();

// Include deleted in results
var all = await db.Articles.IncludeDeleted().ToListAsync();
```

**What you get:**
- `ISoftDeletable` interface with `IsDeleted`, `DeletedAt`, `DeletedBy`
- Extension methods: `SoftDelete()`, `Restore()`
- Query extensions: `IncludeDeleted()`, `OnlyDeleted()`
- EF Core query filter configuration
- Automatic exclusion from queries

[Learn more about Soft Delete →](#soft-delete-pattern)

### Auditable *(Coming Soon)*

Automatically track creation and modification timestamps with user attribution.

```bash
swap g pattern auditable Product
```

**When to use:**
- Compliance requirements for data tracking
- Debugging "who changed what when"
- Audit log generation
- Change history tracking

**What you'll get:**
- `IAuditable` interface with `CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy`
- EF Core interceptor for automatic population
- No manual timestamp management needed

### Sluggable *(Coming Soon)*

Generate SEO-friendly URL slugs with automatic collision handling.

```bash
swap g pattern sluggable BlogPost
```

**When to use:**
- Blog posts, articles, products needing pretty URLs
- SEO optimization
- User-friendly URLs instead of IDs

**What you'll get:**
- `ISluggable` interface with `Slug` property
- Automatic slug generation from title/name
- Collision detection (e.g., `my-post`, `my-post-2`)
- Unique constraint configuration

## Soft Delete Pattern

### Overview

The soft delete pattern marks records as deleted instead of removing them from the database. This is essential for:

- **Audit Compliance** - GDPR, HIPAA, SOX require data retention
- **Data Recovery** - Undo accidental deletions
- **Referential Integrity** - Preserve relationships even after "deletion"
- **Analytics** - Analyze deletion patterns and trends

### Installation

The pattern is provided by the `Swap.Patterns` library, which is automatically referenced when you use the CLI command.

**Manual installation:**
```bash
dotnet add package Swap.Patterns --prerelease
```

### Usage

#### 1. Apply Pattern to Entity

```bash
swap g pattern softdelete Post
```

This modifies your entity:

```csharp
using Swap.Patterns.SoftDelete;

namespace MyApp.Models;

public class Post : ISoftDeletable
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Content { get; set; }
    
    // ISoftDeletable properties
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}
```

#### 2. Configure Query Filter

Add to your `DbContext` to automatically exclude deleted records:

```csharp
using Swap.Patterns.SoftDelete;

public class AppDbContext : DbContext
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Configure soft delete filter for all ISoftDeletable entities
        modelBuilder.ConfigureSoftDeleteFilter();
    }
}
```

#### 3. Generate and Apply Migration

```bash
dotnet ef migrations add AddSoftDeleteToPost
dotnet ef database update
```

#### 4. Use in Your Application

**Soft delete a record:**
```csharp
var post = await db.Posts.FindAsync(id);
post.SoftDelete(); // Anonymous deletion
await db.SaveChangesAsync();

// Or with user attribution
post.SoftDelete("user@example.com");
await db.SaveChangesAsync();
```

**Restore a deleted record:**
```csharp
var post = await db.Posts
    .IncludeDeleted()
    .FirstAsync(p => p.Id == id);
    
post.Restore();
await db.SaveChangesAsync();
```

**Query deleted records:**
```csharp
// Only deleted records
var deleted = await db.Posts.OnlyDeleted().ToListAsync();

// Include deleted in results
var all = await db.Posts.IncludeDeleted().ToListAsync();

// Normal queries automatically exclude deleted
var active = await db.Posts.ToListAsync();
```

### API Reference

#### ISoftDeletable Interface

```csharp
public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
    DateTime? DeletedAt { get; set; }
    string? DeletedBy { get; set; }
}
```

#### Extension Methods

**SoftDelete()**
```csharp
void SoftDelete(string? deletedBy = null)
```
Marks the entity as deleted. Sets `IsDeleted = true`, `DeletedAt = DateTime.UtcNow`, and optionally `DeletedBy`.

**Restore()**
```csharp
void Restore()
```
Restores a soft-deleted entity. Resets all soft delete properties to their default values.

#### Query Extensions

**IncludeDeleted()**
```csharp
IQueryable<T> IncludeDeleted<T>() where T : class, ISoftDeletable
```
Includes soft-deleted records in query results by ignoring the query filter.

**OnlyDeleted()**
```csharp
IQueryable<T> OnlyDeleted<T>() where T : class, ISoftDeletable
```
Returns only soft-deleted records.

#### DbContext Extensions

**ConfigureSoftDeleteFilter()**
```csharp
void ConfigureSoftDeleteFilter(this ModelBuilder modelBuilder)
```
Configures global query filter for all `ISoftDeletable` entities. Automatically excludes deleted records from queries.

### Advanced Scenarios

#### Cascade Soft Delete

When deleting a parent entity, soft delete all children:

```csharp
public async Task DeleteBlogPost(int postId)
{
    var post = await db.Posts
        .Include(p => p.Comments)
        .FirstAsync(p => p.Id == postId);
    
    // Soft delete post
    post.SoftDelete("admin@example.com");
    
    // Soft delete all comments
    foreach (var comment in post.Comments)
    {
        comment.SoftDelete("admin@example.com");
    }
    
    await db.SaveChangesAsync();
}
```

#### Permanent Delete

Sometimes you need to actually delete soft-deleted records:

```csharp
// Get deleted records older than 90 days
var cutoff = DateTime.UtcNow.AddDays(-90);
var oldDeleted = await db.Posts
    .IgnoreQueryFilters() // Include soft-deleted
    .Where(p => p.IsDeleted && p.DeletedAt < cutoff)
    .ToListAsync();

// Permanently remove from database
db.Posts.RemoveRange(oldDeleted);
await db.SaveChangesAsync();
```

#### Integration with Controllers

```csharp
[HttpPost("delete/{id}")]
public async Task<IActionResult> Delete(int id)
{
    var post = await _db.Posts.FindAsync(id);
    if (post == null) return NotFound();
    
    // Get current user from claims
    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    
    post.SoftDelete(userId);
    await _db.SaveChangesAsync();
    
    return Ok(new { message = "Post deleted successfully" });
}

[HttpPost("restore/{id}")]
public async Task<IActionResult> Restore(int id)
{
    var post = await _db.Posts
        .IncludeDeleted()
        .FirstOrDefaultAsync(p => p.Id == id);
        
    if (post == null) return NotFound();
    if (!post.IsDeleted) return BadRequest("Post is not deleted");
    
    post.Restore();
    await _db.SaveChangesAsync();
    
    return Ok(new { message = "Post restored successfully" });
}
```

### Testing

Use the Swap.Testing framework to test soft delete behavior:

```csharp
using Swap.Patterns.SoftDelete;
using Swap.Testing;

public class SoftDeleteTests : IClassFixture<HtmxTestFixture<Program>>
{
    private readonly HtmxTestFixture<Program> _fixture;
    
    public SoftDeleteTests(HtmxTestFixture<Program> fixture)
    {
        _fixture = fixture;
    }
    
    [Fact]
    public async Task SoftDelete_HidesFromDefaultQueries()
    {
        // Arrange
        using var scope = _fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        var post = new Post { Title = "Test", Content = "Content" };
        db.Posts.Add(post);
        await db.SaveChangesAsync();
        var postId = post.Id;
        
        // Act
        post.SoftDelete();
        await db.SaveChangesAsync();
        
        // Assert
        var found = await db.Posts.FirstOrDefaultAsync(p => p.Id == postId);
        Assert.Null(found); // Not found in default query
        
        var deleted = await db.Posts.IncludeDeleted().FirstAsync(p => p.Id == postId);
        Assert.True(deleted.IsDeleted);
    }
}
```

## Best Practices

### DO ✅

- **Apply selectively** - Only use patterns where you need them
- **Configure query filters** - Always add `ConfigureSoftDeleteFilter()` to DbContext
- **Track deleters** - Use `SoftDelete(userId)` for audit trails
- **Test soft delete behavior** - Verify queries exclude/include deleted correctly
- **Document deletion policies** - Let users know data can be recovered

### DON'T ❌

- **Don't soft delete everything** - Some data should be permanently deleted (sessions, temp data)
- **Don't forget migrations** - Pattern adds columns, you need a migration
- **Don't query deleted by default** - Use `IncludeDeleted()` explicitly when needed
- **Don't mix hard and soft deletes** - Pick one strategy per entity
- **Don't forget cleanup** - Consider purging old soft-deleted records

## Performance Considerations

- **Query filters are efficient** - EF Core applies them at the SQL level
- **Indexes recommended** - Add index on `IsDeleted` for large tables
- **Archive old deletions** - Consider moving very old deleted records to archive tables
- **Monitor deleted ratios** - High delete rates may indicate design issues

## FAQ

**Q: Can I customize the soft delete properties?**  
A: The interface defines `IsDeleted`, `DeletedAt`, `DeletedBy`. Implement the interface manually if you need different names.

**Q: Does soft delete work with EF Core cascade delete?**  
A: You need to manually soft delete child entities. EF cascade delete will hard delete unless you intercept it.

**Q: Can I use soft delete with owned entities?**  
A: Yes, but you'll need to manually propagate soft delete to owned entities.

**Q: How do I show deleted records in UI?**  
A: Use `IncludeDeleted()` in your controller query and add a filter in the view.

**Q: What about performance with millions of soft-deleted records?**  
A: Add an index on `IsDeleted` and consider archiving very old deletions to a separate table.

## Related

- [CLI: generate pattern](../cli/generate-pattern.md)
- [Testing Framework](./testing-framework.md)
- [Database Migrations](../cli/database.md)
- [Swap.Patterns GitHub](https://github.com/jdtoon/swap/tree/main/framework/Swap.Patterns)
