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
/// A state type that overrides GetStateProperties, to verify the override is honored (not served from
/// or corrupted by the per-Type cache).
/// </summary>
public class OverridingMetadataCacheTestState : SwapState
{
    public string Included { get; set; } = "";
    public string Excluded { get; set; } = "";

    protected override IEnumerable<PropertyInfo> GetStateProperties()
        => base.GetStateProperties().Where(p => p.Name != nameof(Excluded));
}

/// <summary>
/// Tests proving that reflection metadata (the filtered property set) for SwapState
/// subclasses is computed once per Type and reused across instances, rather than
/// being re-computed via GetProperties()+LINQ on every call.
/// </summary>
public class SwapStateMetadataCacheTests
{
    private static IEnumerable<PropertyInfo> InvokeGetStateProperties(SwapState state)
    {
        var method = typeof(SwapState).GetMethod(
            "GetStateProperties",
            BindingFlags.NonPublic | BindingFlags.Instance)!;

        return (IEnumerable<PropertyInfo>)method.Invoke(state, null)!;
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

    [Fact]
    public void SubclassOverride_OfGetStateProperties_IsHonored()
    {
        var overriding = new OverridingMetadataCacheTestState();

        var names = InvokeGetStateProperties(overriding).Select(p => p.Name).ToArray();

        Assert.Contains("Included", names);
        Assert.DoesNotContain("Excluded", names); // the override's filter is applied

        // The cache for a different type is unaffected by the override.
        var normal = new MetadataCacheTestState();
        Assert.Contains("Name", InvokeGetStateProperties(normal).Select(p => p.Name));
    }

    [Fact]
    public void GetStateProperties_ReturnsImmutableView_ThatCannotBeCastToMutableArray()
    {
        var props = InvokeGetStateProperties(new MetadataCacheTestState());

        // Guards the cache: the returned view must not be a raw PropertyInfo[] a caller could mutate.
        Assert.Null(props as PropertyInfo[]);
    }
}
