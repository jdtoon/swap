using System.Text.Json.Serialization;

namespace Swap.Htmx.Models;

/// <summary>
/// Strongly-typed options for HX-Location JSON body.
/// </summary>
public sealed class HxLocationOptions
{
    [JsonPropertyName("path")] public string? Path { get; set; }
    [JsonPropertyName("target")] public string? Target { get; set; }
    [JsonPropertyName("select")] public string? Select { get; set; }
    [JsonPropertyName("swap")] public string? Swap { get; set; }
    [JsonPropertyName("values")] public Dictionary<string, object?>? Values { get; set; }
    [JsonPropertyName("headers")] public Dictionary<string, string>? Headers { get; set; }

    public HxLocationOptions WithSwap(HxReswapOptions options)
    {
        Swap = options.ToHeaderValue();
        return this;
    }
}
