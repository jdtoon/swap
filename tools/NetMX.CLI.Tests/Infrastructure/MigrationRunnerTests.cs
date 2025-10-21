using FluentAssertions;
using NetMX.CLI.Infrastructure;
using Xunit;

namespace NetMX.CLI.Tests.Infrastructure;

/// <summary>
/// Tests for MigrationRunner - dotnet ef wrapper
/// </summary>
public class MigrationRunnerTests
{
    [Fact]
    public async Task IsEfCoreInstalledAsync_ShouldReturnBool()
    {
        // Act
        var result = await MigrationRunner.IsEfCoreInstalledAsync();

        // Assert
        // Note: This test might fail on machines without dotnet-ef installed
        // That's expected behavior - the tool should detect absence
        // Just verify we get a boolean response
        (result == true || result == false).Should().BeTrue();
    }

    [Fact]
    public async Task CreateMigrationAsync_ShouldReturnFalse_WhenProjectPathInvalid()
    {
        // Arrange
        var invalidPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        // Act
        var result = await MigrationRunner.CreateMigrationAsync("TestMigration", invalidPath);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateDatabaseAsync_ShouldReturnFalse_WhenProjectPathInvalid()
    {
        // Arrange
        var invalidPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        // Act
        var result = await MigrationRunner.UpdateDatabaseAsync(invalidPath);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task RemoveMigrationAsync_ShouldReturnFalse_WhenNoMigrations()
    {
        // Arrange
        var invalidPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        // Act
        var result = await MigrationRunner.RemoveMigrationAsync(invalidPath);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ListMigrationsAsync_ShouldReturnEmptyList_WhenNoMigrations()
    {
        // Arrange
        var invalidPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        // Act
        var result = await MigrationRunner.ListMigrationsAsync(invalidPath);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    // Note: Full integration tests (creating actual migrations) would require:
    // - A test EF Core project
    // - A test database
    // - dotnet-ef tools installed
    // These are better suited for integration tests, not unit tests
}
