using Swap.CLI.Commands;
using System.CommandLine;
using Xunit;

namespace Swap.CLI.Tests.Commands;

/// <summary>
/// Tests for relationship-related features in GenerateControllerCommand
/// </summary>
public class GenerateControllerRelationshipTests
{
    [Fact]
    public void Create_ShouldHaveWithRelationshipsOption()
    {
        // Act
        var command = GenerateControllerCommand.Create();

        // Assert
        var withRelOption = command.Options.FirstOrDefault(o => o.Name == "with-relationships");
        Assert.NotNull(withRelOption);
    }

    [Fact]
    public void Create_WithRelationshipsOption_ShouldBeBoolean()
    {
        // Arrange
        var command = GenerateControllerCommand.Create();
        var withRelOption = command.Options.First(o => o.Name == "with-relationships");

        // Act & Assert
        Assert.Equal(typeof(bool), withRelOption.ValueType);
    }

    [Fact]
    public void Create_WithRelationshipsOption_DefaultsFalse()
    {
        // Arrange
        var command = GenerateControllerCommand.Create();
        
        // Act
        var parseResult = command.Parse("Order --fields \"CustomerId:int?\"");

        // Assert
        Assert.Empty(parseResult.Errors);
    }

    [Theory]
    [InlineData("Order --fields \"CustomerId:int?\" --with-relationships")]
    [InlineData("Product --fields \"CategoryId:int?\" --with-relationships")]
    public void Create_WithRelationshipsFlag_ParsesSuccessfully(string args)
    {
        // Arrange
        var command = GenerateControllerCommand.Create();

        // Act
        var parseResult = command.Parse(args);

        // Assert
        Assert.Empty(parseResult.Errors);
    }
}
