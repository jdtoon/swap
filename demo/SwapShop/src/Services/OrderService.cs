using SwapShop.Models;

namespace SwapShop.Services;

/// <summary>
/// In-memory order management service
/// </summary>
public interface IOrderService
{
    Order CreateOrder(Cart cart);
    Order? GetById(int id);
    IReadOnlyList<Order> GetAll();
    IReadOnlyList<Order> GetBySessionId(string sessionId);
    void UpdateStatus(int orderId, OrderStatus newStatus);
}

public class OrderService : IOrderService
{
    private static readonly List<Order> _orders = new();
    private static int _nextOrderId = 1000; // Start with 4-digit order numbers
    private readonly IProductService _productService;

    public OrderService(IProductService productService)
    {
        _productService = productService;
    }

    public Order CreateOrder(Cart cart)
    {
        if (!cart.Items.Any())
        {
            throw new InvalidOperationException("Cannot create order from empty cart");
        }

        // Validate stock availability for all items
        foreach (var item in cart.Items)
        {
            var product = _productService.GetById(item.ProductId);
            if (product == null || product.Stock < item.Quantity)
            {
                throw new InvalidOperationException($"Insufficient stock for product: {item.ProductName}");
            }
        }

        var order = new Order
        {
            Id = _nextOrderId++,
            SessionId = cart.SessionId,
            Items = cart.Items.Select(item => new OrderItem
            {
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                Price = item.Price,
                Quantity = item.Quantity
            }).ToList(),
            Status = OrderStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        _orders.Add(order);

        // Deduct stock for ordered items
        foreach (var item in order.Items)
        {
            _productService.UpdateStock(item.ProductId, -item.Quantity);
        }

        return order;
    }

    public Order? GetById(int id) => _orders.FirstOrDefault(o => o.Id == id);

    public IReadOnlyList<Order> GetAll() => _orders
        .OrderByDescending(o => o.CreatedAt)
        .ToList()
        .AsReadOnly();

    public IReadOnlyList<Order> GetBySessionId(string sessionId) => _orders
        .Where(o => o.SessionId == sessionId)
        .OrderByDescending(o => o.CreatedAt)
        .ToList()
        .AsReadOnly();

    public void UpdateStatus(int orderId, OrderStatus newStatus)
    {
        var order = GetById(orderId);
        if (order != null)
        {
            order.Status = newStatus;
        }
    }
}
