using Microsoft.EntityFrameworkCore;
using Swap.Patterns.Orderable;
using Xunit;

namespace Swap.Patterns.Tests;

public class OrderableExtensionsTests
{
    private class Item : IOrderable
    {
        public int Id { get; set; }
        public int Position { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private class TestDbContext : DbContext
    {
        public DbSet<Item> Items => Set<Item>();
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseInMemoryDatabase($"orderable-tests-{Guid.NewGuid()}");
        }
    }

    [Fact]
    public async Task GetNextPosition_Works()
    {
        using var db = new TestDbContext();
        db.Items.AddRange(new Item { Position = 1 }, new Item { Position = 2 });
        await db.SaveChangesAsync();

        var next = await db.Items.GetNextPositionAsync();
        Assert.Equal(3, next);
    }

    [Fact]
    public async Task Reorder_MovesItemAndShiftsOthers()
    {
        using var db = new TestDbContext();
        db.Items.AddRange(
            new Item { Id = 1, Position = 1, Name = "A" },
            new Item { Id = 2, Position = 2, Name = "B" },
            new Item { Id = 3, Position = 3, Name = "C" }
        );
        await db.SaveChangesAsync();

        var item = await db.Items.FindAsync(3);
        Assert.NotNull(item);
        await db.Items.ReorderAsync(item!, 1);
        await db.SaveChangesAsync();

        var ordered = await db.Items.OrderBy(e => e.Id).ToListAsync();
        Assert.Equal(1, ordered.First(i => i.Id == 3).Position);
        Assert.Equal(2, ordered.First(i => i.Id == 1).Position);
        Assert.Equal(3, ordered.First(i => i.Id == 2).Position);
    }

    [Fact]
    public async Task NormalizePositions_FixesGaps()
    {
        using var db = new TestDbContext();
        db.Items.AddRange(
            new Item { Position = 1 },
            new Item { Position = 5 },
            new Item { Position = 10 }
        );
        await db.SaveChangesAsync();

        await db.Items.NormalizePositionsAsync();
        await db.SaveChangesAsync();

        var positions = await db.Items.OrderBy(e => e.Position).Select(e => e.Position).ToListAsync();
        Assert.Equal(new[] { 1, 2, 3 }, positions);
    }

    [Fact]
    public async Task GetNextPosition_ReturnsOne_WhenEmpty()
    {
        using var db = new TestDbContext();
        var next = await db.Items.GetNextPositionAsync();
        Assert.Equal(1, next);
    }

    [Fact]
    public async Task Reorder_NoOp_WhenSamePosition()
    {
        using var db = new TestDbContext();
        db.Items.AddRange(
            new Item { Id = 1, Position = 1, Name = "A" },
            new Item { Id = 2, Position = 2, Name = "B" }
        );
        await db.SaveChangesAsync();

        var item = await db.Items.FindAsync(2);
        await db.Items.ReorderAsync(item!, 2);
        await db.SaveChangesAsync();

        var positions = await db.Items.OrderBy(e => e.Id).Select(e => e.Position).ToListAsync();
        Assert.Equal(new[] { 1, 2 }, positions);
    }

    [Fact]
    public async Task Reorder_MoveDown_ShiftsCorrectly()
    {
        using var db = new TestDbContext();
        db.Items.AddRange(
            new Item { Id = 1, Position = 1, Name = "A" },
            new Item { Id = 2, Position = 2, Name = "B" },
            new Item { Id = 3, Position = 3, Name = "C" }
        );
        await db.SaveChangesAsync();

        var item = await db.Items.FindAsync(1);
        await db.Items.ReorderAsync(item!, 3);
        await db.SaveChangesAsync();

        var ordered = await db.Items.OrderBy(e => e.Id).ToListAsync();
        Assert.Equal(3, ordered.First(i => i.Id == 1).Position);
        Assert.Equal(1, ordered.First(i => i.Id == 2).Position);
        Assert.Equal(2, ordered.First(i => i.Id == 3).Position);
    }

    [Fact]
    public async Task OrderByPosition_SortsCorrectly()
    {
        using var db = new TestDbContext();
        db.Items.AddRange(
            new Item { Position = 3, Name = "Third" },
            new Item { Position = 1, Name = "First" },
            new Item { Position = 2, Name = "Second" }
        );
        await db.SaveChangesAsync();

        var ordered = await db.Items.OrderByPosition().Select(i => i.Name).ToListAsync();
        Assert.Equal(new[] { "First", "Second", "Third" }, ordered);
    }

    [Fact]
    public async Task OrderByPositionDescending_SortsCorrectly()
    {
        using var db = new TestDbContext();
        db.Items.AddRange(
            new Item { Position = 1, Name = "First" },
            new Item { Position = 2, Name = "Second" },
            new Item { Position = 3, Name = "Third" }
        );
        await db.SaveChangesAsync();

        var ordered = await db.Items.OrderByPositionDescending().Select(i => i.Name).ToListAsync();
        Assert.Equal(new[] { "Third", "Second", "First" }, ordered);
    }
}
