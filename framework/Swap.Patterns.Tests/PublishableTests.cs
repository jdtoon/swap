using Microsoft.EntityFrameworkCore;
using Swap.Patterns.Publishable;
using Xunit;

namespace Swap.Patterns.Tests;

public class PublishableTests
{
    private class Item : IPublishable
    {
        public int Id { get; set; }
        public bool IsPublished { get; set; }
        public DateTime? PublishedAt { get; set; }
        public string Title { get; set; } = string.Empty;
    }

    private class TestDbContext : DbContext
    {
        public DbSet<Item> Items => Set<Item>();
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseInMemoryDatabase($"publishable-tests-{Guid.NewGuid()}");
        }
    }

    [Fact]
    public void Publish_SetsFlagsAndTimestamp()
    {
        var item = new Item { Title = "Draft" };
        Assert.False(item.IsPublished);
        Assert.Null(item.PublishedAt);

        item.Publish();

        Assert.True(item.IsPublished);
        Assert.NotNull(item.PublishedAt);
        Assert.True((DateTime.UtcNow - item.PublishedAt!.Value).TotalSeconds < 5);
    }

    [Fact]
    public void Unpublish_ClearsFlags()
    {
        var item = new Item { Title = "Published" };
        item.Publish(DateTime.UtcNow.AddMinutes(-5));
        Assert.True(item.IsPublished);
        Assert.NotNull(item.PublishedAt);

        item.Unpublish();
        Assert.False(item.IsPublished);
        Assert.Null(item.PublishedAt);
    }

    [Fact]
    public async Task QueryHelpers_FilterCorrectly()
    {
        using var db = new TestDbContext();
        var a = new Item { Title = "A" };
        var b = new Item { Title = "B" };
        a.Publish();
        db.Items.AddRange(a, b);
        await db.SaveChangesAsync();

        var published = await db.Items.Published().ToListAsync();
        var drafts = await db.Items.Drafts().ToListAsync();

        Assert.Single(published);
        Assert.Equal("A", published.Single().Title);
        Assert.Single(drafts);
        Assert.Equal("B", drafts.Single().Title);
    }

    [Fact]
    public void Publish_WithCustomTimestamp_UsesProvided()
    {
        var item = new Item { Title = "Scheduled" };
        var futureDate = DateTime.UtcNow.AddDays(7);

        item.Publish(futureDate);

        Assert.True(item.IsPublished);
        Assert.Equal(futureDate, item.PublishedAt);
    }

    [Fact]
    public async Task PublishedAfter_FiltersCorrectly()
    {
        using var db = new TestDbContext();
        var now = DateTime.UtcNow;

        var old = new Item { Title = "Old" };
        old.Publish(now.AddDays(-7));

        var recent = new Item { Title = "Recent" };
        recent.Publish(now.AddDays(-1));

        db.Items.AddRange(old, recent);
        await db.SaveChangesAsync();

        var recentItems = await db.Items.PublishedAfter(now.AddDays(-2)).ToListAsync();

        Assert.Single(recentItems);
        Assert.Equal("Recent", recentItems.Single().Title);
    }

    [Fact]
    public async Task PublishedBefore_FiltersCorrectly()
    {
        using var db = new TestDbContext();
        var now = DateTime.UtcNow;

        var old = new Item { Title = "Old" };
        old.Publish(now.AddDays(-7));

        var recent = new Item { Title = "Recent" };
        recent.Publish(now.AddDays(-1));

        db.Items.AddRange(old, recent);
        await db.SaveChangesAsync();

        var oldItems = await db.Items.PublishedBefore(now.AddDays(-2)).ToListAsync();

        Assert.Single(oldItems);
        Assert.Equal("Old", oldItems.Single().Title);
    }

    [Fact]
    public async Task Published_ExcludesDrafts()
    {
        using var db = new TestDbContext();
        var draft = new Item { Title = "Draft", IsPublished = false };
        var published = new Item { Title = "Published" };
        published.Publish();

        db.Items.AddRange(draft, published);
        await db.SaveChangesAsync();

        var publishedItems = await db.Items.Published().ToListAsync();

        Assert.Single(publishedItems);
        Assert.Equal("Published", publishedItems.Single().Title);
    }

    [Fact]
    public async Task Drafts_ExcludesPublished()
    {
        using var db = new TestDbContext();
        var draft1 = new Item { Title = "Draft1", IsPublished = false };
        var draft2 = new Item { Title = "Draft2", IsPublished = false };
        var published = new Item { Title = "Published" };
        published.Publish();

        db.Items.AddRange(draft1, draft2, published);
        await db.SaveChangesAsync();

        var draftItems = await db.Items.Drafts().ToListAsync();

        Assert.Equal(2, draftItems.Count);
        Assert.All(draftItems, d => Assert.False(d.IsPublished));
    }
}
