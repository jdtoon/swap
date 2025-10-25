using Swap.CLI.Infrastructure;
using Swap.CLI.Models;
using Xunit;

namespace Swap.CLI.Tests.Infrastructure;

public class EntityGeneratorTests
{
    [Fact]
    public void GenerateEntity_SimpleEntity_ReturnsValidCode()
    {
        // Arrange
        var options = new EntityGenerationOptions
        {
            EntityName = "Product",
            Properties = new List<PropertyDefinition>
            {
                PropertyParser.Parse("name:string:256:required"),
                PropertyParser.Parse("price:decimal:18:2:required:min:0")
            }
        };

        // Act
        var code = EntityGenerator.GenerateEntity(options);

        // Assert
        Assert.Contains("public class Product : AggregateRoot<Guid>", code);
        Assert.Contains("public string Name { get; private set; }", code);
        Assert.Contains("public decimal Price { get; private set; }", code);
        Assert.Contains("public Product(Guid id, string name, decimal price)", code);
        Assert.Contains("Id = id;", code);
        Assert.Contains("Guard.NotNullOrEmpty(name, nameof(name))", code);
        Assert.Contains("public void SetName(string name)", code);
        Assert.Contains("public void SetPrice(decimal price)", code);
    }

    [Fact]
    public void GenerateEntity_WithAuditFields_IncludesTimestamps()
    {
        // Arrange
        var options = new EntityGenerationOptions
        {
            EntityName = "Product",
            Properties = new List<PropertyDefinition>
            {
                PropertyParser.Parse("name:string:256:required")
            },
            IncludeAuditFields = true
        };

        // Act
        var code = EntityGenerator.GenerateEntity(options);

        // Assert
        Assert.Contains("public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;", code);
        Assert.Contains("public DateTime? UpdatedAt { get; private set; }", code);
        Assert.Contains("UpdatedAt = DateTime.UtcNow;", code);
    }

    [Fact]
    public void GenerateEntity_WithSoftDelete_IncludesDeleteMethods()
    {
        // Arrange
        var options = new EntityGenerationOptions
        {
            EntityName = "Product",
            Properties = new List<PropertyDefinition>
            {
                PropertyParser.Parse("name:string:256:required")
            },
            IncludeSoftDelete = true
        };

        // Act
        var code = EntityGenerator.GenerateEntity(options);

        // Assert
        Assert.Contains("public bool IsDeleted { get; private set; }", code);
        Assert.Contains("public DateTime? DeletedAt { get; private set; }", code);
        Assert.Contains("public void Delete()", code);
        Assert.Contains("public void Restore()", code);
    }

    [Fact]
    public void GenerateEntity_WithForeignKey_IncludesNavigationProperty()
    {
        // Arrange
        var options = new EntityGenerationOptions
        {
            EntityName = "Product",
            Properties = new List<PropertyDefinition>
            {
                PropertyParser.Parse("name:string:256:required"),
                PropertyParser.Parse("categoryId:guid:fk:Category:Name:required")
            }
        };

        // Act
        var code = EntityGenerator.GenerateEntity(options);

        // Assert
        Assert.Contains("public Guid CategoryId { get; private set; }", code);
        Assert.Contains("public Category? Category { get; set; }", code);
        Assert.Contains("Navigation property to Category", code);
    }

    [Fact]
    public void GenerateEntity_WithCollection_IncludesCollectionProperty()
    {
        // Arrange
        var options = new EntityGenerationOptions
        {
            EntityName = "Product",
            Properties = new List<PropertyDefinition>
            {
                PropertyParser.Parse("name:string:256:required"),
                PropertyParser.Parse("tagIds:guid[]:fk:Tag:Name")
            }
        };

        // Act
        var code = EntityGenerator.GenerateEntity(options);

        // Assert
        Assert.Contains("public ICollection<Tag> Tags { get; set; } = new List<Tag>();", code);
        Assert.Contains("Navigation collection to Tag", code);
    }

