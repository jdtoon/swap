using NetMX.Testing;
using Xunit;

namespace NetMX.CLI.Tests.Commands.Database;

/// <summary>
/// Tests for the SeedCommand specifically.
/// </summary>
public class SeedCommandTests : IAsyncLifetime
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
    public async Task Discover_WithNoSeeders_ReturnsEmptyList()
    {
        // Act
        var result = await Runner.RunCliCommandAsync("db seed");

        // Assert
        Assert.True(result.Success);
        Assert.Contains("no seeders found", result.Output.ToLower());
    }

    [Fact]
    public async Task Discover_WithSeederInDataSeeders_FindsIt()
    {
        // Arrange
        var seedersDir = Path.Combine(ProjectPath, "Data", "Seeders");
        Directory.CreateDirectory(seedersDir);
        await File.WriteAllTextAsync(
            Path.Combine(seedersDir, "TestSeeder.cs"),
            "public class TestSeeder { }"
        );

        // Act
        var result = await Runner.RunCliCommandAsync("db seed");

        // Assert
        Assert.True(result.Success);
        Assert.Contains("TestSeeder", result.Output);
    }

    [Fact]
    public async Task Discover_WithSeederInDatabaseSeeders_FindsIt()
    {
        // Arrange
        var seedersDir = Path.Combine(ProjectPath, "Database", "Seeders");
        Directory.CreateDirectory(seedersDir);
        await File.WriteAllTextAsync(
            Path.Combine(seedersDir, "TestSeeder.cs"),
            "public class TestSeeder { }"
        );

        // Act
        var result = await Runner.RunCliCommandAsync("db seed");

        // Assert
        Assert.True(result.Success);
        Assert.Contains("TestSeeder", result.Output);
    }

    [Fact]
    public async Task Discover_WithMultipleSeeders_FindsAll()
    {
        // Arrange
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

        await File.WriteAllTextAsync(
            Path.Combine(seedersDir, "UserSeeder.cs"),
            "public class UserSeeder { }"
        );

        // Act
        var result = await Runner.RunCliCommandAsync("db seed");

        // Assert
        Assert.True(result.Success);
        Assert.Contains("3 seeder(s)", result.Output);
        Assert.Contains("ProductSeeder", result.Output);
        Assert.Contains("CategorySeeder", result.Output);
        Assert.Contains("UserSeeder", result.Output);
    }

    [Fact]
    public async Task Run_WithSpecificSeeder_OnlyRunsThatOne()
    {
        // Arrange
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

        // Act
        var result = await Runner.RunCliCommandAsync("db seed --seeder Product");

        // Assert
        Assert.True(result.Success);
        Assert.Contains("ProductSeeder", result.Output);
    }

    [Fact]
    public async Task Run_WithNonExistentSeeder_ReturnsError()
    {
        // Arrange
        var seedersDir = Path.Combine(ProjectPath, "Data", "Seeders");
        Directory.CreateDirectory(seedersDir);
        await File.WriteAllTextAsync(
            Path.Combine(seedersDir, "ProductSeeder.cs"),
            "public class ProductSeeder { }"
        );

        // Act
        var result = await Runner.RunCliCommandAsync("db seed --seeder NonExistent");

        // Assert
        // Should fail to find the seeder
        Assert.Contains("not found", result.Output.ToLower());
    }

    [Fact]
    public async Task Discover_IgnoresNonSeederFiles()
    {
        // Arrange
        var dataDir = Path.Combine(ProjectPath, "Data");
        Directory.CreateDirectory(dataDir);

        // Create seeder file
        await File.WriteAllTextAsync(
            Path.Combine(dataDir, "ProductSeeder.cs"),
            "public class ProductSeeder { }"
        );

        // Create non-seeder files
        await File.WriteAllTextAsync(
            Path.Combine(dataDir, "DbContext.cs"),
            "public class DbContext { }"
        );

        await File.WriteAllTextAsync(
            Path.Combine(dataDir, "Repository.cs"),
            "public class Repository { }"
        );

        // Act
        var result = await Runner.RunCliCommandAsync("db seed");

        // Assert
        Assert.True(result.Success);
        Assert.Contains("1 seeder(s)", result.Output); // Only ProductSeeder
        Assert.Contains("ProductSeeder", result.Output);
        Assert.DoesNotContain("DbContext", result.Output);
        Assert.DoesNotContain("Repository", result.Output);
    }

    [Fact]
    public async Task Discover_WithNestedDirectories_FindsAllSeeders()
    {
        // Arrange
        var seedersDir = Path.Combine(ProjectPath, "Data", "Seeders");
        var authSeedersDir = Path.Combine(seedersDir, "Auth");
        var catalogSeedersDir = Path.Combine(seedersDir, "Catalog");

        Directory.CreateDirectory(authSeedersDir);
        Directory.CreateDirectory(catalogSeedersDir);

        await File.WriteAllTextAsync(
            Path.Combine(authSeedersDir, "UserSeeder.cs"),
            "public class UserSeeder { }"
        );

        await File.WriteAllTextAsync(
            Path.Combine(catalogSeedersDir, "ProductSeeder.cs"),
            "public class ProductSeeder { }"
        );

        // Act
        var result = await Runner.RunCliCommandAsync("db seed");

        // Assert
        Assert.True(result.Success);
        Assert.Contains("2 seeder(s)", result.Output);
        Assert.Contains("UserSeeder", result.Output);
        Assert.Contains("ProductSeeder", result.Output);
    }
}
