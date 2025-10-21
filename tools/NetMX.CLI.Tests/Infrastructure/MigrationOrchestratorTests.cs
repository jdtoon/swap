using NetMX.CLI.Infrastructure;
using Xunit;
using FluentAssertions;

namespace NetMX.CLI.Tests.Infrastructure;

/// <summary>
/// Tests for MigrationOrchestrator
/// Note: These are integration-style tests that require a real project structure.
/// </summary>
public class MigrationOrchestratorTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly string _dbContextPath;

    public MigrationOrchestratorTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);

        // Create a minimal DbContext file
        var dataDir = Path.Combine(_testDirectory, "Data");
        Directory.CreateDirectory(dataDir);
        
        _dbContextPath = Path.Combine(dataDir, "TestDbContext.cs");
        var dbContextCode = @"using Microsoft.EntityFrameworkCore;

namespace TestApp.Data;

public class TestDbContext : DbContext
{
    public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }
}";
        File.WriteAllText(_dbContextPath, dbContextCode);
    }

    [Fact(Skip = "Integration test - requires full project structure and EF Core tools")]
    public async Task AddEntityWithMigrationAsync_ShouldAddDbSet_WhenMigrationSkipped()
    {
        // Arrange
        var orchestrator = new MigrationOrchestrator(_testDirectory, verbose: false);

        // Act
        var result = await orchestrator.AddEntityWithMigrationAsync(
            "Product",
            entityNamespace: null,
            createMigration: false,
            applyMigration: false);

        // Assert
        result.IsSuccess.Should().BeTrue($"because operation should succeed. Error: {result.Message}");
        result.Message.Should().Contain("Added DbSet<Product>");
        result.Steps.Should().HaveCount(1);
        result.Steps[0].Should().Contain("Added DbSet<Product>");

        // Verify DbContext was modified
        var updatedCode = await File.ReadAllTextAsync(_dbContextPath);
        updatedCode.Should().Contain("DbSet<Product>");
        updatedCode.Should().Contain("Products");
    }

    [Fact]
    public async Task AddEntityWithMigrationAsync_ShouldReturnFailure_WhenDbContextNotFound()
    {
        // Arrange
        var emptyDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(emptyDir);
        
        var orchestrator = new MigrationOrchestrator(emptyDir, verbose: false);

        try
        {
            // Act
            var result = await orchestrator.AddEntityWithMigrationAsync(
                "Product",
                createMigration: false);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Message.Should().Contain("No DbContext file found");
        }
        finally
        {
            Directory.Delete(emptyDir, true);
        }
    }

    [Fact(Skip = "Integration test - requires full project structure and EF Core tools")]
    public async Task AddEntityWithMigrationAsync_ShouldHandleMultipleEntities()
    {
        // Arrange
        var orchestrator = new MigrationOrchestrator(_testDirectory, verbose: false);

        // Act - Add first entity
        var result1 = await orchestrator.AddEntityWithMigrationAsync(
            "Product",
            createMigration: false);

        // Act - Add second entity
        var result2 = await orchestrator.AddEntityWithMigrationAsync(
            "Category",
            createMigration: false);

        // Assert
        result1.IsSuccess.Should().BeTrue();
        result2.IsSuccess.Should().BeTrue();

        var updatedCode = await File.ReadAllTextAsync(_dbContextPath);
        updatedCode.Should().Contain("DbSet<Product>");
        updatedCode.Should().Contain("DbSet<Category>");
    }

    [Fact(Skip = "Integration test - requires full project structure and EF Core tools")]
    public async Task AddEntityWithMigrationAsync_ShouldPreventDuplicates()
    {
        // Arrange
        var orchestrator = new MigrationOrchestrator(_testDirectory, verbose: false);

        // Act - Add entity first time
        var result1 = await orchestrator.AddEntityWithMigrationAsync(
            "Product",
            createMigration: false);

        // Act - Try to add same entity again
        var result2 = await orchestrator.AddEntityWithMigrationAsync(
            "Product",
            createMigration: false);

        // Assert
        result1.IsSuccess.Should().BeTrue();
        result2.IsSuccess.Should().BeFalse();
        result2.Message.Should().Contain("already exists");
    }

    [Fact]
    public void MigrationOrchestrator_ShouldThrowArgumentNull_WhenProjectDirectoryIsNull()
    {
        // Act & Assert
        var act = () => new MigrationOrchestrator(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact(Skip = "Integration test - requires full project structure and EF Core tools")]
    public async Task AddEntityWithMigrationAsync_ShouldIncludeStepsInResult()
    {
        // Arrange
        var orchestrator = new MigrationOrchestrator(_testDirectory, verbose: false);

        // Act
        var result = await orchestrator.AddEntityWithMigrationAsync(
            "Product",
            createMigration: false);

        // Assert
        result.Steps.Should().NotBeEmpty();
        result.Steps.Should().Contain(s => s.Contains("✅"));
        result.Steps.Should().Contain(s => s.Contains("DbSet<Product>"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            try
            {
                Directory.Delete(_testDirectory, true);
            }
            catch
            {
                // Ignore cleanup errors in tests
            }
        }
    }
}
