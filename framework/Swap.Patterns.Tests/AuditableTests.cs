using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Swap.Patterns.Auditable;
using System.Security.Claims;
using Xunit;

namespace Swap.Patterns.Tests;

public class AuditableTests
{
    private class Item : IAuditable
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
    }

    private class TestDbContext : DbContext
    {
        private readonly IHttpContextAccessor? _httpContextAccessor;

        public DbSet<Item> Items => Set<Item>();

        public TestDbContext(DbContextOptions<TestDbContext> options, IHttpContextAccessor? httpContextAccessor = null)
            : base(options)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (_httpContextAccessor != null)
            {
                optionsBuilder.AddInterceptors(_httpContextAccessor.CreateAuditInterceptor());
            }
        }
    }

    private static TestDbContext CreateContext(string? userId = null, string dbName = "")
    {
        var dbNameToUse = string.IsNullOrEmpty(dbName) ? $"auditable-tests-{Guid.NewGuid()}" : dbName;
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(dbNameToUse)
            .Options;

        IHttpContextAccessor? accessor = null;
        if (userId != null)
        {
            var httpContext = new DefaultHttpContext();
            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId)
            }, "TestAuth"); // Must specify authenticationType to make IsAuthenticated = true

            httpContext.User = new ClaimsPrincipal(identity);
            accessor = new HttpContextAccessor { HttpContext = httpContext };
        }

        return new TestDbContext(options, accessor);
    }

    [Fact]
    public void SetsAuditFieldsOnInsert_WithUser()
    {
        using var db = CreateContext("user123");
        var item = new Item { Name = "Test" };
        db.Items.Add(item);
        db.SaveChanges();

        Assert.NotEqual(default, item.CreatedAt);
        Assert.Equal("user123", item.CreatedBy);
        Assert.Null(item.UpdatedAt);
        Assert.Null(item.UpdatedBy);
        Assert.True((DateTime.UtcNow - item.CreatedAt).TotalSeconds < 5);
    }

    [Fact]
    public void SetsAuditFieldsOnInsert_WithoutUser()
    {
        using var db = CreateContext();
        var item = new Item { Name = "Test" };
        db.Items.Add(item);
        db.SaveChanges();

        // Without interceptor, timestamps won't be set
        Assert.Equal(default, item.CreatedAt);
        Assert.Null(item.CreatedBy);
        Assert.Null(item.UpdatedAt);
        Assert.Null(item.UpdatedBy);
    }

    [Fact]
    public void UpdatesAuditFieldsOnModify()
    {
        var dbName = $"auditable-tests-{Guid.NewGuid()}";
        using var db1 = CreateContext("user123", dbName);
        var item = new Item { Name = "Test" };
        db1.Items.Add(item);
        db1.SaveChanges();

        var createdAt = item.CreatedAt;
        var createdBy = item.CreatedBy;
        var itemId = item.Id;

        System.Threading.Thread.Sleep(50);

        // Modify as different user in new context with same db
        using var db2 = CreateContext("user456", dbName);
        var existingItem = db2.Items.Find(itemId);
        existingItem!.Name = "Updated";
        db2.SaveChanges();

        Assert.Equal(createdAt, existingItem.CreatedAt); // CreatedAt unchanged
        Assert.Equal(createdBy, existingItem.CreatedBy); // CreatedBy unchanged
        Assert.NotNull(existingItem.UpdatedAt);
        Assert.True(existingItem.UpdatedAt > createdAt);
        Assert.Equal("user456", existingItem.UpdatedBy);
    }

    [Fact]
    public void MultipleUpdates_TracksLatest()
    {
        var dbName = $"auditable-tests-{Guid.NewGuid()}";
        using var db1 = CreateContext("user1", dbName);
        var item = new Item { Name = "Test" };
        db1.Items.Add(item);
        db1.SaveChanges();

        Assert.Equal("user1", item.CreatedBy);
        Assert.Null(item.UpdatedBy);
        var itemId = item.Id;

        var updates = new[] { "user2", "user3", "user4" };
        foreach (var userId in updates)
        {
            System.Threading.Thread.Sleep(10);

            using var db = CreateContext(userId, dbName);
            var existingItem = db.Items.Find(itemId);
            existingItem!.Name = $"Updated by {userId}";
            db.SaveChanges();

            Assert.Equal(userId, existingItem.UpdatedBy);
        }

        // Verify final state
        using var dbFinal = CreateContext("reader", dbName);
        var finalItem = dbFinal.Items.Find(itemId);
        Assert.Equal("user1", finalItem!.CreatedBy); // Never changes
        Assert.Equal("user4", finalItem.UpdatedBy); // Latest updater
    }

    [Fact]
    public async Task BulkInsert_SetsAllAuditFields()
    {
        using var db = CreateContext("admin");
        var items = new[]
        {
            new Item { Name = "Item1" },
            new Item { Name = "Item2" },
            new Item { Name = "Item3" }
        };

        db.Items.AddRange(items);
        await db.SaveChangesAsync();

        foreach (var item in items)
        {
            Assert.NotEqual(default, item.CreatedAt);
            Assert.Equal("admin", item.CreatedBy);
            Assert.Null(item.UpdatedAt);
            Assert.Null(item.UpdatedBy);
        }
    }

    [Fact]
    public void NoAuditChange_WhenNoModification()
    {
        using var db = CreateContext("user123");
        var item = new Item { Name = "Test" };
        db.Items.Add(item);
        db.SaveChanges();

        var createdAt = item.CreatedAt;
        var updatedAt = item.UpdatedAt;

        System.Threading.Thread.Sleep(50);
        db.SaveChanges();

        Assert.Equal(createdAt, item.CreatedAt);
        Assert.Equal(updatedAt, item.UpdatedAt);
    }

    [Fact]
    public void UsesNameClaim_WhenNameIdentifierMissing()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase($"auditable-tests-{Guid.NewGuid()}")
            .Options;

        var httpContext = new DefaultHttpContext();
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, "John Doe")
        }, "TestAuth");
        httpContext.User = new ClaimsPrincipal(identity);

        var accessor = new HttpContextAccessor { HttpContext = httpContext };
        using var db = new TestDbContext(options, accessor);

        var item = new Item { Name = "Test" };
        db.Items.Add(item);
        db.SaveChanges();

        Assert.Equal("John Doe", item.CreatedBy);
    }

    [Fact]
    public void UsesEmailClaim_WhenOthersMissing()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase($"auditable-tests-{Guid.NewGuid()}")
            .Options;

        var httpContext = new DefaultHttpContext();
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Email, "user@test.com")
        }, "TestAuth");
        httpContext.User = new ClaimsPrincipal(identity);

        var accessor = new HttpContextAccessor { HttpContext = httpContext };
        using var db = new TestDbContext(options, accessor);

        var item = new Item { Name = "Test" };
        db.Items.Add(item);
        db.SaveChanges();

        Assert.Equal("user@test.com", item.CreatedBy);
    }
}

