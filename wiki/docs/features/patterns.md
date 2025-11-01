# Entity Patterns

Common, battle-tested patterns for your entities that solve real production problems.

## Overview

Swap.Patterns provides opt-in entity patterns that handle common requirements like soft deletion, audit trails, timestamps, ordering, and SEO-friendly URLs. These patterns are extracted from production applications and designed for maximum developer experience.

**Key principles:**
- ✅ **Opt-in by design** - Only add patterns where you need them
- ✅ **CLI-driven** - Single command applies complete pattern
- ✅ **Production-proven** - Every pattern used in real applications
- ✅ **Minimal magic** - Simple, understandable code
- ✅ **Well-tested** - Comprehensive test coverage

## Embedded vs Package Mode

Swap supports two ways to apply patterns:

### Embedded Mode (Default)

```bash
swap g pattern sluggable Article
```

**How it works:**
- Copies pattern code directly into your entity
- No external package dependency
- Full control and visibility of pattern code

**When to use:**
- You want full control over pattern implementation
- You prefer seeing all code in your project
- You don't want external NuGet dependencies
- You plan to customize the pattern

### Package Mode

```bash
swap g pattern auditable Article --use-package
```

**How it works:**
- Implements interfaces from `Swap.Patterns` NuGet package (v0.1.0)
- Cleaner models (just interface implementation)
- Pattern logic in reusable package
- Requires `dotnet add package Swap.Patterns`

**When to use:**
- You want minimal code in your models
- You prefer reusable package patterns
- You trust the pattern implementations
- You want to leverage package updates

**Installation:**
```bash
dotnet add package Swap.Patterns
```

**Example comparison:**

Embedded mode (default):
```csharp
public class Article
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Slug { get; set; } = "";
}
```

Package mode (--use-package):
```csharp
using Swap.Patterns.Sluggable;

public class Article : ISluggable
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Slug { get; set; } = "";
}
```

Both generate the same migration and controller code. The difference is whether the pattern interface comes from the package or is embedded.

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

---

### Auditable

Automatically track creation and modification timestamps with user attribution. Never manually set `CreatedAt` or `UpdatedAt` again.

```bash
swap g pattern auditable Product
```

**When to use:**
- Compliance requirements for data tracking
- Debugging "who changed what when"
- Audit log generation
- Change history tracking
- Any entity where knowing creation/modification details matters

**Quick start:**

1. Apply pattern:
```bash
swap g p auditable Product
```

2. Add HTTP context accessor in `Program.cs`:
```csharp
builder.Services.AddHttpContextAccessor();
```

3. Configure audit interceptor in DbContext:
```csharp
private readonly IHttpContextAccessor _httpContextAccessor;

public AppDbContext(
    DbContextOptions<AppDbContext> options,
    IHttpContextAccessor httpContextAccessor) : base(options)
{
    _httpContextAccessor = httpContextAccessor;
}

protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
{
    optionsBuilder.AddInterceptors(_httpContextAccessor.CreateAuditInterceptor());
}
```

4. Use in code (automatic!):
```csharp
// Just save - timestamps set automatically
var product = new Product { Name = "Widget", Price = 19.99m };
db.Products.Add(product);
await db.SaveChangesAsync();
// product.CreatedAt and product.CreatedBy are now set

// Updates also tracked automatically
product.Price = 24.99m;
await db.SaveChangesAsync();
// product.UpdatedAt and product.UpdatedBy are now set
```

**What you get:**
- `IAuditable` interface with `CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy`
- EF Core `SaveChangesInterceptor` for automatic population
- User identification from Claims (NameIdentifier, Name, or Email)
- `CreatedAt`/`CreatedBy` set once on insert, protected from updates
- `UpdatedAt`/`UpdatedBy` set on every modification
- No manual timestamp management needed

