using Microsoft.EntityFrameworkCore;
using Swap.Patterns.Versionable;
using Xunit;

namespace Swap.Patterns.Tests;

public class VersionableTests
{
    private class Item : IVersionable
    {
        public int Id { get; set; }
        public int Version { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private class TestDbContext : DbContext
    {
        public DbSet<Item> Items => Set<Item>();
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder
                .UseInMemoryDatabase($"versionable-tests-{Guid.NewGuid()}")
                .AddInterceptors(new VersionInterceptor());
        }
    }

    [Fact]
    public void InitializesVersionOnInsert()
    {
        using var db = new TestDbContext();
        var e = new Item { Name = "A" };
        db.Items.Add(e);
        db.SaveChanges();
        Assert.Equal(1, e.Version);
    }

    [Fact]
    public void IncrementsVersionOnUpdate()
    {
        using var db = new TestDbContext();
        var e = new Item { Name = "A" };
        db.Items.Add(e);
        db.SaveChanges();
        var v1 = e.Version;
        Assert.Equal(1, v1);

        e.Name = "B";
        db.SaveChanges();
        Assert.Equal(2, e.Version);

        e.Name = "C";
        db.SaveChanges();
        Assert.Equal(3, e.Version);
    }

    [Fact]
    public async Task QueryHelpers_Work()
    {
        using var db = new TestDbContext();
        var a = new Item { Name = "A" };
        var b = new Item { Name = "B" };
        db.Items.AddRange(a, b);
        await db.SaveChangesAsync();

        b.Name = "B2";
        await db.SaveChangesAsync(); // b now Version=2

        var min2 = await db.Items.WithMinVersion(2).ToListAsync();
        Assert.Single(min2);
        Assert.Equal("B2", min2.Single().Name);

        var v1 = await db.Items.WithVersion(1).OrderByVersion().ToListAsync();
        Assert.Single(v1);
        Assert.Equal("A", v1.Single().Name);
    }

    [Fact]
    public void MultipleUpdates_KeepsIncrementing()
    {
        using var db = new TestDbContext();
        var e = new Item { Name = "A" };
        db.Items.Add(e);
        db.SaveChanges();
        Assert.Equal(1, e.Version);

        for (int i = 2; i <= 10; i++)
        {
            e.Name = $"Update{i}";
            db.SaveChanges();
            Assert.Equal(i, e.Version);
        }
    }

    [Fact]
    public void PresetVersion_IsRespected()
    {
        using var db = new TestDbContext();
        var e = new Item { Name = "Imported", Version = 5 };
        db.Items.Add(e);
        db.SaveChanges();

        // Should keep the preset version on insert
        Assert.Equal(5, e.Version);

        // Should increment from there
        e.Name = "Updated";
        db.SaveChanges();
        Assert.Equal(6, e.Version);
    }

    [Fact]
    public async Task OrderByVersion_SortsCorrectly()
    {
        using var db = new TestDbContext();
        var items = new[]
        {
            new Item { Name = "A" },
            new Item { Name = "B" },
            new Item { Name = "C" }
        };
        db.Items.AddRange(items);
        await db.SaveChangesAsync();

        // Update B twice, C once
        items[1].Name = "B2";
        await db.SaveChangesAsync();
        items[1].Name = "B3";
        await db.SaveChangesAsync();

        items[2].Name = "C2";
        await db.SaveChangesAsync();

        var ordered = await db.Items.OrderByVersion().Select(i => i.Name).ToListAsync();
        Assert.Equal(new[] { "A", "C2", "B3" }, ordered);
    }

    [Fact]
    public async Task OrderByVersionDescending_SortsCorrectly()
    {
        using var db = new TestDbContext();
        var items = new[]
        {
            new Item { Name = "A" },
            new Item { Name = "B" },
            new Item { Name = "C" }
        };
        db.Items.AddRange(items);
        await db.SaveChangesAsync();

        items[0].Name = "A2";
        await db.SaveChangesAsync();
        items[0].Name = "A3";
        await db.SaveChangesAsync();

        var ordered = await db.Items.OrderByVersionDescending().Select(i => i.Name).ToListAsync();
        Assert.Equal(new[] { "A3", "B", "C" }, ordered);
    }

    [Fact]
    public void NoVersionChange_WhenNoModification()
    {
        using var db = new TestDbContext();
        var e = new Item { Name = "A" };
        db.Items.Add(e);
        db.SaveChanges();
        Assert.Equal(1, e.Version);

        // SaveChanges without state change
        db.SaveChanges();
        Assert.Equal(1, e.Version);
    }
}
