namespace EcomApp.Models;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public string ImageUrl { get; set; } = "/images/placeholder.jpg";
    public int CategoryId { get; set; }
    public Category? Category { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
    
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
}

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public bool IsActive { get; set; } = true;
    
    public ICollection<Product> Products { get; set; } = new List<Product>();
}

public class Cart
{
    public int Id { get; set; }
    public string SessionId { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    public ICollection<CartItem> Items { get; set; } = new List<CartItem>();
}

public class CartItem
{
    public int Id { get; set; }
    public int CartId { get; set; }
    public Cart? Cart { get; set; }
    public int ProductId { get; set; }
    public Product? Product { get; set; }
    public int Quantity { get; set; }
    public decimal PriceAtAdd { get; set; }
}

public class Order
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public string CustomerEmail { get; set; } = "";
    public string ShippingAddress { get; set; } = "";
    public decimal TotalAmount { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}

public class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public Order? Order { get; set; }
    public int ProductId { get; set; }
    public Product? Product { get; set; }
    public int Quantity { get; set; }
    public decimal PriceAtOrder { get; set; }
}

public enum OrderStatus
{
    Pending,
    Processing,
    Shipped,
    Delivered,
    Cancelled
}
