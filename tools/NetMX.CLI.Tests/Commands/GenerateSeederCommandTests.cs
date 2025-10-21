using NetMX.CLI.Commands;
using Xunit;

namespace NetMX.CLI.Tests.Commands;

/// <summary>
/// Tests for GenerateSeederCommand
/// </summary>
public class GenerateSeederCommandTests
{
    [Fact]
    public void Constructor_AcceptsName()
    {
        // Arrange & Act
        var command = new GenerateSeederCommand("ProductSeeder");

        // Assert
        Assert.NotNull(command);
    }

    [Fact]
    public void Constructor_AcceptsNameAndModule()
    {
        // Arrange & Act
        var command = new GenerateSeederCommand("PermissionSeeder", "Authorization");

        // Assert
        Assert.NotNull(command);
    }

    [Fact]
    public void Constructor_AcceptsNullModule()
    {
        // Arrange & Act
        var command = new GenerateSeederCommand("ProductSeeder", null);

        // Assert
        Assert.NotNull(command);
    }
}
