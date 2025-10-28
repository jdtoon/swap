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
}
