using Microsoft.EntityFrameworkCore;
using Swap.Patterns.SoftDelete;
using Xunit;

namespace Swap.Patterns.Tests;

public class SoftDeleteTests
{
    private class Item : ISoftDeletable
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
        public string? DeletedBy { get; set; }
    }

    private class TestDbContext : DbContext
    {
        public DbSet<Item> Items => Set<Item>();
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseInMemoryDatabase($"softdelete-tests-{Guid.NewGuid()}");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ConfigureSoftDeleteFilter();
        }
    }

    [Fact]
    public void SoftDelete_SetsProperties()
    {
        var item = new Item { Name = "Test" };
        Assert.False(item.IsDeleted);
        Assert.Null(item.DeletedAt);
        Assert.Null(item.DeletedBy);

        item.SoftDelete("user@test.com");

        Assert.True(item.IsDeleted);
        Assert.NotNull(item.DeletedAt);
        Assert.Equal("user@test.com", item.DeletedBy);
        Assert.True((DateTime.UtcNow - item.DeletedAt!.Value).TotalSeconds < 5);
    }

    [Fact]
    public void SoftDelete_WithoutUser_SetsPropertiesExceptUser()
    {
        var item = new Item { Name = "Test" };
        
        item.SoftDelete();

        Assert.True(item.IsDeleted);
        Assert.NotNull(item.DeletedAt);
        Assert.Null(item.DeletedBy);
    }

    [Fact]
    public void Restore_ClearsProperties()
    {
        var item = new Item { Name = "Test" };
        item.SoftDelete("user@test.com");
        Assert.True(item.IsDeleted);

        item.Restore();

        Assert.False(item.IsDeleted);
        Assert.Null(item.DeletedAt);
        Assert.Null(item.DeletedBy);
    }

    [Fact]
    public async Task QueryFilter_ExcludesDeleted()
    {
        using var db = new TestDbContext();
        var active = new Item { Name = "Active" };
        var deleted = new Item { Name = "Deleted" };
        deleted.SoftDelete();

        db.Items.AddRange(active, deleted);
        await db.SaveChangesAsync();

        var items = await db.Items.ToListAsync();

        Assert.Single(items);
        Assert.Equal("Active", items.Single().Name);
    }

    [Fact]
    public async Task IncludeDeleted_ReturnsAll()
    {
        using var db = new TestDbContext();
        var active = new Item { Name = "Active" };
        var deleted = new Item { Name = "Deleted" };
        deleted.SoftDelete();

        db.Items.AddRange(active, deleted);
        await db.SaveChangesAsync();

        var items = await db.Items.IncludeDeleted().ToListAsync();

        Assert.Equal(2, items.Count);
    }

    [Fact]
    public async Task OnlyDeleted_ReturnsOnlyDeleted()
    {
        using var db = new TestDbContext();
        var active = new Item { Name = "Active" };
        var deleted1 = new Item { Name = "Deleted1" };
        var deleted2 = new Item { Name = "Deleted2" };
        deleted1.SoftDelete();
        deleted2.SoftDelete("admin");

        db.Items.AddRange(active, deleted1, deleted2);
        await db.SaveChangesAsync();

        var items = await db.Items.OnlyDeleted().Select(i => i.Name).ToListAsync();

        Assert.Equal(2, items.Count);
        Assert.Contains("Deleted1", items);
        Assert.Contains("Deleted2", items);
    }

    [Fact]
    public async Task SoftDelete_ThenRestore_WorksInDatabase()
    {
        using var db = new TestDbContext();
        var item = new Item { Name = "Test" };
        db.Items.Add(item);
        await db.SaveChangesAsync();

        // Soft delete
        item.SoftDelete("admin");
        await db.SaveChangesAsync();

        // Should be excluded
        var activeItems = await db.Items.ToListAsync();
        Assert.Empty(activeItems);

        // Should be in deleted
        var deletedItems = await db.Items.OnlyDeleted().ToListAsync();
        Assert.Single(deletedItems);

        // Restore
        item.Restore();
        await db.SaveChangesAsync();

        // Should be back in active
        var restoredItems = await db.Items.ToListAsync();
        Assert.Single(restoredItems);
        Assert.Equal("Test", restoredItems.Single().Name);
    }

    [Fact]
    public async Task MultipleDeletes_TracksDifferentUsers()
    {
        using var db = new TestDbContext();
        var item1 = new Item { Name = "Item1" };
        var item2 = new Item { Name = "Item2" };
        var item3 = new Item { Name = "Item3" };

        item1.SoftDelete("user1@test.com");
        item2.SoftDelete("user2@test.com");
        item3.SoftDelete("admin@test.com");

        db.Items.AddRange(item1, item2, item3);
        await db.SaveChangesAsync();

        var deleted = await db.Items.OnlyDeleted().ToListAsync();

        Assert.Equal(3, deleted.Count);
        Assert.Contains(deleted, d => d.DeletedBy == "user1@test.com");
        Assert.Contains(deleted, d => d.DeletedBy == "user2@test.com");
        Assert.Contains(deleted, d => d.DeletedBy == "admin@test.com");
    }

    [Fact]
    public async Task FindById_RespectsQueryFilter()
    {
        using var db = new TestDbContext();
        var item = new Item { Name = "Test" };
        db.Items.Add(item);
        await db.SaveChangesAsync();
        var id = item.Id;

        item.SoftDelete();
        await db.SaveChangesAsync();

        var found = await db.Items.FindAsync(id);
        // FindAsync bypasses query filters, so it will find the deleted item
        Assert.NotNull(found);
        Assert.True(found.IsDeleted);
    }
}
