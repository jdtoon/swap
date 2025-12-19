using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SwapDebtors.Data;
using SwapDebtors.Models;

namespace SwapDebtors.Services;

/// <summary>
/// Currency service that fetches rates from exchangerate-api.com.
/// Caches rates in the database to reduce API calls.
/// </summary>
public class CurrencyService : ICurrencyService
{
    private readonly DebtorsDbContext _db;
    private readonly HttpClient _httpClient;
    private readonly ILogger<CurrencyService> _logger;
    private readonly string? _apiKey;

    // Supported currencies for this demo
    private static readonly string[] SupportedCurrencies = 
        ["USD", "EUR", "GBP", "JPY", "CAD", "AUD", "CHF", "CNY", "INR", "MXN"];

    // Fallback rates if API is unavailable
    private static readonly Dictionary<string, decimal> FallbackRates = new()
    {
        ["USD"] = 1.00m,
        ["EUR"] = 0.92m,
        ["GBP"] = 0.79m,
        ["JPY"] = 149.50m,
        ["CAD"] = 1.36m,
        ["AUD"] = 1.53m,
        ["CHF"] = 0.88m,
        ["CNY"] = 7.24m,
        ["INR"] = 83.12m,
        ["MXN"] = 17.15m
    };

    public CurrencyService(
        DebtorsDbContext db, 
        HttpClient httpClient, 
        ILogger<CurrencyService> logger,
        IConfiguration config)
    {
        _db = db;
        _httpClient = httpClient;
        _logger = logger;
        _apiKey = config["ExchangeRateApiKey"]; // Optional - uses free tier if not set
    }

    public string[] GetSupportedCurrencies() => SupportedCurrencies;

    public async Task<Dictionary<string, decimal>> GetRatesAsync(string baseCurrency = "USD")
    {
        // Try to get cached rates (less than 1 hour old)
        var cutoff = DateTime.UtcNow.AddHours(-1);
        var cachedRates = await _db.ExchangeRates
            .Where(r => r.BaseCurrency == baseCurrency && r.FetchedAt > cutoff)
            .ToDictionaryAsync(r => r.TargetCurrency, r => r.Rate);

        if (cachedRates.Count >= SupportedCurrencies.Length - 1)
        {
            // Add base currency
            cachedRates[baseCurrency] = 1.00m;
            return cachedRates;
        }

        // Fetch fresh rates
        try
        {
            var rates = await FetchRatesFromApiAsync(baseCurrency);
            await CacheRatesAsync(baseCurrency, rates);
            return rates;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch exchange rates, using fallback");
            return ConvertFallbackRates(baseCurrency);
        }
    }

    public async Task<decimal> ConvertAsync(decimal amount, string fromCurrency, string toCurrency)
    {
        if (fromCurrency == toCurrency) return amount;

        var rates = await GetRatesAsync(fromCurrency);
        if (rates.TryGetValue(toCurrency, out var rate))
        {
            return Math.Round(amount * rate, 2);
        }

        // Try reverse conversion
        var reverseRates = await GetRatesAsync(toCurrency);
        if (reverseRates.TryGetValue(fromCurrency, out var reverseRate) && reverseRate != 0)
        {
            return Math.Round(amount / reverseRate, 2);
        }

        throw new InvalidOperationException($"Cannot convert from {fromCurrency} to {toCurrency}");
    }

    public async Task RefreshRatesAsync()
    {
        // Remove old rates
        var oldRates = await _db.ExchangeRates.ToListAsync();
        _db.ExchangeRates.RemoveRange(oldRates);
        await _db.SaveChangesAsync();

        // Fetch new rates for USD base
        try
        {
            var rates = await FetchRatesFromApiAsync("USD");
            await CacheRatesAsync("USD", rates);
            _logger.LogInformation("Refreshed exchange rates from API");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to refresh rates from API");
        }
    }

    private async Task<Dictionary<string, decimal>> FetchRatesFromApiAsync(string baseCurrency)
    {
        // exchangerate-api.com free tier (or with API key)
        var url = string.IsNullOrEmpty(_apiKey)
            ? $"https://open.er-api.com/v6/latest/{baseCurrency}"
            : $"https://v6.exchangerate-api.com/v6/{_apiKey}/latest/{baseCurrency}";

        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);

        var rates = new Dictionary<string, decimal> { [baseCurrency] = 1.00m };

        if (doc.RootElement.TryGetProperty("rates", out var ratesElement))
        {
            foreach (var currency in SupportedCurrencies)
            {
                if (currency != baseCurrency && ratesElement.TryGetProperty(currency, out var rateElement))
                {
                    rates[currency] = rateElement.GetDecimal();
                }
            }
        }

        return rates;
    }

    private async Task CacheRatesAsync(string baseCurrency, Dictionary<string, decimal> rates)
    {
        var now = DateTime.UtcNow;
        
        foreach (var (currency, rate) in rates)
        {
            if (currency == baseCurrency) continue;

            var existing = await _db.ExchangeRates
                .FirstOrDefaultAsync(r => r.BaseCurrency == baseCurrency && r.TargetCurrency == currency);

            if (existing != null)
            {
                existing.Rate = rate;
                existing.FetchedAt = now;
            }
            else
            {
                _db.ExchangeRates.Add(new ExchangeRate
                {
                    BaseCurrency = baseCurrency,
                    TargetCurrency = currency,
                    Rate = rate,
                    FetchedAt = now
                });
            }
        }

        await _db.SaveChangesAsync();
    }

    private Dictionary<string, decimal> ConvertFallbackRates(string baseCurrency)
    {
        if (baseCurrency == "USD") return new Dictionary<string, decimal>(FallbackRates);

        // Convert fallback rates to requested base currency
        if (!FallbackRates.TryGetValue(baseCurrency, out var baseRate) || baseRate == 0)
        {
            return new Dictionary<string, decimal>(FallbackRates);
        }

        var converted = new Dictionary<string, decimal>();
        foreach (var (currency, rate) in FallbackRates)
        {
            converted[currency] = Math.Round(rate / baseRate, 6);
        }
        return converted;
    }
}
