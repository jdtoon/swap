using SwapShop.Services;
using SwapShop.Models;
using Xunit;

namespace SwapShop.UnitTests;

/// <summary>
/// Unit tests for ProductService
/// </summary>
public class ProductServiceTests
{
    [Fact]
    public void GetAll_ReturnsAllProducts()
    {
        // Arrange
        var service = new ProductService();

        // Act
        var products = service.GetAll();

        // Assert
        Assert.NotNull(products);
        Assert.NotEmpty(products);
    }

    [Fact]
    public void GetById_ReturnsCorrectProduct()
    {
        // Arrange
        var service = new ProductService();

        // Act
        var product = service.GetById(1);

        // Assert
        Assert.NotNull(product);
        Assert.Equal(1, product.Id);
    }

    [Fact]
    public void GetById_InvalidId_ReturnsNull()
    {
        // Arrange
        var service = new ProductService();

        // Act
        var product = service.GetById(99999);

        // Assert
        Assert.Null(product);
    }

    [Fact]
    public void Search_ByName_ReturnsMatchingProducts()
    {
        // Arrange
        var service = new ProductService();

        // Act
        var products = service.Search("Keyboard");

        // Assert
        Assert.NotNull(products);
        Assert.Contains(products, p => p.Name.Contains("Keyboard", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Search_EmptyQuery_ReturnsAllProducts()
    {
        // Arrange
        var service = new ProductService();

        // Act
        var products = service.Search(null);

        // Assert
        Assert.NotNull(products);
        Assert.NotEmpty(products);
    }
}
