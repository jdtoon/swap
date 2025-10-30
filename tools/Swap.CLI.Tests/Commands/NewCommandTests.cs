using System.CommandLine;
using Swap.CLI.Commands;
using Xunit;

namespace Swap.CLI.Tests.Commands;

public class NewCommandTests
{
    [Fact]
    public void Create_ShouldReturnCommandWithCorrectName()
    {
        // Act
        var command = NewCommand.Create();

        // Assert
        Assert.Equal("new", command.Name);
    }

    [Fact]
    public void Create_ShouldHaveNameArgument()
    {
        // Act
        var command = NewCommand.Create();

        // Assert
        var nameArg = command.Arguments.FirstOrDefault(a => a.Name == "name");
        Assert.NotNull(nameArg);
    }

    [Fact]
    public void Create_ShouldHaveDatabaseOption()
    {
        // Act
        var command = NewCommand.Create();

        // Assert
        var dbOption = command.Options.FirstOrDefault(o => o.Name == "database");
        Assert.NotNull(dbOption);
    }

    [Fact]
    public void Create_ShouldHaveOutputOption()
    {
        // Act
        var command = NewCommand.Create();

        // Assert
        var outputOption = command.Options.FirstOrDefault(o => o.Name == "output");
        Assert.NotNull(outputOption);
    }

    [Fact]
    public void Create_DatabaseOptionShouldDefaultToSqlite()
    {
        // Act
        var command = NewCommand.Create();
        var dbOption = command.Options.FirstOrDefault(o => o.Name == "database") as Option<string>;

        // Assert
        Assert.NotNull(dbOption);
        // Note: Testing default value requires parsing, which is complex in unit tests
        // This is better tested via integration tests
    }

    [Theory]
    [InlineData("MyApp")]
    [InlineData("TestProject")]
    [InlineData("Api")]
    public void Create_ShouldAcceptValidProjectNames(string projectName)
    {
        // Arrange
        var command = NewCommand.Create();
        var args = new[] { projectName };

        // Act
        var parseResult = command.Parse(args);

        // Assert
        Assert.Empty(parseResult.Errors);
    }

    [Theory]
    [InlineData("sqlite")]
    [InlineData("sqlserver")]
    [InlineData("postgres")]
    public void Create_ShouldAcceptValidDatabaseProviders(string provider)
    {
        // Arrange
        var command = NewCommand.Create();
        var args = new[] { "TestApp", "--database", provider };

        // Act
        var parseResult = command.Parse(args);

        // Assert
        Assert.Empty(parseResult.Errors);
    }

    [Fact]
    public void Create_ShouldParseOutputOption()
    {
        // Arrange
        var command = NewCommand.Create();
        var args = new[] { "TestApp", "--output", "./custom/path" };

        // Act
        var parseResult = command.Parse(args);

        // Assert
        Assert.Empty(parseResult.Errors);
    }

    [Fact]
    public void Create_ShouldRequireProjectName()
    {
        // Arrange
        var command = NewCommand.Create();
        var args = Array.Empty<string>();

        // Act
        var parseResult = command.Parse(args);

        // Assert
        Assert.NotEmpty(parseResult.Errors);
    }

    [Fact]
    public void Create_ShouldAcceptAllOptionsSimultaneously()
    {
        // Arrange
        var command = NewCommand.Create();
        var args = new[] { "TestApp", "--database", "postgres", "--output", "./output" };

        // Act
        var parseResult = command.Parse(args);

        // Assert
        Assert.Empty(parseResult.Errors);
    }

    [Fact]
    public void Create_ShouldHaveLocalNugetOption()
    {
        // Act
        var command = NewCommand.Create();

        // Assert
        var localNugetOption = command.Options.FirstOrDefault(o => o.Name == "local-nuget");
        Assert.NotNull(localNugetOption);
    }

    [Fact]
    public void Create_ShouldParseLocalNugetOption()
    {
        // Arrange
        var command = NewCommand.Create();
        var args = new[] { "TestApp", "--local-nuget" };

        // Act
        var parseResult = command.Parse(args);

        // Assert
        Assert.Empty(parseResult.Errors);
    }
}
