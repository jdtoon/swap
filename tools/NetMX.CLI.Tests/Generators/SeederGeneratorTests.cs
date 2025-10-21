using NetMX.CLI.Infrastructure;
using NetMX.CLI.Models;
using Xunit;

namespace NetMX.CLI.Tests.Generators;

/// <summary>
/// Tests for SeederGenerator
/// </summary>
public class SeederGeneratorTests
{
    [Fact]
    public void Generate_CreatesSeederClass()
    {
        // Arrange
        var options = new SeederGenerationOptions
        {
            SeederName = "ProductSeeder",
            EntityName = "Product",
            Namespace = "MyApp.Web.Seeding"
        };

        // Act
        var result = SeederGenerator.Generate(options);

        // Assert
        Assert.Contains("public class ProductSeeder : ISeeder", result);
    }

    [Fact]
    public void Generate_IncludesRepository()
    {
        // Arrange
        var options = new SeederGenerationOptions
        {
            SeederName = "ProductSeeder",
            EntityName = "Product",
            Namespace = "MyApp.Web.Seeding"
        };

        // Act
        var result = SeederGenerator.Generate(options);

        // Assert
        Assert.Contains("IQueryableRepository<Product, Guid>", result);
    }

    [Fact]
    public void Generate_IncludesSeedAsyncMethod()
    {
        // Arrange
        var options = new SeederGenerationOptions
        {
            SeederName = "ProductSeeder",
            EntityName = "Product",
            Namespace = "MyApp.Web.Seeding"
        };

        // Act
        var result = SeederGenerator.Generate(options);

        // Assert
        Assert.Contains("public async Task SeedAsync()", result);
    }

    [Fact]
    public void Generate_ChecksIfAlreadySeeded()
    {
        // Arrange
        var options = new SeederGenerationOptions
        {
            SeederName = "ProductSeeder",
            EntityName = "Product",
            Namespace = "MyApp.Web.Seeding"
        };

        // Act
        var result = SeederGenerator.Generate(options);

        // Assert
        Assert.Contains("if (await _repository.GetCountAsync() > 0)", result);
        Assert.Contains("return;", result);
    }

    [Fact]
    public void Generate_IncludesSampleData()
    {
        // Arrange
        var options = new SeederGenerationOptions
        {
            SeederName = "ProductSeeder",
            EntityName = "Product",
            Namespace = "MyApp.Web.Seeding"
        };

        // Act
        var result = SeederGenerator.Generate(options);

        // Assert
        Assert.Contains("var items = new[]", result);
        Assert.Contains("new Product(Guid.NewGuid(), \"Sample Product 1\")", result);
    }

    [Fact]
    public void Generate_IncludesInsertLoop()
    {
        // Arrange
        var options = new SeederGenerationOptions
        {
            SeederName = "ProductSeeder",
            EntityName = "Product",
            Namespace = "MyApp.Web.Seeding"
        };

        // Act
        var result = SeederGenerator.Generate(options);

        // Assert
        Assert.Contains("foreach (var item in items)", result);
        Assert.Contains("await _repository.InsertAsync(item);", result);
    }

    [Fact]
    public void Generate_UsesCorrectNamespace()
    {
        // Arrange
        var options = new SeederGenerationOptions
        {
            SeederName = "ProductSeeder",
            EntityName = "Product",
            Namespace = "MyApp.Web.Seeding"
        };

        // Act
        var result = SeederGenerator.Generate(options);

        // Assert
        Assert.Contains("namespace MyApp.Web.Seeding;", result);
    }

    [Fact]
    public void Generate_HandlesModuleContext()
    {
        // Arrange
        var options = new SeederGenerationOptions
        {
            SeederName = "PermissionSeeder",
            EntityName = "Permission",
            Namespace = "Authorization.Application.Seeding",
            ModuleName = "Authorization"
        };

        // Act
        var result = SeederGenerator.Generate(options);

        // Assert
        Assert.Contains("namespace Authorization.Application.Seeding;", result);
        Assert.Contains("public class PermissionSeeder : ISeeder", result);
    }

    [Fact]
    public void Generate_IncludesConstructor()
    {
        // Arrange
        var options = new SeederGenerationOptions
        {
            SeederName = "ProductSeeder",
            EntityName = "Product",
            Namespace = "MyApp.Web.Seeding"
        };

        // Act
        var result = SeederGenerator.Generate(options);

        // Assert
        Assert.Contains("public ProductSeeder(IQueryableRepository<Product, Guid> repository)", result);
        Assert.Contains("_repository = repository;", result);
    }

    [Fact]
    public void Generate_IncludesXmlDocumentation()
    {
        // Arrange
        var options = new SeederGenerationOptions
        {
            SeederName = "ProductSeeder",
            EntityName = "Product",
            Namespace = "MyApp.Web.Seeding"
        };

        // Act
        var result = SeederGenerator.Generate(options);

        // Assert
        Assert.Contains("/// <summary>", result);
        Assert.Contains("/// Seeder for Product entity", result);
    }

    [Fact]
    public void Generate_IncludesRepositoryUsing()
    {
        // Arrange
        var options = new SeederGenerationOptions
        {
            SeederName = "ProductSeeder",
            EntityName = "Product",
            Namespace = "MyApp.Web.Seeding"
        };

        // Act
        var result = SeederGenerator.Generate(options);

        // Assert
        Assert.Contains("using NetMX.Ddd.Domain.Repositories;", result);
    }
}
