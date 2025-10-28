using Microsoft.EntityFrameworkCore;
using Swap.Patterns.Visibility;
using Xunit;

namespace Swap.Patterns.Tests;

public class VisibilityTests
{
    private class Item : IVisibility
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsVisible { get; set; }
        public DateTime? VisibleFrom { get; set; }
        public DateTime? VisibleUntil { get; set; }
    }

    private class TestDbContext : DbContext
    {
        public DbSet<Item> Items => Set<Item>();
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseInMemoryDatabase($"visibility-tests-{Guid.NewGuid()}");
        }
    }

    [Fact]
    public void ShowAndHide_TogglesFlag()
    {
        var item = new Item { Name = "Test" };
        Assert.False(item.IsVisible);

        item.Show();
        Assert.True(item.IsVisible);

        item.Hide();
        Assert.False(item.IsVisible);
    }

    [Fact]
    public void ShowNow_ClearsSchedule()
    {
        var item = new Item
        {
            IsVisible = false,
            VisibleFrom = DateTime.UtcNow.AddDays(1),
            VisibleUntil = DateTime.UtcNow.AddDays(2)
        };

        item.ShowNow();

        Assert.True(item.IsVisible);
        Assert.Null(item.VisibleFrom);
        Assert.Null(item.VisibleUntil);
    }

    [Fact]
    public void ScheduleVisibility_SetsFutureStart()
    {
        var item = new Item();
        var future = DateTime.UtcNow.AddHours(1);

        item.ScheduleVisibility(future);

        Assert.True(item.IsVisible);
        Assert.Equal(future, item.VisibleFrom);
        Assert.Null(item.VisibleUntil);
    }

    [Fact]
    public void ScheduleVisibilityWindow_SetsRange()
    {
        var item = new Item();
        var start = DateTime.UtcNow.AddHours(1);
        var end = DateTime.UtcNow.AddHours(5);

        item.ScheduleVisibilityWindow(start, end);

        Assert.True(item.IsVisible);
        Assert.Equal(start, item.VisibleFrom);
        Assert.Equal(end, item.VisibleUntil);
    }

    [Fact]
    public void IsCurrentlyVisible_RespectsTimeWindow()
    {
        var now = DateTime.UtcNow;

        // Visible now
        var visible = new Item { IsVisible = true };
        Assert.True(visible.IsCurrentlyVisible());

        // Hidden by flag
        var hidden = new Item { IsVisible = false };
        Assert.False(hidden.IsCurrentlyVisible());

        // Not yet visible (future start)
        var future = new Item
        {
            IsVisible = true,
            VisibleFrom = now.AddHours(1)
        };
        Assert.False(future.IsCurrentlyVisible());

        // Expired (past end)
        var expired = new Item
        {
            IsVisible = true,
            VisibleUntil = now.AddHours(-1)
        };
        Assert.False(expired.IsCurrentlyVisible());

        // Within window
        var active = new Item
        {
            IsVisible = true,
            VisibleFrom = now.AddHours(-1),
            VisibleUntil = now.AddHours(1)
        };
        Assert.True(active.IsCurrentlyVisible());
    }

    [Fact]
    public async Task QueryHelpers_FilterCorrectly()
    {
        using var db = new TestDbContext();
        var now = DateTime.UtcNow;

        var items = new[]
        {
            new Item { Name = "VisibleNow", IsVisible = true },
            new Item { Name = "Hidden", IsVisible = false },
            new Item { Name = "Scheduled", IsVisible = true, VisibleFrom = now.AddHours(1) },
            new Item { Name = "Expired", IsVisible = true, VisibleUntil = now.AddHours(-1) },
            new Item { Name = "Active", IsVisible = true, VisibleFrom = now.AddHours(-1), VisibleUntil = now.AddHours(1) }
        };

        db.Items.AddRange(items);
        await db.SaveChangesAsync();

        // Visible = flag on + in time window
        var visible = await db.Items.Visible().Select(i => i.Name).ToListAsync();
        Assert.Equal(2, visible.Count);
        Assert.Contains("VisibleNow", visible);
        Assert.Contains("Active", visible);

        // Hidden = flag off OR outside window
        var hidden = await db.Items.Hidden().Select(i => i.Name).ToListAsync();
        Assert.Equal(3, hidden.Count);

        // Scheduled = future start
        var scheduled = await db.Items.Scheduled().Select(i => i.Name).ToListAsync();
        Assert.Single(scheduled);
        Assert.Contains("Scheduled", scheduled);

        // Expired = past end
        var expired = await db.Items.Expired().Select(i => i.Name).ToListAsync();
        Assert.Single(expired);
        Assert.Contains("Expired", expired);
    }

    [Fact]
    public void IsCurrentlyVisible_OnlyFlag_NoTimestamp()
    {
        var visible = new Item { IsVisible = true };
        Assert.True(visible.IsCurrentlyVisible());

        var hidden = new Item { IsVisible = false };
        Assert.False(hidden.IsCurrentlyVisible());
    }

    [Fact]
    public void IsCurrentlyVisible_OnlyStart_NoEnd()
    {
        var now = DateTime.UtcNow;

        var pastStart = new Item
        {
            IsVisible = true,
            VisibleFrom = now.AddHours(-1)
        };
        Assert.True(pastStart.IsCurrentlyVisible());

        var futureStart = new Item
        {
            IsVisible = true,
            VisibleFrom = now.AddHours(1)
        };
        Assert.False(futureStart.IsCurrentlyVisible());
    }

    [Fact]
    public void IsCurrentlyVisible_OnlyEnd_NoStart()
    {
        var now = DateTime.UtcNow;

        var beforeEnd = new Item
        {
            IsVisible = true,
            VisibleUntil = now.AddHours(1)
        };
        Assert.True(beforeEnd.IsCurrentlyVisible());

        var afterEnd = new Item
        {
            IsVisible = true,
            VisibleUntil = now.AddHours(-1)
        };
        Assert.False(afterEnd.IsCurrentlyVisible());
    }

    [Fact]
    public async Task Visible_ExcludesHiddenByFlag()
    {
        using var db = new TestDbContext();
        var visible = new Item { Name = "Visible", IsVisible = true };
        var hidden = new Item { Name = "Hidden", IsVisible = false };

        db.Items.AddRange(visible, hidden);
        await db.SaveChangesAsync();

        var result = await db.Items.Visible().Select(i => i.Name).ToListAsync();

        Assert.Single(result);
        Assert.Equal("Visible", result.Single());
    }

    [Fact]
    public async Task Scheduled_OnlyReturnsFutureItems()
    {
        using var db = new TestDbContext();
        var now = DateTime.UtcNow;

        var items = new[]
        {
            new Item { Name = "Past", IsVisible = true, VisibleFrom = now.AddHours(-1) },
            new Item { Name = "Future1", IsVisible = true, VisibleFrom = now.AddHours(1) },
            new Item { Name = "Future2", IsVisible = true, VisibleFrom = now.AddHours(2) },
            new Item { Name = "NoSchedule", IsVisible = true }
        };

        db.Items.AddRange(items);
        await db.SaveChangesAsync();

        var scheduled = await db.Items.Scheduled().Select(i => i.Name).ToListAsync();

        Assert.Equal(2, scheduled.Count);
        Assert.Contains("Future1", scheduled);
        Assert.Contains("Future2", scheduled);
    }

    [Fact]
    public async Task Expired_OnlyReturnsExpired()
    {
        using var db = new TestDbContext();
        var now = DateTime.UtcNow;

        var items = new[]
        {
            new Item { Name = "Expired1", IsVisible = true, VisibleUntil = now.AddHours(-2) },
            new Item { Name = "Expired2", IsVisible = true, VisibleUntil = now.AddHours(-1) },
            new Item { Name = "Active", IsVisible = true, VisibleUntil = now.AddHours(1) },
            new Item { Name = "NoEnd", IsVisible = true }
        };

        db.Items.AddRange(items);
        await db.SaveChangesAsync();

        var expired = await db.Items.Expired().Select(i => i.Name).ToListAsync();

        Assert.Equal(2, expired.Count);
        Assert.Contains("Expired1", expired);
        Assert.Contains("Expired2", expired);
    }

    [Fact]
    public async Task Hidden_IncludesMultipleReasons()
    {
        using var db = new TestDbContext();
        var now = DateTime.UtcNow;

        var items = new[]
        {
            new Item { Name = "FlagOff", IsVisible = false },
            new Item { Name = "NotYet", IsVisible = true, VisibleFrom = now.AddHours(1) },
            new Item { Name = "Expired", IsVisible = true, VisibleUntil = now.AddHours(-1) },
            new Item { Name = "Visible", IsVisible = true }
        };

        db.Items.AddRange(items);
        await db.SaveChangesAsync();

        var hidden = await db.Items.Hidden().Select(i => i.Name).ToListAsync();

        Assert.Equal(3, hidden.Count);
        Assert.Contains("FlagOff", hidden);
        Assert.Contains("NotYet", hidden);
        Assert.Contains("Expired", hidden);
    }
}
