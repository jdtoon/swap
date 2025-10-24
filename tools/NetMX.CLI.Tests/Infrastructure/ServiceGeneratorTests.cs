using NetMX.CLI.Infrastructure;
using NetMX.CLI.Models;
using Xunit;

namespace NetMX.CLI.Tests.Infrastructure;

public class ServiceGeneratorTests
{
    [Fact]
    public void GenerateServiceInterface_SimpleEntity_ReturnsValidCode()
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
        var code = ServiceGenerator.GenerateServiceInterface(options);

        // Assert
        Assert.Contains("namespace Services;", code);
        Assert.Contains("public interface IProductService", code);
        Assert.Contains("Task<List<ProductDto>> GetAllAsync();", code);
        Assert.Contains("Task<ProductDto?> GetByIdAsync(Guid id);", code);
        Assert.Contains("Task<ProductDto> CreateAsync(CreateProductDto dto);", code);
        Assert.Contains("Task<ProductDto> UpdateAsync(UpdateProductDto dto);", code);
        Assert.Contains("Task DeleteAsync(Guid id);", code);
    }

    [Fact]
    public void GenerateServiceInterface_WithPagination_ReturnsPagedMethod()
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
        var code = ServiceGenerator.GenerateServiceInterface(options);

        // Assert
        Assert.Contains("Task<PagedProductResultDto> GetAllAsync(", code);
        Assert.Contains("ProductFilterDto filter,", code);
        Assert.Contains("int pageNumber = 1,", code);
        Assert.Contains("int pageSize = 20,", code);
        Assert.Contains("string? sortBy = null,", code);
        Assert.Contains("bool sortDescending = false", code);
    }

    [Fact]
    public void GenerateServiceInterface_WithModule_UsesModuleNamespace()
    {
        // Arrange
        var options = new EntityGenerationOptions
        {
            EntityName = "Product",
            ModuleName = "Catalog",
            Properties = new List<PropertyDefinition>()
        };

        // Act
        var code = ServiceGenerator.GenerateServiceInterface(options);

        // Assert
        Assert.Contains("namespace Catalog.Contracts.Services;", code);
        Assert.Contains("using Catalog.Contracts.Dtos;", code);
    }

    [Fact]
    public void GenerateServiceImplementation_SimpleEntity_ReturnsValidCode()
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
        var code = ServiceGenerator.GenerateServiceImplementation(options);

        // Assert
        Assert.Contains("namespace Services;", code);
        Assert.Contains("public class ProductService : IProductService", code);
        Assert.Contains("private readonly AppDbContext _context;", code);
        Assert.Contains("public ProductService(AppDbContext context)", code);
        Assert.Contains("_context = context;", code);
    }

    [Fact]
    public void GenerateServiceImplementation_SimpleGetAll_ReturnsListMethod()
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
        var code = ServiceGenerator.GenerateServiceImplementation(options);

        // Assert
        Assert.Contains("public async Task<List<ProductDto>> GetAllAsync()", code);
        Assert.Contains("_context.Set<Product>()", code);
        Assert.Contains("Select(x => new ProductDto", code);
        Assert.Contains("Id = x.Id,", code);
        Assert.Contains("Name = x.Name,", code);
        Assert.Contains("Price = x.Price", code);
        Assert.Contains(".ToListAsync();", code);
    }

    [Fact]
    public void GenerateServiceImplementation_WithSearch_AppliesSearchFilter()
    {
        // Arrange
        var options = new EntityGenerationOptions
        {
            EntityName = "Product",
            Properties = new List<PropertyDefinition>
            {
                PropertyParser.Parse("name:string:256:required"),
                PropertyParser.Parse("description:string")
            },
            PageSize = 20,
            SearchableProperties = new List<string> { "name", "description" }
        };

        // Act
        var code = ServiceGenerator.GenerateServiceImplementation(options);

        // Assert
        Assert.Contains("// Apply search", code);
        Assert.Contains("if (!string.IsNullOrWhiteSpace(filter.SearchQuery))", code);
        Assert.Contains("var search = filter.SearchQuery.ToLower();", code);
        Assert.Contains("x.Name.ToLower().Contains(search)", code);
        Assert.Contains("x.Description.ToLower().Contains(search)", code);
    }

    [Fact]
    public void GenerateServiceImplementation_WithFilters_AppliesFilterLogic()
    {
        // Arrange
        var options = new EntityGenerationOptions
        {
            EntityName = "Product",
            Properties = new List<PropertyDefinition>
            {
                PropertyParser.Parse("name:string:256:required"),
                PropertyParser.Parse("categoryId:guid:fk:Category"),
                PropertyParser.Parse("isActive:bool")
            },
            PageSize = 20,
            FilterableProperties = new List<string> { "categoryId", "isActive" }
        };

        // Act
        var code = ServiceGenerator.GenerateServiceImplementation(options);

        // Assert
        Assert.Contains("// Apply filters", code);
        Assert.Contains("if (filter.CategoryId.HasValue)", code);
        Assert.Contains("query = query.Where(x => x.CategoryId == filter.CategoryId.Value);", code);
        Assert.Contains("if (filter.IsActive.HasValue)", code);
        Assert.Contains("query = query.Where(x => x.IsActive == filter.IsActive.Value);", code);
    }

    [Fact]
    public void GenerateServiceImplementation_WithRangeFilter_AppliesMinMaxLogic()
    {
        // Arrange
        var options = new EntityGenerationOptions
        {
            EntityName = "Product",
            Properties = new List<PropertyDefinition>
            {
                PropertyParser.Parse("name:string:256:required"),
                PropertyParser.Parse("price:decimal:18:2:required")
            },
            PageSize = 20,
            FilterableProperties = new List<string> { "priceRange" }
        };

        // Act
        var code = ServiceGenerator.GenerateServiceImplementation(options);

        // Assert
        Assert.Contains("if (filter.PriceMin.HasValue)", code);
        Assert.Contains("query = query.Where(x => x.Price >= filter.PriceMin.Value);", code);
        Assert.Contains("if (filter.PriceMax.HasValue)", code);
        Assert.Contains("query = query.Where(x => x.Price <= filter.PriceMax.Value);", code);
    }

    [Fact]
    public void GenerateServiceImplementation_WithSorting_AppliesSortLogic()
    {
        // Arrange
        var options = new EntityGenerationOptions
        {
            EntityName = "Product",
            Properties = new List<PropertyDefinition>
            {
                PropertyParser.Parse("name:string:256:required"),
                PropertyParser.Parse("price:decimal:18:2:required")
            },
            PageSize = 20,
            SortableProperties = new List<string> { "name", "price" }
        };

        // Act
        var code = ServiceGenerator.GenerateServiceImplementation(options);

        // Assert
        Assert.Contains("// Apply sorting", code);
        Assert.Contains("if (!string.IsNullOrWhiteSpace(sortBy))", code);
        Assert.Contains("query = sortBy.ToLower() switch", code);
        Assert.Contains("\"name\" => sortDescending", code);
        Assert.Contains("? query.OrderByDescending(x => x.Name)", code);
        Assert.Contains(": query.OrderBy(x => x.Name),", code);
        Assert.Contains("\"price\" => sortDescending", code);
        Assert.Contains("? query.OrderByDescending(x => x.Price)", code);
        Assert.Contains(": query.OrderBy(x => x.Price),", code);
    }

    [Fact]
    public void GenerateServiceImplementation_WithPagination_AppliesPaginationLogic()
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
        var code = ServiceGenerator.GenerateServiceImplementation(options);

        // Assert
        Assert.Contains("var totalCount = await query.CountAsync();", code);
        Assert.Contains("// Apply pagination", code);
        Assert.Contains(".Skip((pageNumber - 1) * pageSize)", code);
        Assert.Contains(".Take(pageSize)", code);
        Assert.Contains("return new PagedProductResultDto", code);
        Assert.Contains("Items = items,", code);
        Assert.Contains("TotalCount = totalCount,", code);
        Assert.Contains("PageNumber = pageNumber,", code);
        Assert.Contains("PageSize = pageSize", code);
    }

    [Fact]
    public void GenerateServiceImplementation_GetById_ReturnsNullableDto()
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
        var code = ServiceGenerator.GenerateServiceImplementation(options);

        // Assert
        Assert.Contains("public async Task<ProductDto?> GetByIdAsync(Guid id)", code);
        Assert.Contains("var entity = await _context.Set<Product>()", code);
        Assert.Contains(".FirstOrDefaultAsync(x => x.Id == id);", code);
        Assert.Contains("if (entity == null)", code);
        Assert.Contains("return null;", code);
        Assert.Contains("return new ProductDto", code);
    }

    [Fact]
    public void GenerateServiceImplementation_Create_UsesConstructorForRequiredProps()
    {
        // Arrange
        var options = new EntityGenerationOptions
        {
            EntityName = "Product",
            Properties = new List<PropertyDefinition>
            {
                PropertyParser.Parse("name:string:256:required"),
                PropertyParser.Parse("price:decimal:18:2:required"),
                PropertyParser.Parse("description:string")
            }
        };

        // Act
        var code = ServiceGenerator.GenerateServiceImplementation(options);

        // Assert
        Assert.Contains("public async Task<ProductDto> CreateAsync(CreateProductDto dto)", code);
        Assert.Contains("var entity = new Product(Guid.NewGuid(), dto.Name, dto.Price);", code);
        Assert.Contains("// Set optional properties", code);
        Assert.Contains("entity.SetDescription(dto.Description);", code);
        Assert.Contains("_context.Set<Product>().Add(entity);", code);
        Assert.Contains("await _context.SaveChangesAsync();", code);
    }

    [Fact]
    public void GenerateServiceImplementation_Update_UsesSetMethods()
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
        var code = ServiceGenerator.GenerateServiceImplementation(options);

        // Assert
        Assert.Contains("public async Task<ProductDto> UpdateAsync(UpdateProductDto dto)", code);
        Assert.Contains("var entity = await _context.Set<Product>()", code);
        Assert.Contains(".FirstOrDefaultAsync(x => x.Id == dto.Id);", code);
        Assert.Contains("if (entity == null)", code);
        Assert.Contains("throw new InvalidOperationException(\"Product not found\");", code);
        Assert.Contains("entity.SetName(dto.Name);", code);
        Assert.Contains("entity.SetPrice(dto.Price);", code);
        Assert.Contains("await _context.SaveChangesAsync();", code);
    }

    [Fact]
    public void GenerateServiceImplementation_Delete_WithSoftDelete_CallsDeleteMethod()
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
        var code = ServiceGenerator.GenerateServiceImplementation(options);

        // Assert
        Assert.Contains("public async Task DeleteAsync(Guid id)", code);
        Assert.Contains("// Soft delete", code);
        Assert.Contains("entity.Delete();", code);
        Assert.DoesNotContain("_context.Set<Product>().Remove(entity);", code);
    }

    [Fact]
    public void GenerateServiceImplementation_Delete_WithoutSoftDelete_RemovesEntity()
    {
        // Arrange
        var options = new EntityGenerationOptions
        {
            EntityName = "Product",
            Properties = new List<PropertyDefinition>
            {
                PropertyParser.Parse("name:string:256:required")
            },
            IncludeSoftDelete = false
        };

        // Act
        var code = ServiceGenerator.GenerateServiceImplementation(options);

        // Assert
        Assert.Contains("public async Task DeleteAsync(Guid id)", code);
        Assert.Contains("// Hard delete", code);
        Assert.Contains("_context.Set<Product>().Remove(entity);", code);
        Assert.DoesNotContain("entity.Delete();", code);
    }

    [Fact]
    public void GenerateServiceImplementation_WithAuditFields_IncludesTimestamps()
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
        var code = ServiceGenerator.GenerateServiceImplementation(options);

        // Assert
        Assert.Contains("CreatedAt = x.CreatedAt,", code);
        Assert.Contains("UpdatedAt = x.UpdatedAt", code);
    }

    [Fact]
    public void GenerateServiceImplementation_WithModule_UsesModuleNamespaces()
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
        var code = ServiceGenerator.GenerateServiceImplementation(options);

        // Assert
        Assert.Contains("namespace Catalog.Application.Services;", code);
        Assert.Contains("using Catalog.Contracts.Dtos;", code);
        Assert.Contains("using Catalog.Contracts.Services;", code);
        Assert.Contains("using Catalog.Core.Entities;", code);
    }
}
