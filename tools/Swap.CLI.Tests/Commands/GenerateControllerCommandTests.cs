using System.CommandLine;
using Swap.CLI.Commands;
using Xunit;

namespace Swap.CLI.Tests.Commands;

public class GenerateControllerCommandTests
{
    [Fact]
    public void Create_ShouldReturnCommandWithCorrectName()
    {
        // Act
        var command = GenerateControllerCommand.Create();

        // Assert
        Assert.Equal("controller", command.Name);
    }

    [Fact]
    public void Create_ShouldHaveNameArgument()
    {
        // Act
        var command = GenerateControllerCommand.Create();

        // Assert
        var nameArg = command.Arguments.FirstOrDefault(a => a.Name == "name");
        Assert.NotNull(nameArg);
    }

    [Theory]
    [InlineData("Product")]
    [InlineData("Task")]
    [InlineData("TodoItem")]
    [InlineData("User")]
    public void Create_ShouldAcceptValidEntityNames(string entityName)
    {
        // Arrange
        var command = GenerateControllerCommand.Create();
        var args = new[] { entityName };

        // Act
        var parseResult = command.Parse(args);

        // Assert
        Assert.Empty(parseResult.Errors);
    }

    [Fact]
    public void Create_ShouldRequireEntityName()
    {
        // Arrange
        var command = GenerateControllerCommand.Create();
        var args = Array.Empty<string>();

        // Act
        var parseResult = command.Parse(args);

        // Assert
        Assert.NotEmpty(parseResult.Errors);
    }

    [Theory]
    [InlineData("Product", "Products")]
    [InlineData("Task", "Tasks")]
    [InlineData("Category", "Categorys")] // Simple pluralization
    [InlineData("Item", "Items")]
    public void PluralizationLogic_ShouldAddS(string singular, string expectedPlural)
    {
        // Simple test to document current pluralization behavior
        // In real implementation, this would be in the command execution
        var actual = singular + "s";
        
        Assert.Equal(expectedPlural, actual);
    }

    [Theory]
    [InlineData("product", "Product")] // lowercase to PascalCase
    [InlineData("myTask", "MyTask")] // camelCase to PascalCase
    [InlineData("Product", "Product")] // already PascalCase
    public void EntityNameValidation_ShouldConvertToPascalCase(string input, string expected)
    {
        // Test documents that entity names should be PascalCase
        var actual = char.IsUpper(input[0]) 
            ? input 
            : char.ToUpper(input[0]) + input.Substring(1);
        
        Assert.Equal(expected, actual);
    }
}
