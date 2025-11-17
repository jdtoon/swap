using SwapShop.Models;

namespace SwapShop.Services;

/// <summary>
/// In-memory shopping cart service (session-based)
/// </summary>
public interface ICartService
{
    Cart GetCart(string sessionId);
    void AddItem(string sessionId, int productId, int quantity);
    void UpdateQuantity(string sessionId, int productId, int quantity);
    void RemoveItem(string sessionId, int productId);
    void ClearCart(string sessionId);
    int GetItemCount(string sessionId);
}

public class CartService : ICartService
{
    private static readonly Dictionary<string, Cart> _carts = new();
    private readonly IProductService _productService;

    public CartService(IProductService productService)
    {
        _productService = productService;
    }

    public Cart GetCart(string sessionId)
    {
        if (!_carts.TryGetValue(sessionId, out var cart))
        {
            cart = new Cart { SessionId = sessionId };
            _carts[sessionId] = cart;
        }
        return cart;
    }

    public void AddItem(string sessionId, int productId, int quantity)
    {
        var product = _productService.GetById(productId);
        if (product == null || product.Stock < quantity)
        {
            return; // Product not found or insufficient stock
        }

        var cart = GetCart(sessionId);
        var existingItem = cart.Items.FirstOrDefault(i => i.ProductId == productId);

        if (existingItem != null)
        {
            // Update quantity if item already exists
            var newQuantity = existingItem.Quantity + quantity;
            if (newQuantity <= product.Stock)
            {
                existingItem.Quantity = newQuantity;
            }
        }
        else
        {
            // Add new item
            cart.Items.Add(new CartItem
            {
                ProductId = productId,
                ProductName = product.Name,
                Price = product.Price,
                Quantity = quantity,
                ImageUrl = product.ImageUrl
            });
        }
    }

    public void UpdateQuantity(string sessionId, int productId, int quantity)
    {
        var cart = GetCart(sessionId);
        var item = cart.Items.FirstOrDefault(i => i.ProductId == productId);
        
        if (item != null)
        {
            if (quantity <= 0)
            {
                RemoveItem(sessionId, productId);
            }
            else
            {
                var product = _productService.GetById(productId);
                if (product != null && quantity <= product.Stock)
                {
                    item.Quantity = quantity;
                }
            }
        }
    }

    public void RemoveItem(string sessionId, int productId)
    {
        var cart = GetCart(sessionId);
        var item = cart.Items.FirstOrDefault(i => i.ProductId == productId);
        if (item != null)
        {
            cart.Items.Remove(item);
        }
    }

    public void ClearCart(string sessionId)
    {
        var cart = GetCart(sessionId);
        cart.Items.Clear();
    }

    public int GetItemCount(string sessionId)
    {
        var cart = GetCart(sessionId);
        return cart.Items.Sum(i => i.Quantity);
    }
}
