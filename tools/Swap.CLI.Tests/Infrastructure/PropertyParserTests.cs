using Swap.CLI.Infrastructure;
using Swap.CLI.Models;
using Xunit;

namespace Swap.CLI.Tests.Infrastructure;

public class PropertyParserTests
{
    [Fact]
    public void Parse_SimpleString_ReturnsCorrectProperty()
    {
        // Arrange
        var input = "name:string:256:required";

        // Act
        var result = PropertyParser.Parse(input);

        // Assert
        Assert.Equal("Name", result.Name);
        Assert.Equal("string", result.Type);
        Assert.Equal("string", result.CliType);
        Assert.Equal(256, result.MaxLength);
        Assert.True(result.IsRequired);
        Assert.False(result.IsNullable);
        Assert.False(result.IsCollection);
    }

    [Fact]
    public void Parse_Decimal_ReturnsCorrectProperty()
    {
        // Arrange
        var input = "price:decimal:18:2:required:min:0";

        // Act
        var result = PropertyParser.Parse(input);

        // Assert
        Assert.Equal("Price", result.Name);
        Assert.Equal("decimal", result.Type);
        Assert.Equal(18, result.Precision);
        Assert.Equal(2, result.Scale);
        Assert.True(result.IsRequired);
        Assert.Equal("0", result.MinValue);
    }

    [Fact]
    public void Parse_Guid_ReturnsCorrectProperty()
    {
        // Arrange
        var input = "id:guid:required";

        // Act
        var result = PropertyParser.Parse(input);

        // Assert
        Assert.Equal("Id", result.Name);
        Assert.Equal("Guid", result.Type);
        Assert.True(result.IsRequired);
    }

    [Fact]
    public void Parse_Bool_WithDefault_ReturnsCorrectProperty()
    {
        // Arrange
        var input = "isActive:bool:default:true";

        // Act
        var result = PropertyParser.Parse(input);

        // Assert
        Assert.Equal("IsActive", result.Name);
        Assert.Equal("bool", result.Type);
        Assert.Equal("true", result.DefaultValue);
        Assert.False(result.IsRequired);
    }

    [Fact]
    public void Parse_Enum_ReturnsCorrectProperty()
    {
        // Arrange
        var input = "status:enum:Draft,Published,Archived:default:Draft";

        // Act
        var result = PropertyParser.Parse(input);

        // Assert
        Assert.Equal("Status", result.Name);
        Assert.Equal("StatusEnum", result.Type);
        Assert.True(result.IsEnum);
        Assert.Equal(3, result.EnumValues.Count);
        Assert.Contains("Draft", result.EnumValues);
        Assert.Contains("Published", result.EnumValues);
        Assert.Contains("Archived", result.EnumValues);
        Assert.Equal("Draft", result.DefaultValue);
    }

    [Fact]
    public void Parse_ForeignKey_ReturnsCorrectProperty()
    {
        // Arrange
        var input = "categoryId:guid:fk:Category:Name:required";

        // Act
        var result = PropertyParser.Parse(input);

        // Assert
        Assert.Equal("CategoryId", result.Name);
        Assert.Equal("Guid", result.Type);
        Assert.Equal("Category", result.ForeignKey);
        Assert.Equal("Name", result.ForeignKeyDisplay);
        Assert.True(result.IsNavigationProperty);
        Assert.True(result.IsRequired);
    }

    [Fact]
    public void Parse_ForeignKey_WithoutDisplayProperty_ReturnsCorrectProperty()
    {
        // Arrange
        var input = "categoryId:guid:fk:Category:required";

        // Act
        var result = PropertyParser.Parse(input);

        // Assert
        Assert.Equal("CategoryId", result.Name);
        Assert.Equal("Category", result.ForeignKey);
        Assert.Null(result.ForeignKeyDisplay);
    }

    [Fact]
    public void Parse_Array_ReturnsCorrectProperty()
    {
        // Arrange
        var input = "tagIds:guid[]:fk:Tag:Name";

        // Act
        var result = PropertyParser.Parse(input);

        // Assert
        Assert.Equal("TagIds", result.Name);
        Assert.Equal("List<Guid>", result.Type);
        Assert.True(result.IsCollection);
        Assert.Equal("Tag", result.ForeignKey);
        Assert.Equal("Name", result.ForeignKeyDisplay);
    }

