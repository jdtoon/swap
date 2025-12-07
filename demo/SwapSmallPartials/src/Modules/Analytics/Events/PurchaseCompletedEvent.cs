namespace SwapSmallPartials.Modules.Analytics.Events;

public class PurchaseCompletedEvent
{
    public int ProductId { get; set; }
    public string Region { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public bool IsNewCustomer { get; set; }
    public bool IsVip { get; set; }
}
