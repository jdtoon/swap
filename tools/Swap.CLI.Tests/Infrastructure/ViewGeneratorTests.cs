using Swap.CLI.Infrastructure;
using Swap.CLI.Models;
using Xunit;

namespace Swap.CLI.Tests.Infrastructure;

public class ViewGeneratorTests
{
    [Fact]
    public void GenerateIndexView_SimpleEntity_ReturnsValidRazor()
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
        var code = ViewGenerator.GenerateIndexView(options);

        // Assert
        Assert.Contains("ViewData[\"Title\"] = \"Products\";", code);
        Assert.Contains("<h1 class=\"title\">", code);
        Assert.Contains("<i class=\"fas fa-box\"></i> Products", code);
        Assert.Contains("id=\"list-container\"", code);
        Assert.Contains("hx-get=\"/Product/List\"", code);
        Assert.Contains($"hx-trigger=\"load, @Events.Product.Created from:body, @Events.Product.Updated from:body, @Events.Product.Deleted from:body\"", code);
        Assert.Contains("id=\"modal-container\"", code);
    }

    [Fact]
    public void GenerateIndexView_WithSearch_IncludesSearchBox()
    {
        // Arrange
        var options = new EntityGenerationOptions
        {
            EntityName = "Product",
            Properties = new List<PropertyDefinition>
            {
                PropertyParser.Parse("name:string:256:required")
            },
            SearchableProperties = new List<string> { "name" }
        };

        // Act
        var code = ViewGenerator.GenerateIndexView(options);

        // Assert
        Assert.Contains("<!-- Search and Filters -->", code);
        Assert.Contains("<label class=\"label\">Search</label>", code);
        Assert.Contains("name=\"searchQuery\"", code);
        Assert.Contains("placeholder=\"Search...\"", code);
        Assert.Contains("hx-trigger=\"keyup changed delay:500ms\"", code);
        Assert.Contains("<i class=\"fas fa-search\"></i>", code);
    }

    [Fact]
    public void GenerateIndexView_WithFilters_IncludesFilterFields()
    {
        // Arrange
        var options = new EntityGenerationOptions
        {
            EntityName = "Product",
            Properties = new List<PropertyDefinition>
            {
                PropertyParser.Parse("name:string:256:required"),
                PropertyParser.Parse("categoryId:guid:fk:Category")
            },
            FilterableProperties = new List<string> { "categoryId" }
        };

        // Act
        var code = ViewGenerator.GenerateIndexView(options);

        // Assert
        Assert.Contains("<label class=\"label\">CategoryId</label>", code);
        Assert.Contains("name=\"filterCategoryId\"", code);
        Assert.Contains("hx-trigger=\"change\"", code);
    }

    [Fact]
    public void GenerateIndexView_CreateButton_OpensModal()
    {
        // Arrange
        var options = new EntityGenerationOptions
        {
            EntityName = "Product",
            Properties = new List<PropertyDefinition>()
        };

        // Act
        var code = ViewGenerator.GenerateIndexView(options);

        // Assert
        Assert.Contains("<button class=\"button is-primary\"", code);
        Assert.Contains("hx-get=\"/Product/Create\"", code);
        Assert.Contains("hx-target=\"#modal-container\"", code);
        Assert.Contains("<i class=\"fas fa-plus\"></i>", code);
        Assert.Contains("<span>New Product</span>", code);
    }

    [Fact]
    public void GenerateListView_SimpleEntity_ReturnsTable()
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
        var code = ViewGenerator.GenerateListView(options);

        // Assert
        Assert.Contains("@model List<ProductDto>", code);
        Assert.Contains("<table class=\"table is-fullwidth is-striped is-hoverable\">", code);
        Assert.Contains("<th>Name</th>", code);
        Assert.Contains("<th>Price</th>", code);
        Assert.Contains("<th class=\"has-text-right\">Actions</th>", code);
        Assert.Contains("@foreach (var item in Model)", code);
    }

    [Fact]
    public void GenerateListView_WithPagination_UsesPagedModel()
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
        var code = ViewGenerator.GenerateListView(options);

        // Assert
        Assert.Contains("@model PagedProductResultDto", code);
        Assert.Contains("@if (!Model.Items.Any())", code);
        Assert.Contains("@foreach (var item in Model.Items)", code);
        Assert.Contains("<nav class=\"pagination is-centered\"", code);
        Assert.Contains("Model.HasPreviousPage", code);
        Assert.Contains("Model.HasNextPage", code);
        Assert.Contains("Page @Model.PageNumber of @Model.TotalPages", code);
    }

    [Fact]
    public void GenerateListView_WithSorting_IncludesSortableHeaders()
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
            SortableProperties = new List<string> { "name", "price" }
        };

        // Act
        var code = ViewGenerator.GenerateListView(options);

        // Assert
        Assert.Contains("hx-get=\"/Product/List?sortBy=name\"", code);
        Assert.Contains("hx-get=\"/Product/List?sortBy=price\"", code);
        Assert.Contains("<i class=\"fas fa-sort\"></i>", code);
    }

    [Fact]
    public void GenerateListView_DecimalProperty_FormatsAsCurrency()
    {
        // Arrange
        var options = new EntityGenerationOptions
        {
            EntityName = "Product",
            Properties = new List<PropertyDefinition>
            {
                PropertyParser.Parse("price:decimal:18:2:required")
            }
        };

        // Act
        var code = ViewGenerator.GenerateListView(options);

        // Assert
        Assert.Contains("@item.Price?.ToString(\"C\")", code);
    }

    [Fact]
    public void GenerateListView_DateTimeProperty_FormatsAsGeneralDate()
    {
        // Arrange
        var options = new EntityGenerationOptions
        {
            EntityName = "Product",
            Properties = new List<PropertyDefinition>
            {
                PropertyParser.Parse("createdAt:datetime:required")
            }
        };

        // Act
        var code = ViewGenerator.GenerateListView(options);

        // Assert
        Assert.Contains("@item.CreatedAt?.ToString(\"g\")", code);
    }

    [Fact]
    public void GenerateListView_BoolProperty_ShowsIcons()
    {
        // Arrange
        var options = new EntityGenerationOptions
        {
            EntityName = "Product",
            Properties = new List<PropertyDefinition>
            {
                PropertyParser.Parse("isActive:bool:required")
            }
        };

        // Act
        var code = ViewGenerator.GenerateListView(options);

        // Assert
        Assert.Contains("@if (item.IsActive == true)", code);
        Assert.Contains("<i class=\"fas fa-check\"></i>", code);
        Assert.Contains("<i class=\"fas fa-times\"></i>", code);
    }

    [Fact]
    public void GenerateListView_Actions_IncludesEditAndDelete()
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
        var code = ViewGenerator.GenerateListView(options);

        // Assert
        Assert.Contains("hx-get=\"/Product/Edit/@item.Id\"", code);
        Assert.Contains("hx-target=\"#modal-container\"", code);
        Assert.Contains("<i class=\"fas fa-edit\"></i>", code);
        Assert.Contains("hx-delete=\"/Product/Delete/@item.Id\"", code);
        Assert.Contains("hx-target=\"#row-@item.Id\"", code);
        Assert.Contains("hx-confirm=\"Are you sure you want to delete this item?\"", code);
        Assert.Contains("<i class=\"fas fa-trash\"></i>", code);
    }

    [Fact]
    public void GenerateFormView_SimpleEntity_ReturnsModal()
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
        var code = ViewGenerator.GenerateFormView(options);

        // Assert
        Assert.Contains("@model dynamic", code);
        Assert.Contains("<div class=\"modal is-active\">", code);
        Assert.Contains("<p class=\"modal-card-title\">@title</p>", code);
        Assert.Contains("hx-post=\"/@($\"/Product/{action}\")\"", code);
        Assert.Contains("hx-target=\"#modal-container\"", code);
        Assert.Contains("hx-swap=\"outerHTML\"", code);
    }

    [Fact]
    public void GenerateFormView_StringProperty_GeneratesInput()
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
        var code = ViewGenerator.GenerateFormView(options);

        // Assert
        Assert.Contains("<label class=\"label\">Name</label>", code);
        Assert.Contains("<input class=\"input\"", code);
        Assert.Contains("type=\"text\"", code);
        Assert.Contains("name=\"Name\"", code);
        Assert.Contains("value=\"@Model.Name\"", code);
        Assert.Contains("required", code);
        Assert.Contains("maxlength=\"256\"", code);
    }

    [Fact]
    public void GenerateFormView_LongStringProperty_GeneratesTextarea()
    {
        // Arrange
        var options = new EntityGenerationOptions
        {
            EntityName = "Product",
            Properties = new List<PropertyDefinition>
            {
                PropertyParser.Parse("description:string:1000")
            }
        };

        // Act
        var code = ViewGenerator.GenerateFormView(options);

        // Assert
        Assert.Contains("<textarea class=\"textarea\"", code);
        Assert.Contains("name=\"Description\"", code);
        Assert.Contains("maxlength=\"1000\">@Model.Description</textarea>", code);
    }

    [Fact]
    public void GenerateFormView_IntProperty_GeneratesNumberInput()
    {
        // Arrange
        var options = new EntityGenerationOptions
        {
            EntityName = "Product",
            Properties = new List<PropertyDefinition>
            {
                PropertyParser.Parse("quantity:int:required")
            }
        };

        // Act
        var code = ViewGenerator.GenerateFormView(options);

        // Assert
        Assert.Contains("<label class=\"label\">Quantity</label>", code);
        Assert.Contains("type=\"number\"", code);
        Assert.Contains("name=\"Quantity\"", code);
    }

    [Fact]
    public void GenerateFormView_DecimalProperty_GeneratesNumberInputWithStep()
    {
        // Arrange
        var options = new EntityGenerationOptions
        {
            EntityName = "Product",
            Properties = new List<PropertyDefinition>
            {
                PropertyParser.Parse("price:decimal:18:2:required")
            }
        };

        // Act
        var code = ViewGenerator.GenerateFormView(options);

        // Assert
        Assert.Contains("<label class=\"label\">Price</label>", code);
        Assert.Contains("type=\"number\"", code);
        Assert.Contains("step=\"0.01\"", code);
        Assert.Contains("name=\"Price\"", code);
    }

    [Fact]
    public void GenerateFormView_BoolProperty_GeneratesCheckbox()
    {
        // Arrange
        var options = new EntityGenerationOptions
        {
            EntityName = "Product",
            Properties = new List<PropertyDefinition>
            {
                PropertyParser.Parse("isActive:bool:required")
            }
        };

        // Act
        var code = ViewGenerator.GenerateFormView(options);

        // Assert
        Assert.Contains("<label class=\"checkbox\">", code);
        Assert.Contains("type=\"checkbox\"", code);
        Assert.Contains("name=\"IsActive\"", code);
        Assert.Contains("@(Model.IsActive == true ? \"checked\" : \"\")", code);
    }

    [Fact]
    public void GenerateFormView_DateTimeProperty_GeneratesDateTimeInput()
    {
        // Arrange
        var options = new EntityGenerationOptions
        {
            EntityName = "Product",
            Properties = new List<PropertyDefinition>
            {
                PropertyParser.Parse("releaseDate:datetime:required")
            }
        };

        // Act
        var code = ViewGenerator.GenerateFormView(options);

        // Assert
        Assert.Contains("<label class=\"label\">ReleaseDate</label>", code);
        Assert.Contains("type=\"datetime-local\"", code);
        Assert.Contains("name=\"ReleaseDate\"", code);
        Assert.Contains("value=\"@Model.ReleaseDate?.ToString(\"yyyy-MM-ddTHH:mm\")\"", code);
    }

    [Fact]
    public void GenerateFormView_FormButtons_IncludesSaveAndCancel()
    {
        // Arrange
        var options = new EntityGenerationOptions
        {
            EntityName = "Product",
            Properties = new List<PropertyDefinition>()
        };

        // Act
        var code = ViewGenerator.GenerateFormView(options);

        // Assert
        Assert.Contains("<button type=\"submit\" class=\"button is-success\">Save</button>", code);
        Assert.Contains("<button type=\"button\" class=\"button\" onclick=\"this.closest('.modal').remove()\">Cancel</button>", code);
    }

    [Fact]
    public void GenerateFormView_IncludesModalCloseScript()
    {
        // Arrange
        var options = new EntityGenerationOptions
        {
            EntityName = "Product",
            Properties = new List<PropertyDefinition>()
        };

        // Act
        var code = ViewGenerator.GenerateFormView(options);

        // Assert
        Assert.Contains("<script>", code);
        Assert.Contains("document.body.addEventListener('htmx:afterSwap'", code);
        Assert.Contains("const modal = document.querySelector('.modal');", code);
        Assert.Contains("if (modal) modal.remove();", code);
    }
}

