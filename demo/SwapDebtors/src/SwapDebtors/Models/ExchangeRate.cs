namespace SwapDebtors.Models;

/// <summary>
/// Cached exchange rate from external API
/// </summary>
public class ExchangeRate
{
    public int Id { get; set; }
    public string BaseCurrency { get; set; } = "USD";
    public string TargetCurrency { get; set; } = string.Empty;
    public decimal Rate { get; set; }
    public DateTime FetchedAt { get; set; } = DateTime.UtcNow;
}
