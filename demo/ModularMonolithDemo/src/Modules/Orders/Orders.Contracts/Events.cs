namespace ModularMonolithDemo.Modules.Orders.Contracts;

public static class OrderEvents
{
    public const string OrderCreated = "Orders.OrderCreated";
}

public record OrderCreated(Guid OrderId, decimal Total);
