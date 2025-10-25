using Swap.CLI.Infrastructure;
using Xunit;

namespace Swap.CLI.Tests.Infrastructure;

public class ControllerTemplateTests
{
    [Fact]
    public void ControllerTemplate_ShouldReplaceEntityName()
    {
        // Arrange
        var template = @"public class {{EntityName}}Controller : Controller
{
    private readonly AppDbContext _context;

    public {{EntityName}}Controller(AppDbContext context)
    {
        _context = context;
    }
}";
        var variables = new Dictionary<string, string>
        {
            { "EntityName", "Product" },
            { "EntityNamePlural", "Products" },
            { "ProjectName", "TestApp" }
        };

        // Act
        var result = TemplateEngine.Process(template, variables);

        // Assert
        Assert.Contains("public class ProductController", result);
        Assert.Contains("public ProductController(AppDbContext context)", result);
        Assert.DoesNotContain("{{EntityName}}", result);
    }

    [Fact]
    public void ControllerTemplate_ShouldReplacePluralEntityName()
    {
        // Arrange
        var template = @"public IActionResult Index()
{
    return View(_context.{{EntityNamePlural}}.ToList());
}";
        var variables = new Dictionary<string, string>
        {
            { "EntityName", "Product" },
            { "EntityNamePlural", "Products" },
            { "ProjectName", "TestApp" }
        };

        // Act
        var result = TemplateEngine.Process(template, variables);

        // Assert
        Assert.Contains("_context.Products.ToList()", result);
    }

    [Fact]
    public void ControllerTemplate_ShouldUseFullyQualifiedTypeName()
    {
        // Arrange
        var template = @"var item = new Models.{{EntityName}}
{
    Title = title,
    IsComplete = false
};";
        var variables = new Dictionary<string, string>
        {
            { "EntityName", "Task" },
            { "EntityNamePlural", "Tasks" },
            { "ProjectName", "TestApp" }
        };

        // Act
        var result = TemplateEngine.Process(template, variables);

        // Assert
        Assert.Contains("var item = new Models.Task", result);
        Assert.DoesNotContain("new Task", result); // Should not have unqualified Task
    }

    [Fact]
    public void ModelTemplate_ShouldReplaceNamespaceAndEntityName()
    {
        // Arrange
        var template = @"namespace {{ProjectName}}.Models;

public class {{EntityName}}
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public bool IsComplete { get; set; }
}";
        var variables = new Dictionary<string, string>
        {
            { "EntityName", "Product" },
            { "ProjectName", "TestApp" }
        };

        // Act
        var result = TemplateEngine.Process(template, variables);

        // Assert
        Assert.Contains("namespace TestApp.Models;", result);
        Assert.Contains("public class Product", result);
    }

    [Fact]
    public void ViewTemplate_ShouldReplaceModelAndEntityNames()
    {
        // Arrange
        var template = @"@model List<{{ProjectName}}.Models.{{EntityName}}>

@{
    ViewData[""Title""] = ""{{EntityNamePlural}}"";
}

<h1>{{EntityNamePlural}}</h1>
<div id=""{{EntityNameLower}}-list"">
";
        var variables = new Dictionary<string, string>
        {
            { "EntityName", "Product" },
            { "EntityNamePlural", "Products" },
            { "EntityNameLower", "product" },
            { "ProjectName", "TestApp" }
        };

        // Act
        var result = TemplateEngine.Process(template, variables);

        // Assert
        Assert.Contains("@model List<TestApp.Models.Product>", result);
        Assert.Contains("ViewData[\"Title\"] = \"Products\";", result);
        Assert.Contains("<h1>Products</h1>", result);
        Assert.Contains("id=\"product-list\"", result);
    }

    [Fact]
    public void ViewTemplate_ShouldReplaceHtmxTargets()
    {
        // Arrange
        var template = @"<form hx-post=""/{{EntityName}}/Create"" 
      hx-target=""#{{EntityNameLower}}-list"" 
      hx-swap=""outerHTML"">";
        var variables = new Dictionary<string, string>
        {
            { "EntityName", "Product" },
            { "EntityNameLower", "product" }
        };

        // Act
        var result = TemplateEngine.Process(template, variables);

        // Assert
        Assert.Contains("hx-post=\"/Product/Create\"", result);
        Assert.Contains("hx-target=\"#product-list\"", result);
    }

    [Fact]
    public void PartialViewTemplate_ShouldReplaceListVariables()
    {
        // Arrange
        var template = @"@model List<{{ProjectName}}.Models.{{EntityName}}>

<div id=""{{EntityNameLower}}-list"">
    @foreach (var item in Model)
    {
        <input type=""checkbox"" 
               hx-post=""/{{EntityName}}/Toggle?id=@item.Id""
               hx-target=""#{{EntityNameLower}}-list"" />
    }
</div>";
        var variables = new Dictionary<string, string>
        {
            { "EntityName", "Task" },
            { "EntityNameLower", "task" },
            { "ProjectName", "MyApp" }
        };

        // Act
        var result = TemplateEngine.Process(template, variables);

        // Assert
        Assert.Contains("@model List<MyApp.Models.Task>", result);
        Assert.Contains("id=\"task-list\"", result);
        Assert.Contains("hx-post=\"/Task/Toggle?id=@item.Id\"", result);
        Assert.Contains("hx-target=\"#task-list\"", result);
    }

    [Theory]
    [InlineData("Product", "product")]
    [InlineData("TodoItem", "todoItem")]
    [InlineData("User", "user")]
    public void EntityNameLower_ShouldConvertToCamelCase(string pascalCase, string expected)
    {
        // Test documents the camelCase conversion logic
        var actual = char.ToLower(pascalCase[0]) + pascalCase.Substring(1);
        
        Assert.Equal(expected, actual);
    }
}
