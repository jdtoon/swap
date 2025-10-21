using FluentAssertions;
using NetMX.CLI.Infrastructure;
using Xunit;

namespace NetMX.CLI.Tests.Infrastructure;

/// <summary>
/// Tests for DbContextInjector - Roslyn-based code injection
/// </summary>
public class DbContextInjectorTests : IDisposable
{
    private readonly string _testDirectory;

    public DbContextInjectorTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);
    }

    [Fact]
    public async Task AddDbSetAsync_ShouldInjectDbSetProperty()
    {
        // Arrange
        var dbContextPath = Path.Combine(_testDirectory, "TestDbContext.cs");
        var dbContextCode = @"using Microsoft.EntityFrameworkCore;

namespace TestApp.Data;

public class TestDbContext : DbContext
{
    public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }
}";
        await File.WriteAllTextAsync(dbContextPath, dbContextCode);

        // Act
        var result = await DbContextInjector.AddDbSetAsync(dbContextPath, "Product");

        // Assert
        result.Should().BeTrue();

        var updatedCode = await File.ReadAllTextAsync(dbContextPath);
        updatedCode.Should().Contain("DbSet<Product>");
        updatedCode.Should().Contain("Products");
        updatedCode.Should().Contain("=> Set<Product>();");
    }

    [Fact]
    public async Task AddDbSetAsync_ShouldNotAddDuplicateDbSet()
    {
        // Arrange
        var dbContextPath = Path.Combine(_testDirectory, "TestDbContext.cs");
        var dbContextCode = @"using Microsoft.EntityFrameworkCore;

namespace TestApp.Data;

public class TestDbContext : DbContext
{
    public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }

    public DbSet<Product> Products => Set<Product>();
}";
        await File.WriteAllTextAsync(dbContextPath, dbContextCode);

        // Act
        var result = await DbContextInjector.AddDbSetAsync(dbContextPath, "Product");

        // Assert
        result.Should().BeFalse(); // Should fail - already exists

        var updatedCode = await File.ReadAllTextAsync(dbContextPath);
        // Should only appear once
        var dbSetCount = updatedCode.Split("DbSet<Product>").Length - 1;
        dbSetCount.Should().Be(1);
    }

    [Fact]
    public async Task AddDbSetAsync_ShouldReturnFalseIfFileNotFound()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_testDirectory, "NonExistent.cs");

        // Act
        var result = await DbContextInjector.AddDbSetAsync(nonExistentPath, "Product");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task AddDbSetAsync_ShouldReturnFalseIfNoDbContextFound()
    {
        // Arrange
        var nonDbContextPath = Path.Combine(_testDirectory, "RegularClass.cs");
        var regularClassCode = @"namespace TestApp;

public class RegularClass
{
    public string Name { get; set; }
}";
        await File.WriteAllTextAsync(nonDbContextPath, regularClassCode);

        // Act
        var result = await DbContextInjector.AddDbSetAsync(nonDbContextPath, "Product");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task AddDbSetAsync_ShouldHandleMultipleEntities()
    {
        // Arrange
        var dbContextPath = Path.Combine(_testDirectory, "TestDbContext.cs");
        var dbContextCode = @"using Microsoft.EntityFrameworkCore;

namespace TestApp.Data;

public class TestDbContext : DbContext
{
    public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }
}";
        await File.WriteAllTextAsync(dbContextPath, dbContextCode);

        // Act
        var result1 = await DbContextInjector.AddDbSetAsync(dbContextPath, "Product");
        var result2 = await DbContextInjector.AddDbSetAsync(dbContextPath, "Category");
        var result3 = await DbContextInjector.AddDbSetAsync(dbContextPath, "Order");

        // Assert
        result1.Should().BeTrue();
        result2.Should().BeTrue();
        result3.Should().BeTrue();

        var updatedCode = await File.ReadAllTextAsync(dbContextPath);
        updatedCode.Should().Contain("DbSet<Product>");
        updatedCode.Should().Contain("DbSet<Category>");
        updatedCode.Should().Contain("DbSet<Order>");
        updatedCode.Should().Contain("Products");
        updatedCode.Should().Contain("Categories"); // Smart pluralization
        updatedCode.Should().Contain("Orders");
    }

    [Fact]
    public async Task AddDbSetAsync_ShouldPreserveExistingCode()
    {
        // Arrange
        var dbContextPath = Path.Combine(_testDirectory, "TestDbContext.cs");
        var dbContextCode = @"using Microsoft.EntityFrameworkCore;

namespace TestApp.Data;

public class TestDbContext : DbContext
{
    public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        // Custom configuration
        modelBuilder.Entity<User>().HasIndex(u => u.Email);
    }
}";
        await File.WriteAllTextAsync(dbContextPath, dbContextCode);

        // Act
        var result = await DbContextInjector.AddDbSetAsync(dbContextPath, "Product");

        // Assert
        result.Should().BeTrue();

        var updatedCode = await File.ReadAllTextAsync(dbContextPath);
        updatedCode.Should().Contain("DbSet<Product>");
        updatedCode.Should().Contain("OnModelCreating"); // Preserved
        updatedCode.Should().Contain("HasIndex"); // Preserved
    }

    [Fact]
    public void FindDbContext_ShouldReturnNullIfNoDbContextFound()
    {
        // Arrange - empty directory
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        var originalDir = Directory.GetCurrentDirectory();

        try
        {
            Directory.SetCurrentDirectory(tempDir);

            // Act
            var result = DbContextInjector.FindDbContext();

            // Assert
            result.Should().BeNull();
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDir);
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void FindDbContext_ShouldFindDbContextFile()
    {
        // Arrange
        var dbContextPath = Path.Combine(_testDirectory, "Data", "AppDbContext.cs");
        Directory.CreateDirectory(Path.Combine(_testDirectory, "Data"));
        File.WriteAllText(dbContextPath, "public class AppDbContext : DbContext { }");

        var originalDir = Directory.GetCurrentDirectory();

        try
        {
            Directory.SetCurrentDirectory(_testDirectory);

            // Act
            var result = DbContextInjector.FindDbContext();

            // Assert
            result.Should().NotBeNull();
            result.Should().EndWith("AppDbContext.cs");
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDir);
        }
    }

    [Fact]
    public void FindDbContext_ShouldIgnoreBinAndObjDirectories()
    {
        // Arrange
        var binPath = Path.Combine(_testDirectory, "bin", "Debug");
        var objPath = Path.Combine(_testDirectory, "obj", "Debug");
        Directory.CreateDirectory(binPath);
        Directory.CreateDirectory(objPath);

        // Create DbContext in bin and obj (should be ignored)
        File.WriteAllText(Path.Combine(binPath, "AppDbContext.cs"), "public class AppDbContext { }");
        File.WriteAllText(Path.Combine(objPath, "AppDbContext.cs"), "public class AppDbContext { }");

        // Create DbContext in root (should be found)
        var validPath = Path.Combine(_testDirectory, "AppDbContext.cs");
        File.WriteAllText(validPath, "public class AppDbContext { }");

        var originalDir = Directory.GetCurrentDirectory();

        try
        {
            Directory.SetCurrentDirectory(_testDirectory);

            // Act
            var result = DbContextInjector.FindDbContext();

            // Assert
            result.Should().NotBeNull();
            result.Should().Be(validPath);
            result.Should().NotContain("bin");
            result.Should().NotContain("obj");
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDir);
        }
    }

    // Cleanup
    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, recursive: true);
        }
    }
}
