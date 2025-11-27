using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Swap.Htmx.State;
using Swap.Htmx.Models;
using Swap.Htmx.Events;

namespace Swap.Htmx.Benchmarks;

/// <summary>
/// Benchmarks for SwapState operations.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public class SwapStateBenchmarks
{
    private TestState _state = null!;

    [GlobalSetup]
    public void Setup()
    {
        _state = new TestState();
    }

    [Benchmark]
    public IDictionary<string, object?> GetStateValues()
    {
        return _state.GetStateValues();
    }

    [Benchmark]
    public string ToQueryString()
    {
        return _state.ToQueryString();
    }

    [Benchmark]
    public void PropertyUpdate()
    {
        _state.Tab = "products";
        _state.Page = 2;
        _state.Search = "widget";
    }

    [Benchmark]
    public void CreateAndPopulate()
    {
        var state = new TestState
        {
            Tab = "products",
            Page = 5,
            Search = "test search",
            SortBy = "price",
            SortDesc = true
        };
        _ = state.GetStateValues();
    }
}

/// <summary>
/// Benchmarks for SwapResponseBuilder operations.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public class SwapResponseBuilderBenchmarks
{
    [Benchmark]
    public SwapResponseBuilder SimpleResponse()
    {
        return new SwapResponseBuilder()
            .WithView("_Grid")
            .WithSuccessToast("Item added");
    }

    [Benchmark]
    public SwapResponseBuilder ComplexResponse()
    {
        return new SwapResponseBuilder()
            .WithView("_Grid", new { Items = new[] { 1, 2, 3 } })
            .AlsoUpdate("sidebar", "_Sidebar", new { Count = 5 })
            .AlsoUpdate("header", "_Header", null)
            .WithSuccessToast("Operation complete")
            .WithTrigger("data.changed");
    }

    [Benchmark]
    public SwapResponseBuilder WithState()
    {
        var state = new TestState { Tab = "all", Page = 1 };
        return new SwapResponseBuilder()
            .WithView("_Grid")
            .WithState(state);
    }
}

/// <summary>
/// Benchmarks for EventKey operations.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public class EventKeyBenchmarks
{
    private EventKey _key1;
    private EventKey _key2;

    [GlobalSetup]
    public void Setup()
    {
        _key1 = new EventKey("inventory.updated");
        _key2 = new EventKey("inventory.updated");
    }

    [Benchmark]
    public EventKey CreateEventKey()
    {
        return new EventKey("cart.item.added");
    }

    [Benchmark]
    public bool EventKeyEquality()
    {
        return _key1 == _key2;
    }

    [Benchmark]
    public int EventKeyHashCode()
    {
        return _key1.GetHashCode();
    }

    [Benchmark]
    public EventKey ParameterizedEventKey()
    {
        return new EventKey($"product.{42}.updated");
    }
}

/// <summary>
/// Test state class for benchmarks.
/// </summary>
public class TestState : SwapState
{
    public string Tab { get; set; } = "all";
    public int Page { get; set; } = 1;
    public string? Search { get; set; }
    public string SortBy { get; set; } = "name";
    public bool SortDesc { get; set; } = false;
}

public class Program
{
    public static void Main(string[] args)
    {
        var summary = BenchmarkRunner.Run(typeof(Program).Assembly);
    }
}
