using System.CommandLine;
using Swap.CLI.Commands;
using Xunit;

namespace Swap.CLI.Tests.Commands;

public class GenerateCommandTests
{
    [Fact]
    public void Create_ShouldReturnCommandWithCorrectName()
    {
        // Act
        var command = GenerateCommand.Create();

        // Assert
        Assert.Equal("generate", command.Name);
    }

    [Fact]
    public void Create_ShouldHaveAlias()
    {
        // Act
        var command = GenerateCommand.Create();

        // Assert
        Assert.Contains("g", command.Aliases);
    }

    [Fact]
    public void Create_ShouldHaveControllerSubcommand()
    {
        // Act
        var command = GenerateCommand.Create();

        // Assert
        var controllerCommand = command.Subcommands.FirstOrDefault(c => c.Name == "controller");
        Assert.NotNull(controllerCommand);
    }

    [Fact]
    public void Create_ShouldParseGenerateControllerCommand()
    {
        // Arrange
        var command = GenerateCommand.Create();
        var args = new[] { "controller", "Product" };

        // Act
        var parseResult = command.Parse(args);

        // Assert
        Assert.Empty(parseResult.Errors);
    }

    [Fact]
    public void Create_ShouldParseGenerateWithAlias()
    {
        // Arrange
        var rootCommand = new RootCommand();
        var generateCommand = GenerateCommand.Create();
        rootCommand.AddCommand(generateCommand);
        var args = new[] { "g", "controller", "Product" };

        // Act
        var parseResult = rootCommand.Parse(args);

        // Assert
        Assert.Empty(parseResult.Errors);
    }
}
