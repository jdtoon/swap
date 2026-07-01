using System.Reflection;
using Swap.Htmx.State;
using Xunit;

namespace Swap.Htmx.Tests;

/// <summary>
/// Test state class used to verify per-Type reflection metadata caching.
/// </summary>
public class MetadataCacheTestState : SwapState
{
    public string Name { get; set; } = "";
    public int Count { get; set; }
    public bool Flag { get; set; }
}

/// <summary>
/// A second, distinct state type so we can confirm the cache is keyed per-Type
/// (i.e. different types don't share the same cached property array).
/// </summary>
public class OtherMetadataCacheTestState : SwapState
{
    public string Title { get; set; } = "";
}

/// <summary>
/// Tests proving that reflection metadata (the filtered property set) for SwapState
/// subclasses is computed once per Type and reused across instances, rather than
/// being re-computed via GetProperties()+LINQ on every call.
/// </summary>
public class SwapStateMetadataCacheTests
{
    private static PropertyInfo[] InvokeGetStateProperties(SwapState state)
    {
        var method = typeof(SwapState).GetMethod(
            "GetStateProperties",
            BindingFlags.NonPublic | BindingFlags.Instance)!;

        var result = (IEnumerable<PropertyInfo>)method.Invoke(state, null)!;
        return result as PropertyInfo[] ?? result.ToArray();
    }

    [Fact]
    public void GetStateProperties_ReturnsSameArrayInstance_AcrossDifferentInstancesOfSameType()
    {
        var a = new MetadataCacheTestState();
        var b = new MetadataCacheTestState();

        var propsA = InvokeGetStateProperties(a);
        var propsB = InvokeGetStateProperties(b);

        // Proves the underlying PropertyInfo[] is cached per-Type and reused,
        // rather than reflected (GetProperties()+LINQ) again for each instance.
        Assert.Same(propsA, propsB);
    }

    [Fact]
    public void GetStateProperties_ReturnsEqualPropertySets_AcrossDifferentInstancesOfSameType()
    {
        var a = new MetadataCacheTestState();
        var b = new MetadataCacheTestState();

        var namesA = InvokeGetStateProperties(a).Select(p => p.Name).OrderBy(n => n).ToArray();
        var namesB = InvokeGetStateProperties(b).Select(p => p.Name).OrderBy(n => n).ToArray();

        Assert.Equal(new[] { "Count", "Flag", "Name" }, namesA);
        Assert.Equal(namesA, namesB);
    }

    [Fact]
    public void GetStateProperties_DoesNotShareCachedArray_BetweenDifferentTypes()
    {
        var a = new MetadataCacheTestState();
        var other = new OtherMetadataCacheTestState();

        var propsA = InvokeGetStateProperties(a);
        var propsOther = InvokeGetStateProperties(other);

        Assert.NotSame(propsA, propsOther);
        Assert.DoesNotContain(propsOther, p => p.Name == "Name");
    }

    [Fact]
    public void StateValues_RoundTrip_ThroughCachedMetadata()
    {
        var state = new MetadataCacheTestState
        {
            Name = "hello",
            Count = 42,
            Flag = true
        };

        var values = state.GetStateValues();

        var restored = new MetadataCacheTestState();
        restored.SetStateValues(values);

        Assert.Equal(state.Name, restored.Name);
        Assert.Equal(state.Count, restored.Count);
        Assert.Equal(state.Flag, restored.Flag);
    }
}
