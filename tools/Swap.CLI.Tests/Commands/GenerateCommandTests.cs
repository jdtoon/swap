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
}
