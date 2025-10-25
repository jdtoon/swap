using EcomApp.Data;
using EcomApp.Models;
using Microsoft.EntityFrameworkCore;

namespace EcomApp.Services;

public interface IProductService
{
    Task<List<Product>> GetAllAsync();
    Task<Product?> GetByIdAsync(int id);
    Task<List<Product>> SearchAsync(string query);
    Task<List<Product>> GetByCategoryAsync(int categoryId);
}

public class ProductService : IProductService
{
    private readonly EcomDbContext _context;
    
    public ProductService(EcomDbContext context)
    {
        _context = context;
    }
    
    public async Task<List<Product>> GetAllAsync()
    {
        return await _context.Products
            .Include(p => p.Category)
            .Where(p => p.IsActive)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }
    
    public async Task<Product?> GetByIdAsync(int id)
    {
        return await _context.Products
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);
    }
    
    public async Task<List<Product>> SearchAsync(string query)
    {
        return await _context.Products
            .Include(p => p.Category)
            .Where(p => p.IsActive && 
                       (p.Name.Contains(query) || 
                        p.Description.Contains(query)))
            .ToListAsync();
    }
    
    public async Task<List<Product>> GetByCategoryAsync(int categoryId)
    {
        return await _context.Products
            .Include(p => p.Category)
            .Where(p => p.IsActive && p.CategoryId == categoryId)
            .ToListAsync();
    }
}

public interface ICategoryService
{
    Task<List<Category>> GetAllAsync();
}

public class CategoryService : ICategoryService
{
    private readonly EcomDbContext _context;
    
    public CategoryService(EcomDbContext context)
    {
        _context = context;
    }
    
    public async Task<List<Category>> GetAllAsync()
    {
        return await _context.Categories
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync();
    }
}

public interface ICartService
{
    Task<Cart> GetOrCreateCartAsync(string sessionId);
    Task AddItemAsync(string sessionId, int productId, int quantity);
    Task RemoveItemAsync(string sessionId, int cartItemId);
    Task UpdateQuantityAsync(string sessionId, int cartItemId, int quantity);
    Task ClearCartAsync(string sessionId);
    Task<decimal> GetTotalAsync(string sessionId);
}

public class CartService : ICartService
{
    private readonly EcomDbContext _context;
    
    public CartService(EcomDbContext context)
    {
        _context = context;
    }
    
    public async Task<Cart> GetOrCreateCartAsync(string sessionId)
    {
        var cart = await _context.Carts
            .Include(c => c.Items)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(c => c.SessionId == sessionId);
            
        if (cart == null)
        {
            cart = new Cart { SessionId = sessionId };
            _context.Carts.Add(cart);
            await _context.SaveChangesAsync();
        }
        
        return cart;
    }
    
    public async Task AddItemAsync(string sessionId, int productId, int quantity)
    {
        var cart = await GetOrCreateCartAsync(sessionId);
        var product = await _context.Products.FindAsync(productId);
        
        if (product == null || !product.IsActive) return;
        
        var existingItem = cart.Items.FirstOrDefault(i => i.ProductId == productId);
        
        if (existingItem != null)
        {
            existingItem.Quantity += quantity;
        }
        else
        {
            cart.Items.Add(new CartItem
            {
                CartId = cart.Id,
                ProductId = productId,
                Quantity = quantity,
                PriceAtAdd = product.Price
            });
        }
        
        cart.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }
    
    public async Task RemoveItemAsync(string sessionId, int cartItemId)
    {
        var cart = await GetOrCreateCartAsync(sessionId);
        var item = cart.Items.FirstOrDefault(i => i.Id == cartItemId);
        
        if (item != null)
        {
            cart.Items.Remove(item);
            cart.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }
    
    public async Task UpdateQuantityAsync(string sessionId, int cartItemId, int quantity)
    {
        var cart = await GetOrCreateCartAsync(sessionId);
        var item = cart.Items.FirstOrDefault(i => i.Id == cartItemId);
        
        if (item != null)
        {
            if (quantity <= 0)
            {
                await RemoveItemAsync(sessionId, cartItemId);
            }
            else
            {
                item.Quantity = quantity;
                cart.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }
    }
    
    public async Task ClearCartAsync(string sessionId)
    {
        var cart = await GetOrCreateCartAsync(sessionId);
        cart.Items.Clear();
        await _context.SaveChangesAsync();
    }
    
    public async Task<decimal> GetTotalAsync(string sessionId)
    {
        var cart = await GetOrCreateCartAsync(sessionId);
        return cart.Items.Sum(i => i.Quantity * i.PriceAtAdd);
    }
}

public interface IOrderService
{
    Task<Order> CreateOrderAsync(string sessionId, string customerName, string customerEmail, string shippingAddress);
    Task<List<Order>> GetAllAsync();
    Task<Order?> GetByIdAsync(int id);
}

public class OrderService : IOrderService
{
    private readonly EcomDbContext _context;
    private readonly ICartService _cartService;
    
    public OrderService(EcomDbContext context, ICartService cartService)
    {
        _context = context;
        _cartService = cartService;
    }
    
    public async Task<Order> CreateOrderAsync(string sessionId, string customerName, string customerEmail, string shippingAddress)
    {
        var cart = await _cartService.GetOrCreateCartAsync(sessionId);
        
        if (!cart.Items.Any())
            throw new InvalidOperationException("Cart is empty");
        
        var order = new Order
        {
            OrderNumber = $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpper()}",
            CustomerName = customerName,
            CustomerEmail = customerEmail,
            ShippingAddress = shippingAddress,
            TotalAmount = cart.Items.Sum(i => i.Quantity * i.PriceAtAdd),
            Status = OrderStatus.Pending
        };
        
        foreach (var cartItem in cart.Items)
        {
            order.Items.Add(new OrderItem
            {
                ProductId = cartItem.ProductId,
                Quantity = cartItem.Quantity,
                PriceAtOrder = cartItem.PriceAtAdd
            });
        }
        
        _context.Orders.Add(order);
        await _cartService.ClearCartAsync(sessionId);
        await _context.SaveChangesAsync();
        
        return order;
    }
    
    public async Task<List<Order>> GetAllAsync()
    {
        return await _context.Orders
            .Include(o => o.Items)
            .ThenInclude(i => i.Product)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
    }
    
    public async Task<Order?> GetByIdAsync(int id)
    {
        return await _context.Orders
            .Include(o => o.Items)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.Id == id);
    }
}