    [Fact]
    public void Parse_Text_ReturnsStringType()
    {
        // Arrange
        var input = "description:text";

        // Act
        var result = PropertyParser.Parse(input);

        // Assert
        Assert.Equal("Description", result.Name);
        Assert.Equal("string", result.Type);
        Assert.Equal("text", result.CliType);
    }

    [Fact]
    public void Parse_DateTime_ReturnsCorrectProperty()
    {
        // Arrange
        var input = "createdAt:datetime:required";

        // Act
        var result = PropertyParser.Parse(input);

        // Assert
        Assert.Equal("CreatedAt", result.Name);
        Assert.Equal("DateTime", result.Type);
        Assert.True(result.IsRequired);
    }

    [Fact]
    public void Parse_OptionalInt_IsNullable()
    {
        // Arrange
        var input = "count:int";

        // Act
        var result = PropertyParser.Parse(input);

        // Assert
        Assert.Equal("Count", result.Name);
        Assert.Equal("int", result.Type);
        Assert.False(result.IsRequired);
        Assert.True(result.IsNullable);
    }

    [Fact]
    public void Parse_InvalidFormat_ThrowsArgumentException()
    {
        // Arrange
        var input = "invalid";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => PropertyParser.Parse(input));
    }

    [Fact]
    public void Parse_UnknownType_ThrowsArgumentException()
    {
        // Arrange
        var input = "field:unknown:required";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => PropertyParser.Parse(input));
    }

    [Fact]
    public void ParseMultiple_ReturnsAllProperties()
    {
        // Arrange
        var inputs = new[]
        {
            "name:string:256:required",
            "price:decimal:18:2:required:min:0",
            "isActive:bool:default:true"
        };

        // Act
        var results = PropertyParser.ParseMultiple(inputs);

        // Assert
        Assert.Equal(3, results.Count);
        Assert.Equal("Name", results[0].Name);
        Assert.Equal("Price", results[1].Name);
        Assert.Equal("IsActive", results[2].Name);
    }

    [Fact]
    public void GeneratePropertyDeclaration_String_ReturnsCorrectCode()
    {
        // Arrange
        var prop = PropertyParser.Parse("name:string:256:required");

        // Act
        var result = PropertyParser.GeneratePropertyDeclaration(prop);

        // Assert
        Assert.Contains("[Required]", result);
        Assert.Contains("[MaxLength(256)]", result);
        Assert.Contains("public string Name { get; private set; }", result);
    }

    [Fact]
    public void GeneratePropertyDeclaration_Decimal_ReturnsCorrectCode()
    {
        // Arrange
        var prop = PropertyParser.Parse("price:decimal:18:2:required:min:0");

        // Act
        var result = PropertyParser.GeneratePropertyDeclaration(prop);

        // Assert
        Assert.Contains("[Required]", result);
        Assert.Contains("[Range(0, 999999999)]", result);
        Assert.Contains("[Column(TypeName = \"decimal(18,2)\")]", result);
        Assert.Contains("public decimal Price { get; private set; }", result);
    }

    [Fact]
    public void GeneratePropertyDeclaration_BoolWithDefault_ReturnsCorrectCode()
    {
        // Arrange
        var prop = PropertyParser.Parse("isActive:bool:default:true");

        // Act
        var result = PropertyParser.GeneratePropertyDeclaration(prop);

        // Assert
        Assert.Contains("public bool? IsActive { get; private set; } = true;", result);
    }

    [Fact]
    public void GenerateConstructorParameter_ReturnsCorrectCode()
    {
        // Arrange
        var prop = PropertyParser.Parse("name:string:256:required");

        // Act
        var result = PropertyParser.GenerateConstructorParameter(prop);

        // Assert
        Assert.Equal("string name", result);
    }

    [Fact]
    public void GenerateConstructorAssignment_RequiredString_HasGuard()
    {
        // Arrange
        var prop = PropertyParser.Parse("name:string:256:required");

        // Act
        var result = PropertyParser.GenerateConstructorAssignment(prop);

        // Assert
        Assert.Contains("Guard.NotNullOrEmpty(name, nameof(name))", result);
    }

    [Fact]
    public void GenerateConstructorAssignment_OptionalInt_NoGuard()
    {
        // Arrange
        var prop = PropertyParser.Parse("count:int");

        // Act
        var result = PropertyParser.GenerateConstructorAssignment(prop);

        // Assert
        Assert.Contains("Count = count;", result);
        Assert.DoesNotContain("Guard", result);
    }
}

