using SwapShop.Models;

namespace SwapShop.Services;

/// <summary>
/// In-memory product catalog service
/// </summary>
public interface IProductService
{
    IReadOnlyList<Product> GetAll();
    Product? GetById(int id);
    IReadOnlyList<Product> Search(string? query);
    void UpdateStock(int productId, int quantity);
    int GetProductCount();
}

public class ProductService : IProductService
{
    private static readonly List<Product> _products = new()
    {
        new Product
        {
            Id = 1,
            Name = "Classic White T-Shirt",
            Description = "Premium 100% cotton, comfortable fit, timeless design",
            Price = 24.99m,
            Stock = 50,
            ImageUrl = "/images/products/tshirt-white.jpg",
            Category = "Clothing"
        },
        new Product
        {
            Id = 2,
            Name = "Slim Fit Jeans",
            Description = "Dark wash denim, stretch fabric, modern cut",
            Price = 59.99m,
            Stock = 30,
            ImageUrl = "/images/products/jeans.jpg",
            Category = "Clothing"
        },
        new Product
        {
            Id = 3,
            Name = "Leather Sneakers",
            Description = "Italian leather, cushioned sole, all-day comfort",
            Price = 89.99m,
            Stock = 20,
            ImageUrl = "/images/products/sneakers.jpg",
            Category = "Footwear"
        },
        new Product
        {
            Id = 4,
            Name = "Canvas Backpack",
            Description = "Durable canvas, padded laptop compartment, water-resistant",
            Price = 45.99m,
            Stock = 15,
            ImageUrl = "/images/products/backpack.jpg",
            Category = "Accessories"
        },
        new Product
        {
            Id = 5,
            Name = "Wireless Headphones",
            Description = "Noise-cancelling, 30hr battery, premium sound",
            Price = 129.99m,
            Stock = 8,
            ImageUrl = "/images/products/headphones.jpg",
            Category = "Electronics"
        },
        new Product
        {
            Id = 6,
            Name = "Stainless Steel Water Bottle",
            Description = "Keeps drinks cold 24hrs, leak-proof, BPA-free",
            Price = 19.99m,
            Stock = 0, // Out of stock
            ImageUrl = "/images/products/bottle.jpg",
            Category = "Accessories"
        },
        new Product
        {
            Id = 7,
            Name = "Running Shorts",
            Description = "Moisture-wicking, built-in liner, lightweight",
            Price = 34.99m,
            Stock = 40,
            ImageUrl = "/images/products/shorts.jpg",
            Category = "Clothing"
        },
        new Product
        {
            Id = 8,
            Name = "Yoga Mat",
            Description = "Non-slip surface, extra cushioning, eco-friendly",
            Price = 29.99m,
            Stock = 25,
            ImageUrl = "/images/products/yoga-mat.jpg",
            Category = "Fitness"
        }
    };

    public IReadOnlyList<Product> GetAll() => _products.AsReadOnly();

    public Product? GetById(int id) => _products.FirstOrDefault(p => p.Id == id);

    public IReadOnlyList<Product> Search(string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return GetAll();
        }

        var searchTerm = query.Trim().ToLowerInvariant();
        return _products
            .Where(p =>
                p.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                p.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                p.Category.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
            .ToList()
            .AsReadOnly();
    }

    public void UpdateStock(int productId, int quantity)
    {
        var product = GetById(productId);
        if (product != null)
        {
            product.Stock = Math.Max(0, product.Stock + quantity);
        }
    }

    public int GetProductCount() => _products.Count;
}
