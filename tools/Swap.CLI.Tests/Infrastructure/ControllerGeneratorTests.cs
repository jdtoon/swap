using Swap.CLI.Infrastructure;
using Swap.CLI.Models;
using Xunit;

namespace Swap.CLI.Tests.Infrastructure;

public class ControllerGeneratorTests
{
    [Fact]
    public void GenerateController_SimpleEntity_ReturnsValidCode()
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
        var code = ControllerGenerator.GenerateController(options);

        // Assert
        Assert.Contains("namespace Controllers;", code);
        Assert.Contains("using Microsoft.AspNetCore.Mvc;", code);
        Assert.Contains("using Swap.AspNetCore.Mvc.Htmx;", code);
        Assert.Contains("public class ProductController : Controller", code);
        Assert.Contains("private readonly IProductService _service;", code);
        Assert.Contains("public ProductController(IProductService service)", code);
    }

    [Fact]
    public void GenerateController_IndexAction_ReturnsViewResult()
    {
        // Arrange
        var options = new EntityGenerationOptions
        {
            EntityName = "Product",
            Properties = new List<PropertyDefinition>()
        };

        // Act
        var code = ControllerGenerator.GenerateController(options);

        // Assert
        Assert.Contains("[HttpGet]", code);
        Assert.Contains("public IActionResult Index()", code);
        Assert.Contains("return View();", code);
    }

    [Fact]
    public void GenerateController_SimpleList_ReturnsPartialView()
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
        var code = ControllerGenerator.GenerateController(options);

        // Assert
        Assert.Contains("public async Task<IActionResult> List()", code);
        Assert.Contains("var items = await _service.GetAllAsync();", code);
        Assert.Contains("return PartialView(\"_List\", items);", code);
    }

    [Fact]
    public void GenerateController_WithPagination_IncludesPageParameters()
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
        var code = ControllerGenerator.GenerateController(options);

        // Assert
        Assert.Contains("public async Task<IActionResult> List(", code);
        Assert.Contains("int pageNumber = 1,", code);
        Assert.Contains("int pageSize = 20", code);
        Assert.Contains("var result = await _service.GetAllAsync(filter, pageNumber, pageSize, sortBy, sortDescending);", code);
        Assert.Contains("return PartialView(\"_List\", result);", code);
    }

    [Fact]
    public void GenerateController_WithSearch_IncludesSearchParameter()
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
        var code = ControllerGenerator.GenerateController(options);

        // Assert
        Assert.Contains("string? searchQuery = null,", code);
        Assert.Contains("var filter = new ProductFilterDto", code);
        Assert.Contains("SearchQuery = searchQuery,", code);
    }

    [Fact]
    public void GenerateController_WithFilters_IncludesFilterParameters()
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
        var code = ControllerGenerator.GenerateController(options);

        // Assert
        Assert.Contains("Guid? categoryId = null,", code);
        Assert.Contains("bool? isActive = null,", code);
        Assert.Contains("CategoryId = categoryId,", code);
        Assert.Contains("IsActive = isActive,", code);
    }

    [Fact]
    public void GenerateController_WithRangeFilter_IncludesMinMaxParameters()
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
        var code = ControllerGenerator.GenerateController(options);

        // Assert
        Assert.Contains("decimal? priceMin = null,", code);
        Assert.Contains("decimal? priceMax = null,", code);
        Assert.Contains("PriceMin = priceMin,", code);
        Assert.Contains("PriceMax = priceMax,", code);
    }

    [Fact]
    public void GenerateController_WithSorting_IncludesSortParameters()
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
        var code = ControllerGenerator.GenerateController(options);

        // Assert
        Assert.Contains("string? sortBy = null,", code);
        Assert.Contains("bool sortDescending = false)", code);
    }

    [Fact]
    public void GenerateController_CreateGet_ReturnsFormPartial()
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
        var code = ControllerGenerator.GenerateController(options);

        // Assert
        Assert.Contains("public IActionResult Create()", code);
        Assert.Contains("var dto = new CreateProductDto();", code);
        Assert.Contains("return PartialView(\"_Form\", dto);", code);
    }

    [Fact]
    public void GenerateController_CreatePost_ValidatesAndTriggersEvent()
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
        var code = ControllerGenerator.GenerateController(options);

        // Assert
        Assert.Contains("public async Task<IActionResult> Create(CreateProductDto dto)", code);
        Assert.Contains("if (!ModelState.IsValid)", code);
        Assert.Contains("return PartialView(\"_Form\", dto);", code);
        Assert.Contains("await _service.CreateAsync(dto);", code);
        Assert.Contains("this.HxTrigger(Events.Product.Created);", code);
        Assert.Contains("return Ok();", code);
    }

    [Fact]
    public void GenerateController_EditGet_LoadsEntityAndReturnsForm()
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
        var code = ControllerGenerator.GenerateController(options);

        // Assert
        Assert.Contains("public async Task<IActionResult> Edit(Guid id)", code);
        Assert.Contains("var entity = await _service.GetByIdAsync(id);", code);
        Assert.Contains("if (entity == null)", code);
        Assert.Contains("return NotFound();", code);
        Assert.Contains("var dto = new UpdateProductDto", code);
        Assert.Contains("Id = entity.Id,", code);
        Assert.Contains("Name = entity.Name,", code);
        Assert.Contains("Price = entity.Price,", code);
        Assert.Contains("return PartialView(\"_Form\", dto);", code);
    }

    [Fact]
    public void GenerateController_EditPost_ValidatesAndTriggersEvent()
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
        var code = ControllerGenerator.GenerateController(options);

        // Assert
        Assert.Contains("public async Task<IActionResult> Edit(UpdateProductDto dto)", code);
        Assert.Contains("if (!ModelState.IsValid)", code);
        Assert.Contains("return PartialView(\"_Form\", dto);", code);
        Assert.Contains("await _service.UpdateAsync(dto);", code);
        Assert.Contains("this.HxTrigger(Events.Product.Updated);", code);
        Assert.Contains("return Ok();", code);
    }

    [Fact]
    public void GenerateController_Delete_TriggersEventAndSwapsOut()
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
        var code = ControllerGenerator.GenerateController(options);

        // Assert
        Assert.Contains("[HttpDelete]", code);
        Assert.Contains("public async Task<IActionResult> Delete(Guid id)", code);
        Assert.Contains("await _service.DeleteAsync(id);", code);
        Assert.Contains("this.HxTrigger(Events.Product.Deleted);", code);
        Assert.Contains("this.HxReswap(HtmxSwap.Delete);", code);
        Assert.Contains("return Ok();", code);
    }

    [Fact]
    public void GenerateController_WithModule_UsesModuleNamespaces()
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
        var code = ControllerGenerator.GenerateController(options);

        // Assert
        Assert.Contains("namespace Catalog.Web.Controllers;", code);
        Assert.Contains("using Catalog.Contracts.Dtos;", code);
        Assert.Contains("using Catalog.Contracts.Services;", code);
    }

    [Fact]
    public void GenerateController_EditGet_MapsAllProperties()
    {
        // Arrange
        var options = new EntityGenerationOptions
        {
            EntityName = "Product",
            Properties = new List<PropertyDefinition>
            {
                PropertyParser.Parse("name:string:256:required"),
                PropertyParser.Parse("description:string"),
                PropertyParser.Parse("price:decimal:18:2:required"),
                PropertyParser.Parse("categoryId:guid:fk:Category")
            }
        };

        // Act
        var code = ControllerGenerator.GenerateController(options);

        // Assert
        Assert.Contains("Name = entity.Name,", code);
        Assert.Contains("Description = entity.Description,", code);
        Assert.Contains("Price = entity.Price,", code);
        Assert.Contains("CategoryId = entity.CategoryId,", code);
    }
}

