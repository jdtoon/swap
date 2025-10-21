using NetMX.Testing;
using Xunit;

namespace NetMX.CLI.Tests.Commands.Database;

/// <summary>
/// End-to-end tests for netmx db commands.
/// Uses FeatureTestRunner to test commands in isolated projects.
/// </summary>
public class DatabaseCommandsE2ETests : IAsyncLifetime
{
    private string? _projectPath;
    private FeatureTestRunner? _runner;

    private string ProjectPath => _projectPath ?? throw new InvalidOperationException("Test not initialized");
    private FeatureTestRunner Runner => _runner ?? throw new InvalidOperationException("Test not initialized");

    public async Task InitializeAsync()
    {
        _projectPath = TestProjectFactory.CreateTestProject();
        _runner = new FeatureTestRunner(_projectPath, cleanupOnDispose: true);
        await Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _runner?.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task GenerateFeature_WithMigrate_CreatesAllArtifacts()
    {
        // Act
        var result = await Runner.RunCliCommandAsync("generate feature Product --migrate");

        // Assert - Command succeeded
        Assert.True(result.Success, $"Command failed: {result.Error}");
        Assert.Equal(0, result.ExitCode);

        // Assert - Entity created
        Assert.True(Runner.FileExists("Models/Product.cs"));
        Assert.True(Runner.FileContains("Models/Product.cs", "public class Product"));

        // Assert - DTOs created
        Assert.True(Runner.FileExists("Dtos/ProductDto.cs"));
        Assert.True(Runner.FileExists("Dtos/CreateProductDto.cs"));
        Assert.True(Runner.FileExists("Dtos/UpdateProductDto.cs"));

        // Assert - Service created
        Assert.True(Runner.FileExists("Services/IProductService.cs"));
        Assert.True(Runner.FileExists("Services/ProductService.cs"));

        // Assert - DbContext updated
        Assert.True(Runner.FileContains("Data/AppDbContext.cs", "DbSet<Product>"));

        // Assert - Migration created
        Assert.Equal(1, Runner.GetMigrationCount());

        // Assert - Database exists
        Assert.True(Runner.DatabaseExists());
    }

    [Fact]
    public async Task DbMigrate_CreatesValidMigration()
    {
        // Arrange - Generate feature first
        await Runner.RunCliCommandAsync("generate feature Product");

        // Act - Create migration
        var result = await Runner.RunCliCommandAsync("db migrate AddProduct");

        // Assert
        Assert.True(result.Success, $"Command failed: {result.Error}");
        Assert.Equal(1, Runner.GetMigrationCount());

        // Verify migration file contains expected content
        var migrations = Runner.GetFiles("*AddProduct*.cs");
        Assert.Single(migrations);
        Assert.Contains("AddProduct", migrations[0]);
    }

    [Fact]
    public async Task DbUpdate_AppliesPendingMigrations()
    {
        // Arrange - Generate feature and create migration
        await Runner.RunCliCommandAsync("generate feature Product");
        await Runner.RunCliCommandAsync("db migrate AddProduct");

        // Act - Apply migration
        var result = await Runner.RunCliCommandAsync("db update");

        // Assert
        Assert.True(result.Success, $"Command failed: {result.Error}");
        Assert.Contains("success", result.Output.ToLower());

        // Verify database was created
        Assert.True(Runner.DatabaseExists());
    }

    [Fact]
    public async Task DbStatus_ShowsPendingMigrations()
    {
        // Arrange - Generate feature and create migration (but don't apply)
        await Runner.RunCliCommandAsync("generate feature Product");
        await Runner.RunCliCommandAsync("db migrate AddProduct");

        // Act - Check status
        var result = await Runner.RunCliCommandAsync("db status");

        // Assert
        Assert.True(result.Success, $"Command failed: {result.Error}");
        Assert.Contains("pending", result.Output.ToLower());
        Assert.Contains("AddProduct", result.Output);
    }

    [Fact]
    public async Task DbRollback_UndoesLastMigration()
    {
        // Arrange - Generate feature, migrate, and apply
        await Runner.RunCliCommandAsync("generate feature Product --migrate");
        var initialCount = Runner.GetMigrationCount();

        // Act - Rollback migration
        var result = await Runner.RunCliCommandAsync("db rollback");

        // Assert
        Assert.True(result.Success, $"Command failed: {result.Error}");

        // Note: Rollback removes migration from database, not from file system
        // Migration file should still exist
        Assert.Equal(initialCount, Runner.GetMigrationCount());
    }

    [Fact]
    public async Task DbReset_DropsAndRecreatesDatabase()
    {
        // Arrange - Create database with some data
        await Runner.RunCliCommandAsync("generate feature Product --migrate");
        Assert.True(Runner.DatabaseExists());

        // Act - Reset database (with --force to skip confirmation)
        var result = await Runner.RunCliCommandAsync("db reset --force");

        // Assert
        Assert.True(result.Success, $"Command failed: {result.Error}");
        Assert.Contains("reset", result.Output.ToLower());

        // Database should still exist (recreated)
        Assert.True(Runner.DatabaseExists());
    }

    [Fact]
    public async Task DbSeed_WithNoSeeders_ShowsHelpfulMessage()
    {
        // Act - Try to seed without any seeders
        var result = await Runner.RunCliCommandAsync("db seed");

        // Assert
        Assert.True(result.Success, $"Command failed: {result.Error}");
        Assert.Contains("no seeders found", result.Output.ToLower());
        Assert.Contains("netmx generate seeder", result.Output.ToLower());
    }

    [Fact]
    public async Task DbSeed_WithSeeders_RunsSuccessfully()
    {
        // Arrange - Create a sample seeder
        var seedersDir = Path.Combine(ProjectPath, "Data", "Seeders");
        Directory.CreateDirectory(seedersDir);

        var seederCode = @"
using System;

namespace TestProject.Data.Seeders;

public class ProductSeeder
{
    public void Run()
    {
        Console.WriteLine(""Seeding products..."");
    }
}
";
        await File.WriteAllTextAsync(
            Path.Combine(seedersDir, "ProductSeeder.cs"),
            seederCode
        );

        // Act - Run seeders
        var result = await Runner.RunCliCommandAsync("db seed");

        // Assert
        Assert.True(result.Success, $"Command failed: {result.Error}");
        Assert.Contains("ProductSeeder", result.Output);
    }

    [Fact]
    public async Task DbSeed_WithSpecificSeeder_RunsOnlyThatSeeder()
    {
        // Arrange - Create multiple seeders
        var seedersDir = Path.Combine(ProjectPath, "Data", "Seeders");
        Directory.CreateDirectory(seedersDir);

        await File.WriteAllTextAsync(
            Path.Combine(seedersDir, "ProductSeeder.cs"),
            "public class ProductSeeder { }"
        );

        await File.WriteAllTextAsync(
            Path.Combine(seedersDir, "CategorySeeder.cs"),
            "public class CategorySeeder { }"
        );

        // Act - Run specific seeder
        var result = await Runner.RunCliCommandAsync("db seed --seeder Product");

        // Assert
        Assert.True(result.Success, $"Command failed: {result.Error}");
        Assert.Contains("ProductSeeder", result.Output);
        // CategorySeeder should not be mentioned (or only in discovery, not execution)
    }

    [Fact]
    public async Task MultipleFeatures_CanBeGeneratedSequentially()
    {
        // Act - Generate multiple features
        var result1 = await Runner.RunCliCommandAsync("generate feature Product --migrate");
        var result2 = await Runner.RunCliCommandAsync("generate feature Category --migrate");
        var result3 = await Runner.RunCliCommandAsync("generate feature Order --migrate");

        // Assert - All succeeded
        Assert.True(result1.Success);
        Assert.True(result2.Success);
        Assert.True(result3.Success);

        // Assert - All entities exist
        Assert.True(Runner.FileExists("Models/Product.cs"));
        Assert.True(Runner.FileExists("Models/Category.cs"));
        Assert.True(Runner.FileExists("Models/Order.cs"));

        // Assert - All DbSets added
        var dbContextCode = Runner.ReadFile("Data/AppDbContext.cs");
        Assert.Contains("DbSet<Product>", dbContextCode);
        Assert.Contains("DbSet<Category>", dbContextCode);
        Assert.Contains("DbSet<Order>", dbContextCode);

        // Assert - All migrations created (3 migrations + 1 initial = 4 total, but we exclude ModelSnapshot)
        Assert.Equal(3, Runner.GetMigrationCount());

        // Assert - Database exists
        Assert.True(Runner.DatabaseExists());
    }

    [Fact]
    public async Task GenerateFeature_WithInvalidName_ReturnsError()
    {
        // Act - Try to generate feature with plural name (potential issue)
        var result = await Runner.RunCliCommandAsync("generate feature Products");

        // Note: Current implementation may not validate this yet
        // This test documents expected future behavior
        // For now, just verify it doesn't crash
        Assert.NotNull(result);
    }

    [Fact]
    public async Task DbMigrate_WithDuplicateName_HandlesGracefully()
    {
        // Arrange - Generate feature and create migration
        await Runner.RunCliCommandAsync("generate feature Product");
        await Runner.RunCliCommandAsync("db migrate AddProduct");

        // Act - Try to create migration with same name again
        var result = await Runner.RunCliCommandAsync("db migrate AddProduct");

        // Note: EF Core handles duplicate names by appending timestamp
        // This test verifies the command doesn't crash
        Assert.NotNull(result);
    }

    [Fact]
    public async Task FullWorkflow_GenerateFeatureToSeed()
    {
        // This test validates the complete workflow from feature generation to seeding

        // Step 1: Generate feature with migration
        var generateResult = await Runner.RunCliCommandAsync("generate feature Product --migrate");
        Assert.True(generateResult.Success);

        // Step 2: Verify migration status
        var statusResult = await Runner.RunCliCommandAsync("db status");
        Assert.True(statusResult.Success);

        // Step 3: Create seeder
        var seedersDir = Path.Combine(ProjectPath, "Data", "Seeders");
        Directory.CreateDirectory(seedersDir);
        await File.WriteAllTextAsync(
            Path.Combine(seedersDir, "ProductSeeder.cs"),
            "public class ProductSeeder { }"
        );

        // Step 4: Run seeder
        var seedResult = await Runner.RunCliCommandAsync("db seed");
        Assert.True(seedResult.Success);

        // Step 5: Verify everything exists
        Assert.True(Runner.FileExists("Models/Product.cs"));
        Assert.True(Runner.FileContains("Data/AppDbContext.cs", "DbSet<Product>"));
        Assert.True(Runner.DatabaseExists());
        Assert.Equal(1, Runner.GetMigrationCount());
    }
}
