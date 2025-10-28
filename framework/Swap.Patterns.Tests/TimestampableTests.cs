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
}
