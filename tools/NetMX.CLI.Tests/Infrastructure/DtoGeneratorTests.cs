using NetMX.CLI.Infrastructure;
using NetMX.CLI.Models;
using Xunit;

namespace NetMX.CLI.Tests.Infrastructure;

public class DtoGeneratorTests
{
    [Fact]
    public void GenerateReadDto_SimpleEntity_ReturnsValidCode()
    {
        // Arrange
        var options = new EntityGenerationOptions
        {
            EntityName = "Product",
            Properties = new List<PropertyDefinition>
            {
                PropertyParser.Parse("name:string:256:required"),
                PropertyParser.Parse("price:decimal:18:2:required")
            }
        };

        // Act
        var code = DtoGenerator.GenerateReadDto(options);

        // Assert
        Assert.Contains("public class ProductDto", code);
        Assert.Contains("public Guid Id { get; set; }", code);
        Assert.Contains("public string Name { get; set; }", code);
        Assert.Contains("public decimal Price { get; set; }", code);
    }

    [Fact]
    public void GenerateReadDto_WithAuditFields_IncludesTimestamps()
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
        var code = DtoGenerator.GenerateReadDto(options);

        // Assert
        Assert.Contains("public DateTime CreatedAt { get; set; }", code);
        Assert.Contains("public DateTime? UpdatedAt { get; set; }", code);
    }

    [Fact]
    public void GenerateCreateDto_WithValidation_IncludesAttributes()
    {
        // Arrange
        var options = new EntityGenerationOptions
        {
            EntityName = "Product",
            Properties = new List<PropertyDefinition>
            {
                PropertyParser.Parse("name:string:256:required"),
                PropertyParser.Parse("price:decimal:18:2:required:min:0:max:10000")
            }
        };

        // Act
        var code = DtoGenerator.GenerateCreateDto(options);

        // Assert
        Assert.Contains("public class CreateProductDto", code);
        Assert.Contains("[Required]", code);
        Assert.Contains("[MaxLength(256)]", code);
        Assert.Contains("[Range(0, 10000)]", code);
    }

    [Fact]
    public void GenerateUpdateDto_IncludesIdProperty()
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
        var code = DtoGenerator.GenerateUpdateDto(options);

        // Assert
        Assert.Contains("public class UpdateProductDto", code);
        Assert.Contains("public Guid Id { get; set; }", code);
        Assert.Contains("public string Name { get; set; }", code);
    }

    [Fact]
    public void GenerateFilterDto_WithSearch_IncludesSearchQuery()
    {
        // Arrange
        var options = new EntityGenerationOptions
        {
            EntityName = "Product",
            Properties = new List<PropertyDefinition>
            {
                PropertyParser.Parse("name:string:256:required"),
                PropertyParser.Parse("description:text")
            },
            SearchableProperties = new List<string> { "name", "description" }
        };

        // Act
        var code = DtoGenerator.GenerateFilterDto(options);

        // Assert
        Assert.Contains("public class ProductFilterDto", code);
        Assert.Contains("public string? SearchQuery { get; set; }", code);
        Assert.Contains("searches: name, description", code);
    }

    [Fact]
    public void GenerateFilterDto_WithFilters_IncludesFilterProperties()
    {
        // Arrange
        var options = new EntityGenerationOptions
        {
            EntityName = "Product",
            Properties = new List<PropertyDefinition>
            {
                PropertyParser.Parse("name:string:256:required"),
                PropertyParser.Parse("categoryId:guid:fk:Category:required"),
                PropertyParser.Parse("isActive:bool:default:true")
            },
            FilterableProperties = new List<string> { "categoryId", "isActive" }
        };

        // Act
        var code = DtoGenerator.GenerateFilterDto(options);

        // Assert
        Assert.Contains("public Guid? CategoryId { get; set; }", code);
        Assert.Contains("public bool? IsActive { get; set; }", code);
    }

    [Fact]
    public void GenerateFilterDto_WithRangeFilter_IncludesMinMax()
    {
        // Arrange
        var options = new EntityGenerationOptions
        {
            EntityName = "Product",
            Properties = new List<PropertyDefinition>
            {
                PropertyParser.Parse("price:decimal:18:2:required")
            },
            FilterableProperties = new List<string> { "priceRange" }
        };

        // Act
        var code = DtoGenerator.GenerateFilterDto(options);

        // Assert
        Assert.Contains("public decimal? PriceMin { get; set; }", code);
        Assert.Contains("public decimal? PriceMax { get; set; }", code);
    }

    [Fact]
    public void GenerateFilterDto_NoFiltersOrSearch_ReturnsEmpty()
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
        var code = DtoGenerator.GenerateFilterDto(options);

        // Assert
        Assert.Equal(string.Empty, code);
    }

    [Fact]
    public void GeneratePagedResultDto_WithPagination_ReturnsValidCode()
    {
        // Arrange
        var options = new EntityGenerationOptions
        {
            EntityName = "Product",
            Properties = new List<PropertyDefinition>
            {
                PropertyParser.Parse("name:string:256:required")
            },
            PageSize = 20
        };

        // Act
        var code = DtoGenerator.GeneratePagedResultDto(options);

        // Assert
        Assert.Contains("public class PagedProductResultDto", code);
        Assert.Contains("public List<ProductDto> Items { get; set; } = new();", code);
        Assert.Contains("public int TotalCount { get; set; }", code);
        Assert.Contains("public int PageNumber { get; set; }", code);
        Assert.Contains("public int PageSize { get; set; }", code);
        Assert.Contains("public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);", code);
        Assert.Contains("public bool HasPreviousPage => PageNumber > 1;", code);
        Assert.Contains("public bool HasNextPage => PageNumber < TotalPages;", code);
    }

    [Fact]
    public void GeneratePagedResultDto_NoPagination_ReturnsEmpty()
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
        var code = DtoGenerator.GeneratePagedResultDto(options);

        // Assert
        Assert.Equal(string.Empty, code);
    }

    [Fact]
    public void GenerateReadDto_WithModule_UsesModuleNamespace()
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
        var code = DtoGenerator.GenerateReadDto(options);

        // Assert
        Assert.Contains("namespace Catalog.Contracts.Dtos;", code);
    }

    [Fact]
    public void GenerateReadDto_WithoutModule_UsesDtosNamespace()
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
        var code = DtoGenerator.GenerateReadDto(options);

        // Assert
        Assert.Contains("namespace Dtos;", code);
    }
}
