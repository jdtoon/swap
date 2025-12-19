namespace SwapShop.Events;

public static class CartEventPayloads
{
    public sealed record AddFailed(int ProductId, string Reason);

    public sealed record UpdateFailed(int ProductId, int RequestedQuantity);
}

public static class OrderEventPayloads
{
    public sealed record Failed(string Reason);
}
