using Microsoft.EntityFrameworkCore;
using Swap.Patterns.Sluggable;
using Xunit;

namespace Swap.Patterns.Tests;

public class SluggableTests
{
    private class Post : ISluggable
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
    }

    private class TestDbContext : DbContext
    {
        private readonly string _dbName;

        public DbSet<Post> Posts => Set<Post>();

        public TestDbContext(string? dbName = null)
        {
            _dbName = dbName ?? $"sluggable-tests-{Guid.NewGuid()}";
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseInMemoryDatabase(_dbName);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ConfigureSlugIndexes();
        }
    }

    [Fact]
    public async Task GenerateSlug_CreatesBasicSlug()
    {
        using var db = new TestDbContext();
        var post = new Post { Title = "Hello World" };
        
        await post.GenerateSlugAsync(post.Title, db);
        
        Assert.Equal("hello-world", post.Slug);
    }

    [Fact]
    public async Task GenerateSlug_HandlesSpecialCharacters()
    {
        using var db = new TestDbContext();
        var post = new Post { Title = "C# & .NET Guide (2024)!" };
        
        await post.GenerateSlugAsync(post.Title, db);
        
        Assert.Equal("c-net-guide-2024", post.Slug);
    }

    [Fact]
    public async Task GenerateSlug_HandlesUnicode()
    {
        using var db = new TestDbContext();
        var post = new Post { Title = "Café München" };
        
        await post.GenerateSlugAsync(post.Title, db);
        
        Assert.Equal("cafe-munchen", post.Slug);
    }

    [Fact]
    public async Task GenerateSlug_HandlesCollisions()
    {
        using var db = new TestDbContext();
        
        var post1 = new Post { Title = "Test Post" };
        await post1.GenerateSlugAsync(post1.Title, db);
        db.Posts.Add(post1);
        await db.SaveChangesAsync();
        Assert.Equal("test-post", post1.Slug);

        var post2 = new Post { Title = "Test Post" };
        await post2.GenerateSlugAsync(post2.Title, db);
        Assert.Equal("test-post-2", post2.Slug);
        db.Posts.Add(post2);
        await db.SaveChangesAsync();

        var post3 = new Post { Title = "Test Post" };
        await post3.GenerateSlugAsync(post3.Title, db);
        Assert.Equal("test-post-3", post3.Slug);
    }

    [Fact]
    public async Task GenerateSlug_WithMaxLength()
    {
        using var db = new TestDbContext();
        var longTitle = "This is a very long title that exceeds the maximum allowed length for a slug and should be truncated properly";
        var post = new Post { Title = longTitle };
        
        await post.GenerateSlugAsync(longTitle, db, maxLength: 50);
        
        Assert.True(post.Slug.Length <= 50);
        Assert.DoesNotContain(" ", post.Slug);
    }

    [Fact]
    public async Task GenerateSlug_TrimsWhitespace()
    {
        using var db = new TestDbContext();
        var post = new Post { Title = "  Hello   World  " };
        
        await post.GenerateSlugAsync(post.Title, db);
        
        Assert.Equal("hello-world", post.Slug);
    }

    [Fact]
    public async Task GenerateSlug_HandlesMultipleHyphens()
    {
        using var db = new TestDbContext();
        var post = new Post { Title = "Hello---World" };
        
        await post.GenerateSlugAsync(post.Title, db);
        
        Assert.Equal("hello-world", post.Slug);
    }

    [Fact]
    public async Task GenerateSlug_EmptyString_ReturnsEmpty()
    {
        using var db = new TestDbContext();
        var post = new Post { Title = "" };
        
        await post.GenerateSlugAsync("", db);
        Assert.Equal(string.Empty, post.Slug);
    }

    [Fact]
    public async Task GenerateSlug_OnlySpecialChars_ReturnsEmpty()
    {
        using var db = new TestDbContext();
        var post = new Post { Title = "!@#$%^&*()" };
        
        await post.GenerateSlugAsync(post.Title, db);
        Assert.Equal(string.Empty, post.Slug);
    }

    [Fact]
    public async Task GenerateSlug_UpdateExisting_MayCollide()
    {
        var dbName = $"sluggable-tests-{Guid.NewGuid()}";
        using var db = new TestDbContext(dbName);
        
        var post = new Post { Title = "Original Title" };
        await post.GenerateSlugAsync(post.Title, db);
        db.Posts.Add(post);
        await db.SaveChangesAsync();
        
        Assert.Equal("original-title", post.Slug);

        // Creating another post with same title gets -2
        using var db2 = new TestDbContext(dbName);
        var post2 = new Post { Title = "Original Title" };
        await post2.GenerateSlugAsync(post2.Title, db2);
        Assert.Equal("original-title-2", post2.Slug);
    }

    [Fact]
    public async Task GenerateSlug_Numbers()
    {
        using var db = new TestDbContext();
        var post = new Post { Title = "Top 10 Tips for 2024" };
        
        await post.GenerateSlugAsync(post.Title, db);
        
        Assert.Equal("top-10-tips-for-2024", post.Slug);
    }

    [Fact]
    public async Task GenerateSlug_LowerCase()
    {
        using var db = new TestDbContext();
        var post = new Post { Title = "SCREAMING TITLE" };
        
        await post.GenerateSlugAsync(post.Title, db);
        
        Assert.Equal("screaming-title", post.Slug);
    }

    [Fact]
    public async Task GenerateSlug_HandlesApostrophes()
    {
        using var db = new TestDbContext();
        var post = new Post { Title = "It's a Wonderful World" };
        
        await post.GenerateSlugAsync(post.Title, db);
        
        Assert.Equal("its-a-wonderful-world", post.Slug);
    }
}
