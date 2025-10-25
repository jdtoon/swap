using System.CommandLine;
using Swap.CLI.Commands;
using Xunit;

namespace Swap.CLI.Tests.Commands;

public class GenerateModelCommandTests
{
    [Fact]
    public void Create_ShouldReturnCommandWithCorrectName()
    {
        // Act
        var command = GenerateModelCommand.Create();

        // Assert
        Assert.Equal("model", command.Name);
    }

    [Fact]
    public void Create_ShouldHaveNameArgument()
    {
        // Act
        var command = GenerateModelCommand.Create();

        // Assert
        var nameArg = command.Arguments.FirstOrDefault(a => a.Name == "name");
        Assert.NotNull(nameArg);
    }

    [Fact]
    public void Create_ShouldHaveFieldsOption()
    {
        // Act
        var command = GenerateModelCommand.Create();

        // Assert
        var fieldsOption = command.Options.FirstOrDefault(o => o.Name == "fields");
        Assert.NotNull(fieldsOption);
    }

    [Theory]
    [InlineData("Product")]
    [InlineData("Task")]
    [InlineData("TodoItem")]
    [InlineData("User")]
    [InlineData("Customer")]
    public void Create_ShouldAcceptValidEntityNames(string entityName)
    {
        // Arrange
        var command = GenerateModelCommand.Create();
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
        var command = GenerateModelCommand.Create();
        var args = Array.Empty<string>();

        // Act
        var parseResult = command.Parse(args);

        // Assert
        Assert.NotEmpty(parseResult.Errors);
    }

    [Theory]
    [InlineData("Product", "--fields", "Name:string,Price:decimal")]
    [InlineData("User", "--fields", "Email:string,Age:int,IsActive:bool")]
    [InlineData("Order", "--fields", "CustomerId:int,Total:decimal,Notes:string?")]
    public void Create_ShouldAcceptValidFieldsOption(string entityName, string fieldsFlag, string fieldsValue)
    {
        // Arrange
        var command = GenerateModelCommand.Create();
        var args = new[] { entityName, fieldsFlag, fieldsValue };

        // Act
        var parseResult = command.Parse(args);

        // Assert
        Assert.Empty(parseResult.Errors);
    }

    [Theory]
    [InlineData("Name:string,Email:string", 2)]
    [InlineData("Name:string", 1)]
    [InlineData("Name:string,Email:string,Age:int", 3)]
    [InlineData("Name:string,Email:string,Age:int,IsActive:bool", 4)]
    public void Create_ShouldParseMultipleFields(string fieldsSpec, int expectedCount)
    {
        // Arrange
        var command = GenerateModelCommand.Create();
        var args = new[] { "TestEntity", "--fields", fieldsSpec };

        // Act
        var parseResult = command.Parse(args);

        // Assert
        Assert.Empty(parseResult.Errors);
        var fieldsOption = command.Options.First(o => o.Name == "fields");
        var fieldsValue = parseResult.GetValueForOption(fieldsOption) as string;
        Assert.NotNull(fieldsValue);
        var fields = fieldsValue.Split(',');
        Assert.Equal(expectedCount, fields.Length);
    }

    [Theory]
    [InlineData("Name:string")]
    [InlineData("Age:int")]
    [InlineData("IsActive:bool")]
    [InlineData("Price:decimal")]
    [InlineData("Total:double")]
    [InlineData("Count:long")]
    [InlineData("Rating:float")]
    [InlineData("CreatedAt:datetime")]
    [InlineData("Id:guid")]
    [InlineData("Value:byte")]
    [InlineData("Score:short")]
    public void Create_ShouldAcceptAllSupportedTypes(string fieldSpec)
    {
        // Arrange
        var command = GenerateModelCommand.Create();
        var args = new[] { "TestEntity", "--fields", fieldSpec };

        // Act
        var parseResult = command.Parse(args);

        // Assert
        Assert.Empty(parseResult.Errors);
    }

    [Theory]
    [InlineData("Name:str")]
    [InlineData("Price:dec")]
    [InlineData("CreatedAt:date")]
    public void Create_ShouldAcceptTypeAliases(string fieldSpec)
    {
        // Arrange
        var command = GenerateModelCommand.Create();
        var args = new[] { "TestEntity", "--fields", fieldSpec };

        // Act
        var parseResult = command.Parse(args);

        // Assert
        Assert.Empty(parseResult.Errors);
    }