    [Fact]
    public void GenerateEntity_WithModule_UsesModuleNamespace()
    {
        // Arrange
        var options = new EntityGenerationOptions
        {
            EntityName = "Product",
            ModuleName = "Catalog",
            Properties = new List<PropertyDefinition>
            {
                PropertyParser.Parse("name:string:256:required")
            }
        };

        // Act
        var code = EntityGenerator.GenerateEntity(options);

        // Assert
        Assert.Contains("namespace Catalog.Core.Entities;", code);
    }

    [Fact]
    public void GenerateEntity_WithoutModule_UsesModelsNamespace()
    {
        // Arrange
        var options = new EntityGenerationOptions
        {
            EntityName = "Product",
            Properties = new List<PropertyDefinition>
            {
                PropertyParser.Parse("name:string:256:required")
            }
        };

        // Act
        var code = EntityGenerator.GenerateEntity(options);

        // Assert
        Assert.Contains("namespace Models;", code);
    }

    [Fact]
    public void GenerateEntity_WithOptionalProperties_AllowsNullInSetters()
    {
        // Arrange
        var options = new EntityGenerationOptions
        {
            EntityName = "Product",
            Properties = new List<PropertyDefinition>
            {
                PropertyParser.Parse("name:string:256:required"),
                PropertyParser.Parse("description:text") // Optional
            }
        };

        // Act
        var code = EntityGenerator.GenerateEntity(options);

        // Assert
        // Note: "text" maps to "string" which doesn't get ? suffix
        Assert.Contains("public void SetDescription(string", code);
        Assert.Contains("Description = description;", code);
        Assert.DoesNotContain("Guard.NotNullOrEmpty(description", code);
    }

    [Fact]
    public void GenerateEntity_PrivateConstructor_ForEfCore()
    {
        // Arrange
        var options = new EntityGenerationOptions
        {
            EntityName = "Product",
            Properties = new List<PropertyDefinition>
            {
                PropertyParser.Parse("name:string:256:required")
            }
        };

        // Act
        var code = EntityGenerator.GenerateEntity(options);

        // Assert
        Assert.Contains("private Product() { }", code);
        Assert.Contains("Private constructor for EF Core", code);
    }

    [Fact]
    public void GenerateEnum_ValidEnumProperty_ReturnsEnumCode()
    {
        // Arrange
        var prop = PropertyParser.Parse("status:enum:Draft,Published,Archived");

        // Act
        var code = EntityGenerator.GenerateEnum(prop);

        // Assert
        Assert.Contains("public enum StatusEnum", code);
        Assert.Contains("Draft = 0", code);
        Assert.Contains("Published = 1", code);
        Assert.Contains("Archived = 2", code);
    }

    [Fact]
    public void GenerateEnum_NonEnumProperty_ThrowsException()
    {
        // Arrange
        var prop = PropertyParser.Parse("name:string:256:required");

        // Act & Assert
        Assert.Throws<ArgumentException>(() => EntityGenerator.GenerateEnum(prop));
    }

    [Fact]
    public void EntityGenerationOptions_HasPagination_ReturnsTrueWhenSet()
    {
        // Arrange
        var options = new EntityGenerationOptions
        {
            PageSize = 20
        };

        // Act & Assert
        Assert.True(options.HasPagination);
    }

    [Fact]
    public void EntityGenerationOptions_HasSearch_ReturnsTrueWhenPropertiesSet()
    {
        // Arrange
        var options = new EntityGenerationOptions
        {
            SearchableProperties = new List<string> { "name", "description" }
        };

        // Act & Assert
        Assert.True(options.HasSearch);
    }

    [Fact]
    public void EntityGenerationOptions_HasAdvancedFeatures_ReturnsTrueWhenAnySet()
    {
        // Arrange
        var options = new EntityGenerationOptions
        {
            PageSize = 20,
            SearchableProperties = new List<string> { "name" }
        };

        // Act & Assert
        Assert.True(options.HasAdvancedFeatures);
    }
}

