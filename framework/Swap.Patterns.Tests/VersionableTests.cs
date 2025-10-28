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
}
