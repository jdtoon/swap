namespace SwapDebtors.Services;

/// <summary>
/// Currency service interface for exchange rates and conversion.
/// </summary>
public interface ICurrencyService
{
    /// <summary>
    /// Get supported currency codes.
    /// </summary>
    string[] GetSupportedCurrencies();

    /// <summary>
    /// Get exchange rates (relative to base currency).
    /// </summary>
    Task<Dictionary<string, decimal>> GetRatesAsync(string baseCurrency = "USD");

    /// <summary>
    /// Convert an amount from one currency to another.
    /// </summary>
    Task<decimal> ConvertAsync(decimal amount, string fromCurrency, string toCurrency);

    /// <summary>
    /// Refresh rates from external API.
    /// </summary>
    Task RefreshRatesAsync();
}