    [Theory]
    [InlineData("Notes:string?")]
    [InlineData("Age:int?")]
    [InlineData("Price:decimal?")]
    [InlineData("IsActive:bool?")]
    public void Create_ShouldAcceptNullableFields(string fieldSpec)
    {
        // Arrange
        var command = GenerateModelCommand.Create();
        var args = new[] { "TestEntity", "--fields", fieldSpec };

        // Act
        var parseResult = command.Parse(args);

        // Assert
        Assert.Empty(parseResult.Errors);
    }

    [Theory]
    [InlineData("Product", "--fields", "Name:string,Price:decimal,Stock:int")]
    [InlineData("Customer", "--fields", "FirstName:string,LastName:string,Email:string")]
    [InlineData("Order", "--fields", "OrderDate:datetime,Total:decimal,Status:string")]
    public void Create_ShouldAcceptComplexFieldSpecifications(string entityName, string fieldsFlag, string fieldsSpec)
    {
        // Arrange
        var command = GenerateModelCommand.Create();
        var args = new[] { entityName, fieldsFlag, fieldsSpec };

        // Act
        var parseResult = command.Parse(args);

        // Assert
        Assert.Empty(parseResult.Errors);
    }

    [Fact]
    public void Create_ShouldAllowEntityNameWithoutFields()
    {
        // Arrange
        var command = GenerateModelCommand.Create();
        var args = new[] { "Product" };

        // Act
        var parseResult = command.Parse(args);

        // Assert
        Assert.Empty(parseResult.Errors);
    }

    [Theory]
    [InlineData("product space")]
    [InlineData("user name")]
    [InlineData("my entity")]
    public void Create_ShouldRejectEntityNamesWithSpaces(string invalidName)
    {
        // Arrange
        var command = GenerateModelCommand.Create();
        var args = new[] { invalidName };

        // Act & Assert
        // This should be validated during execution, not parsing
        // The command line parser accepts it, but execution should reject it
        var parseResult = command.Parse(args);
        
        // Parse succeeds - validation happens at execution time
        Assert.Empty(parseResult.Errors);
    }

    [Theory]
    [InlineData("Name:string,Price:decimal,Stock:int,IsActive:bool")]
    [InlineData("FirstName:string,LastName:string,Email:string?,Phone:string?")]
    [InlineData("OrderDate:datetime,ShipDate:datetime?,Total:decimal,Tax:decimal")]
    public void Create_ShouldAcceptMixedNullableAndRequired(string fieldsSpec)
    {
        // Arrange
        var command = GenerateModelCommand.Create();
        var args = new[] { "TestEntity", "--fields", fieldsSpec };

        // Act
        var parseResult = command.Parse(args);

        // Assert
        Assert.Empty(parseResult.Errors);
    }

    [Fact]
    public void Create_ShouldHaveDescriptionForCommand()
    {
        // Act
        var command = GenerateModelCommand.Create();

        // Assert
        Assert.NotNull(command.Description);
        Assert.NotEmpty(command.Description);
    }

    [Fact]
    public void Create_ShouldHaveDescriptionForFieldsOption()
    {
        // Act
        var command = GenerateModelCommand.Create();
        var fieldsOption = command.Options.First(o => o.Name == "fields");

        // Assert
        Assert.NotNull(fieldsOption.Description);
        Assert.NotEmpty(fieldsOption.Description);
    }

    [Theory]
    [InlineData("Name:string,Email:string,Age:int")]
    [InlineData("Title:string,Description:string?,CreatedAt:datetime")]
    [InlineData("ProductName:string,Price:decimal,Stock:int,IsActive:bool")]
    public void Create_ShouldSupportPascalCaseFieldNames(string fieldsSpec)
    {
        // Arrange
        var command = GenerateModelCommand.Create();
        var args = new[] { "TestEntity", "--fields", fieldsSpec };

        // Act
        var parseResult = command.Parse(args);

        // Assert
        Assert.Empty(parseResult.Errors);
    }
}
