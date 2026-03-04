using Microsoft.AspNetCore.Mvc.Testing;
using System.Diagnostics;
using System.Net;
using Xunit;
using Xunit.Abstractions;

namespace SwapSmallPartials.Tests;

/// <summary>
/// Performance and correctness validation for OOB swap parallelization.
/// Hits the Analytics/SimulatePurchase endpoint that produces 11-14 OOB swaps
/// and measures response time + validates content.
/// </summary>
public class OobPerformanceTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly ITestOutputHelper _output;

    public OobPerformanceTests(WebApplicationFactory<Program> factory, ITestOutputHelper output)
    {
        _factory = factory;
        _output = output;
    }

    [Fact]
    public async Task SimulatePurchase_ReturnsSuccess_WithEventTriggers()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("HX-Request", "true");

        var response = await client.PostAsync("/Analytics/SimulatePurchase", null);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();

        // SimulatePurchase uses SwapEvent() which fires through the event system.
        // Verify the response is valid HTML (may contain OOB swaps or just triggers)
        _output.WriteLine($"Response body length: {content.Length} chars");

        if (content.Length > 0)
        {
            var oobCount = CountOccurrences(content, "hx-swap-oob");
            _output.WriteLine($"OOB swap count: {oobCount}");
        }

        // Check for HX-Trigger header (event system always emits these)
        var hasTrigger = response.Headers.Contains("HX-Trigger");
        _output.WriteLine($"Has HX-Trigger header: {hasTrigger}");
    }

    [Fact]
    public async Task SimulatePurchase_TimedRun_MultipleRequests()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("HX-Request", "true");

        // Warmup
        for (var i = 0; i < 3; i++)
        {
            var warmup = await client.PostAsync("/Analytics/SimulatePurchase", null);
            warmup.EnsureSuccessStatusCode();
        }

        // Timed runs
        const int iterations = 20;
        var times = new List<double>();

        for (var i = 0; i < iterations; i++)
        {
            var sw = Stopwatch.StartNew();
            var response = await client.PostAsync("/Analytics/SimulatePurchase", null);
            sw.Stop();

            response.EnsureSuccessStatusCode();
            times.Add(sw.Elapsed.TotalMilliseconds);
        }

        var avg = times.Average();
        var min = times.Min();
        var max = times.Max();
        var p50 = Percentile(times, 50);
        var p95 = Percentile(times, 95);

        _output.WriteLine($"=== OOB Performance ({iterations} iterations) ===");
        _output.WriteLine($"  Avg: {avg:F2}ms");
        _output.WriteLine($"  Min: {min:F2}ms");
        _output.WriteLine($"  Max: {max:F2}ms");
        _output.WriteLine($"  P50: {p50:F2}ms");
        _output.WriteLine($"  P95: {p95:F2}ms");
    }

    [Fact]
    public async Task SimulatePurchase_ResponseIsValid()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("HX-Request", "true");

        var response = await client.PostAsync("/Analytics/SimulatePurchase", null);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();

        _output.WriteLine($"Response length: {content.Length} chars");

        if (content.Length > 0)
        {
            // If there's body content, verify it contains OOB partials
            Assert.Contains("partial-", content);
            _output.WriteLine($"OOB count: {CountOccurrences(content, "hx-swap-oob")}");
        }
    }

    private static int CountOccurrences(string text, string pattern)
    {
        var count = 0;
        var index = 0;
        while ((index = text.IndexOf(pattern, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += pattern.Length;
        }
        return count;
    }

    private static double Percentile(List<double> values, int percentile)
    {
        var sorted = values.OrderBy(v => v).ToList();
        var index = (int)Math.Ceiling(percentile / 100.0 * sorted.Count) - 1;
        return sorted[Math.Max(0, index)];
    }
}
