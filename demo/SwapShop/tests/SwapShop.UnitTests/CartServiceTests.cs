using SwapShop.Services;
using SwapShop.Models;
using Xunit;

namespace SwapShop.UnitTests;

/// <summary>
/// Unit tests for CartService
/// </summary>
public class CartServiceTests
{
    [Fact]
    public void GetCart_NewSession_ReturnsEmptyCart()
    {
        // Arrange
        var service = new CartService(new ProductService());
        var sessionId = Guid.NewGuid().ToString();

        // Act
        var cart = service.GetCart(sessionId);

        // Assert
        Assert.NotNull(cart);
        Assert.Empty(cart.Items);
        Assert.Equal(0, cart.TotalPrice);
    }

    [Fact]
    public void AddItem_ValidProduct_AddsToCart()
    {
        // Arrange
        var service = new CartService(new ProductService());
        var sessionId = Guid.NewGuid().ToString();

        // Act
        var result = service.AddItem(sessionId, 1, 1);

        // Assert
        Assert.True(result);
        var cart = service.GetCart(sessionId);
        Assert.Single(cart.Items);
    }

    [Fact]
    public void AddItem_InvalidProduct_ReturnsFalse()
    {
        // Arrange
        var service = new CartService(new ProductService());
        var sessionId = Guid.NewGuid().ToString();

        // Act
        var result = service.AddItem(sessionId, 99999, 1);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void UpdateQuantity_ValidItem_UpdatesQuantity()
    {
        // Arrange
        var service = new CartService(new ProductService());
        var sessionId = Guid.NewGuid().ToString();
        service.AddItem(sessionId, 1, 1);

        // Act
        var result = service.UpdateQuantity(sessionId, 1, 3);

        // Assert
        Assert.True(result);
        var cart = service.GetCart(sessionId);
        Assert.Equal(3, cart.Items.First().Quantity);
    }

    [Fact]
    public void RemoveItem_ValidItem_RemovesFromCart()
    {
        // Arrange
        var service = new CartService(new ProductService());
        var sessionId = Guid.NewGuid().ToString();
        service.AddItem(sessionId, 1, 1);

        // Act
        service.RemoveItem(sessionId, 1);

        // Assert
        var cart = service.GetCart(sessionId);
        Assert.Empty(cart.Items);
    }

    [Fact]
    public void Clear_RemovesAllItems()
    {
        // Arrange
        var service = new CartService(new ProductService());
        var sessionId = Guid.NewGuid().ToString();
        service.AddItem(sessionId, 1, 1);
        service.AddItem(sessionId, 2, 1);

        // Act
        service.Clear(sessionId);

        // Assert
        var cart = service.GetCart(sessionId);
        Assert.Empty(cart.Items);
    }

    [Fact]
    public void GetItemCount_ReturnsCorrectCount()
    {
        // Arrange
        var service = new CartService(new ProductService());
        var sessionId = Guid.NewGuid().ToString();
        service.AddItem(sessionId, 1, 2);
        service.AddItem(sessionId, 2, 3);

        // Act
        var count = service.GetItemCount(sessionId);

        // Assert
        Assert.Equal(5, count);
    }
}
