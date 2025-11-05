namespace ModularMonolithDemo.Modules.Orders.Contracts;

public static class OrderEvents
{
    public const string OrderCreated = "order.created";
}

public record OrderCreated(Guid OrderId, decimal Total);

// Read-only API surface for cross-module queries
public interface IOrdersReadApi
{
    OrderSummaryDto? GetLatestOrder();
}

public sealed record OrderSummaryDto(Guid OrderId, decimal Total);
