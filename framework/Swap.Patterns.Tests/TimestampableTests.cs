using Microsoft.EntityFrameworkCore;
using Swap.Patterns.Timestampable;
using Xunit;

namespace Swap.Patterns.Tests;

public class TimestampableTests
{
    private class TestEntity : ITimestampable
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    private class TestDbContext : DbContext
    {
        public DbSet<TestEntity> Items => Set<TestEntity>();
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder
                .UseInMemoryDatabase($"timestampable-tests-{Guid.NewGuid()}")
                .AddInterceptors(new TimestampInterceptor());
        }
    }

    [Fact]
    public void SetsTimestampsOnInsert()
    {
        using var db = new TestDbContext();
        var e = new TestEntity();
        db.Items.Add(e);
        db.SaveChanges();

        Assert.NotEqual(default, e.CreatedAt);
        Assert.NotEqual(default, e.UpdatedAt);
        Assert.True(e.UpdatedAt >= e.CreatedAt);
        Assert.True((DateTime.UtcNow - e.CreatedAt).TotalSeconds < 5);
    }

    [Fact]
    public void UpdatesUpdatedAtOnModify()
    {
        using var db = new TestDbContext();
        var e = new TestEntity();
        db.Items.Add(e);
        db.SaveChanges();
        var createdAt = e.CreatedAt;
        var updatedAt = e.UpdatedAt;

        // simulate delay
        System.Threading.Thread.Sleep(50);

        // modify
        db.Entry(e).State = EntityState.Modified;
        db.SaveChanges();

        Assert.Equal(createdAt, e.CreatedAt); // CreatedAt should remain unchanged
        Assert.True(e.UpdatedAt > updatedAt);
    }

    [Fact]
    public void MultipleUpdates_IncrementUpdatedAt()
    {
        using var db = new TestDbContext();
        var e = new TestEntity();
        db.Items.Add(e);
        db.SaveChanges();
        var createdAt = e.CreatedAt;

        var timestamps = new List<DateTime> { e.UpdatedAt };

        for (int i = 0; i < 3; i++)
        {
            System.Threading.Thread.Sleep(10);
            db.Entry(e).State = EntityState.Modified;
            db.SaveChanges();
            timestamps.Add(e.UpdatedAt);
        }

        // CreatedAt never changes
        Assert.Equal(createdAt, e.CreatedAt);

        // Each UpdatedAt is later than the previous
        for (int i = 1; i < timestamps.Count; i++)
        {
            Assert.True(timestamps[i] > timestamps[i - 1]);
        }
    }

    [Fact]
    public async Task BulkInsert_SetsAllTimestamps()
    {
        using var db = new TestDbContext();
        var items = new[]
        {
            new TestEntity(),
            new TestEntity(),
            new TestEntity()
        };

        db.Items.AddRange(items);
        await db.SaveChangesAsync();

        foreach (var item in items)
        {
            Assert.NotEqual(default, item.CreatedAt);
            Assert.NotEqual(default, item.UpdatedAt);
            Assert.True(item.UpdatedAt >= item.CreatedAt);
        }
    }

    [Fact]
    public void NoTimestampChange_WhenNoModification()
    {
        using var db = new TestDbContext();
        var e = new TestEntity();
        db.Items.Add(e);
        db.SaveChanges();
        var createdAt = e.CreatedAt;
        var updatedAt = e.UpdatedAt;

        // SaveChanges without any state change
        System.Threading.Thread.Sleep(50);
        db.SaveChanges();

        Assert.Equal(createdAt, e.CreatedAt);
        Assert.Equal(updatedAt, e.UpdatedAt);
    }
}