[Learn more about Auditable →](#auditable)

---

### Sluggable

Generate SEO-friendly URL slugs with automatic collision handling. Perfect for content-heavy applications.

```bash
swap g pattern sluggable BlogPost
```

**When to use:**
- Blog posts, articles, products needing pretty URLs
- SEO optimization with readable URLs
- User-friendly URLs instead of numeric IDs
- Content management systems
- E-commerce product pages

**Quick start:**

1. Apply pattern:
```bash
swap g p sluggable Article
```

2. Configure unique index in DbContext:
```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.ConfigureSlugIndexes();
    
    // OR manually:
    modelBuilder.Entity<Article>()
        .HasIndex(e => e.Slug)
        .IsUnique();
}
```

3. Use in code:
```csharp
// Generate slug from title
var article = new Article { Title = "Hello World!" };
await article.GenerateSlugAsync(article.Title, db);
await db.SaveChangesAsync();
// article.Slug is now "hello-world"

// Handles collisions automatically
var duplicate = new Article { Title = "Hello World!" };
await duplicate.GenerateSlugAsync(duplicate.Title, db);
await db.SaveChangesAsync();
// duplicate.Slug is now "hello-world-2"

// Find by slug
var found = await db.Articles
    .FirstOrDefaultAsync(a => a.Slug == "hello-world");
```

**What you get:**
- `ISluggable` interface with `Slug` property
- `SlugGenerator` with text normalization and URL-safe conversion
- International character support (café → cafe, München → munchen)
- Automatic collision detection with counter suffixes
- Configurable max length (default: 80 characters)
- Unique constraint configuration helpers

**Features:**
- Converts "Hello World!" → "hello-world"
- Removes diacritics: "Café München" → "cafe-munchen"
- Removes special characters: "C# & .NET Guide (2024)" → "c-net-guide-2024"
- Handles collisions: "post" → "post-2" → "post-3"
- Respects max length while preserving whole words

[Learn more about Sluggable →](#sluggable)

---

### Timestampable

Automatically track creation and update timestamps without user attribution. Use this when you only need dates, not who performed the action.

```bash
swap g pattern timestampable Product
```

**When to use:**
- Entities where user attribution isn't needed
- Lightweight alternative to Auditable
- Background jobs, system-generated records

**Quick start:**

1. Apply pattern:
```bash
swap g p timestampable Product
```

2. Configure timestamp interceptor in DbContext:
```csharp
using Swap.Patterns.Timestampable;

protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
{
    optionsBuilder.AddInterceptors(new TimestampInterceptor());
}
```

3. Use in code (automatic!):
```csharp
var product = new Product { Name = "Widget" };
db.Products.Add(product);
await db.SaveChangesAsync();
// product.CreatedAt set automatically

product.Name = "Widget+";
await db.SaveChangesAsync();
// product.UpdatedAt updated automatically
```

**What you get:**
- `ITimestampable` with `CreatedAt`, `UpdatedAt`
- EF Core `SaveChangesInterceptor` for automatic population
- No dependency on `IHttpContextAccessor`

> Note: Do NOT combine Timestampable with Auditable on the same entity (both define `CreatedAt`/`UpdatedAt`). Choose one based on your needs.

---

### Orderable

Add a stable `Position` property and helpers for ordering lists, sortable tables, and drag-and-drop UIs.

```bash
swap g pattern orderable Category
```

**When to use:**
- Manually ordered lists (menus, categories, steps)
- Drag-and-drop reordering UIs
- Maintaining deterministic order without relying on creation dates

**Quick start:**

1. Apply pattern:
```bash
swap g p orderable Category
```

2. Use helpers in code:
```csharp
// Get next position for a new item
var next = await db.Categories.GetNextPositionAsync();
db.Categories.Add(new Category { Name = "New", Position = next });

// Move an item to a new position (1-based)
var item = await db.Categories.FindAsync(id);
await db.Categories.ReorderAsync(item!, 1);
await db.SaveChangesAsync();

// Normalize after deletes/bulk operations
await db.Categories.NormalizePositionsAsync();
await db.SaveChangesAsync();

// Order queries by position
var ordered = await db.Categories.OrderByPosition().ToListAsync();
```

**What you get:**
- `IOrderable` interface with `Position` property
- Extensions: `OrderByPosition()`, `OrderByPositionDescending()`, `GetNextPositionAsync()`, `ReorderAsync()`, `NormalizePositionsAsync()`

---

### Publishable

Add a simple draft/published workflow with a boolean flag and published timestamp.

```bash
swap g pattern publishable Article
```

**When to use:**
- Content that should be hidden until ready (blog posts, docs, products)
- Scheduled or manual publishing flows
- Lightweight alternative to complex workflow engines

**Quick start:**

1. Apply pattern:
```bash
swap g p publishable Article
```

2. Use helpers and queries:
```csharp
// Publish now (sets IsPublished and PublishedAt = UtcNow)
article.Publish();

// Unpublish (revert to draft)
article.Unpublish();

// Query helpers
var published = await db.Articles.Published().ToListAsync();
var drafts = await db.Articles.Drafts().ToListAsync();
```

**What you get:**
- `IPublishable` interface with `IsPublished`, `PublishedAt`
- Extensions: `Publish()`, `Unpublish()`, `Published()`, `Drafts()`, `PublishedAfter()`, `PublishedBefore()`

---

### Versionable

Track and increment a simple integer version on every update via an EF Core interceptor.

```bash
swap g pattern versionable Document
```

**When to use:**
- You need optimistic version counters without full change history
- Displaying revision numbers to users (v1, v2, v3)
- Lightweight alternative to snapshot/version-history systems

**Quick start:**

1. Apply pattern:
```bash
swap g p versionable Document
```

2. Configure version interceptor in DbContext:
```csharp
using Swap.Patterns.Versionable;

protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
{
    optionsBuilder.AddInterceptors(new VersionInterceptor());
}
```

3. Use in code:
```csharp
var doc = new Document { Title = "Draft" };
db.Documents.Add(doc);
await db.SaveChangesAsync();
// doc.Version == 1

doc.Title = "Draft (edited)";
await db.SaveChangesAsync();
// doc.Version == 2
```

**What you get:**
- `IVersionable` interface with `Version` property
- `VersionInterceptor` to initialize and increment the version
- Query helpers: `.WithMinVersion(n)`, `.WithVersion(n)`, `.OrderByVersion()`

---

### Visibility

Control visibility of entities with optional time-based scheduling. Perfect for feature flags, scheduled content releases, or time-bound campaigns.

```bash
swap g pattern visibility Banner
```

**When to use:**
- Feature flags (enable/disable features without deployments)
- Scheduled content releases (blog posts, announcements)
- Time-bound offers, promotions, or events
- A/B testing toggles
- Preview/staging content that should become visible later

**Quick start:**

1. Apply pattern:
```bash
swap g p visibility Banner
```

2. Use in code:
```csharp
// Manual toggle
banner.Show();
banner.Hide();

// Schedule for future (UTC)
banner.ScheduleVisibility(DateTime.UtcNow.AddDays(7));

// Schedule within a window
banner.ScheduleVisibilityWindow(
    DateTime.UtcNow.AddDays(7),
    DateTime.UtcNow.AddDays(14)
);

// Check if currently visible (respects time window)
if (banner.IsCurrentlyVisible())
{
    // render banner
}

// Query visible items (checks IsVisible + time window)
var visible = await _db.Banners.Visible().ToListAsync();

// Query scheduled (future start date)
var scheduled = await _db.Banners.Scheduled().ToListAsync();

// Query expired (past end date)
var expired = await _db.Banners.Expired().ToListAsync();
```

**What you get:**
- `IVisibility` interface with `IsVisible`, `VisibleFrom`, `VisibleUntil`
- Extensions: `Show()`, `Hide()`, `ShowNow()`, `ScheduleVisibility()`, `ScheduleVisibilityWindow()`, `IsCurrentlyVisible()`
- Query helpers: `.Visible()`, `.Hidden()`, `.Scheduled()`, `.Expired()`

**Migration:**
```bash
dotnet ef migrations add AddVisibilityToBanner
dotnet ef database update
```

---

### Combining Patterns

You can mix and match compatible patterns. Do not combine Auditable and Timestampable together (both define `CreatedAt`/`UpdatedAt`).

```csharp
// Example with Auditable + Orderable + Sluggable
public class Article : ISoftDeletable, IAuditable, ISluggable, IOrderable
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Content { get; set; }
    
    // Soft Delete (3 properties)
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
    
    // Auditable (4 properties)
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    
    // Sluggable (1 property)
    public string Slug { get; set; } = "";
    
    // Orderable (1 property)
    public int Position { get; set; }
}
```

Apply patterns in sequence:
```bash
swap g pattern softdelete Article
swap g pattern auditable Article  
swap g pattern sluggable Article
swap g pattern orderable Article
```

Complete usage example:
```csharp
// Create article - auditable sets timestamps automatically
var article = new Article 
{ 
    Title = "Getting Started with ASP.NET Core",
    Content = "..."
};

// Generate SEO-friendly slug
await article.GenerateSlugAsync(article.Title, db);

// Save - CreatedAt and CreatedBy set automatically
db.Articles.Add(article);
await db.SaveChangesAsync();
// article.Slug: "getting-started-with-asp-net-core"
// article.CreatedAt: 2024-10-28 14:30:00
// article.CreatedBy: "user@example.com"

// Update - UpdatedAt and UpdatedBy set automatically
article.Title = "Updated Title";
await db.SaveChangesAsync();
// article.UpdatedAt: 2024-10-28 15:45:00
// article.UpdatedBy: "user@example.com"

// Soft delete - preserve audit trail
article.SoftDelete("admin@example.com");
await db.SaveChangesAsync();
// article.IsDeleted: true
// article.DeletedAt: 2024-10-28 16:00:00
// article.DeletedBy: "admin@example.com"

// Normal queries exclude deleted
var active = await db.Articles.ToListAsync();

// Can still access for audit/recovery
var deleted = await db.Articles
    .IncludeDeleted()
    .Where(a => a.IsDeleted)
    .ToListAsync();
```

---

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
dotnet add package Swap.Patterns
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

## Removing Patterns

Swap provides a safe way to remove patterns from entities when they're no longer needed.

### Basic Removal

```bash
swap g pattern remove <entity> <pattern-type>

# Examples
swap g p remove Post softdelete
swap g p remove Product auditable
swap g p remove Article sluggable
```

### What Happens During Removal

1. **Entity cleanup:**
   - Removes pattern interface (e.g., `ISoftDeletable`)
   - Removes pattern properties from the model
   - Updates `using` statements

2. **Configuration tracking:**
   - Updates `swap-config.json` to remove pattern entry
   - Checks if other entities still use the pattern

3. **Smart wiring cleanup:**
   - **Only removes shared wiring if no other entities use it**
   - Soft Delete: Removes `ConfigureSoftDeleteFilter()` call
   - Auditable: Removes audit interceptor and `IHttpContextAccessor`
   - Timestampable: Removes timestamp interceptor
   - Sluggable: Removes the entity's unique slug index

### Database Column Handling

**Important:** Pattern removal is **non-destructive by default** - database columns are NOT automatically dropped.

**Why?**
- Preserves existing data
- Allows time for data migration or archival
- Prevents accidental data loss
- Gives you control over timing

**To drop columns after removal:**

```bash
# 1. Remove the pattern
swap g p remove Article softdelete

# 2. Create a migration
dotnet ef migrations add RemoveSoftDeleteFromArticle

# 3. Edit the generated migration to drop columns
# Example for soft delete:
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.DropColumn(name: "IsDeleted", table: "Articles");
    migrationBuilder.DropColumn(name: "DeletedAt", table: "Articles");
    migrationBuilder.DropColumn(name: "DeletedBy", table: "Articles");
}

# 4. Apply the migration
dotnet ef database update
```

### Safe Removal Workflow

```bash
# 1. Review which entities use the pattern
#    Check swap-config.json

# 2. Remove the pattern
swap g p remove Article auditable

# 3. Review the changes
#    - Models/Article.cs (interface/properties removed)
#    - Data/AppDbContext.cs (wiring removed only if safe)
#    - swap-config.json (pattern entry removed)

# 4. Build and test
dotnet build
dotnet test

# 5. (Optional) Drop database columns
#    Create and edit migration as shown above
```

### Validation and Safety

Swap performs several safety checks during removal:

- ✅ Verifies the entity exists
- ✅ Checks that the pattern was actually applied
- ✅ Updates `swap-config.json` for tracking
- ✅ Only removes shared wiring (filters, interceptors) when **no other entities** use that pattern
- ✅ Provides clear output showing what was changed

**Example output:**
```
Removing softdelete pattern from Post...
✓ Pattern interface removed from Models/Post.cs
✓ Pattern properties removed from Models/Post.cs
✓ Updated swap-config.json
✓ Removed ConfigureSoftDeleteFilter from DbContext (no other entities use SoftDelete)

Pattern removed successfully!

Next steps:
  1. Build project: dotnet build
  2. (Optional) Create migration to drop columns:
     dotnet ef migrations add RemoveSoftDeleteFromPost
```

### When to Remove Patterns

**Good reasons to remove:**
- Pattern functionality is no longer needed
- Simplifying entity structure during refactoring
- Pattern was applied incorrectly or to wrong entity
- Switching to a different approach

**Consider keeping:**
- Historical data depends on pattern columns
- Other parts of the system still reference pattern properties
- Compliance or audit requirements
- Data recovery scenarios

### Common Scenarios

**Scenario 1: Wrong pattern applied**
```bash
# Oops, meant to apply to Product not Post
swap g p remove Post auditable
swap g p auditable Product
```

**Scenario 2: Simplifying a model**
```bash
# Removing unused patterns to reduce complexity
swap g p remove Article timestampable
swap g p remove Article auditable
# Keep only the patterns you actually use
```

**Scenario 3: Switching approaches**
```bash
# Moving from soft delete to hard delete
swap g p remove Order softdelete
# Migrate/archive soft-deleted records first!
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
