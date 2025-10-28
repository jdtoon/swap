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
}
