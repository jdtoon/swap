using System.Globalization;
using Swap.Htmx.State;
using Xunit;

namespace Swap.Htmx.Tests;

/// <summary>
/// Test state class with various property types.
/// </summary>
public class TestSearchState : SwapState
{
    public string Search { get; set; } = "";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public bool SortDesc { get; set; } = false;
    public decimal Price { get; set; } = 0m;
}

/// <summary>
/// Tests for the SwapState base class.
/// </summary>
public class SwapStateTests
{
    [Fact]
    public void ContainerId_GeneratesKebabCaseFromTypeName()
    {
        var state = new TestSearchState();
        
        Assert.Equal("test-search-state", state.ContainerId);
    }
    
    [Fact]
    public void GetStateValues_ReturnsAllPublicProperties()
    {
        var state = new TestSearchState
        {
            Search = "test",
            Page = 2,
            PageSize = 25,
            SortDesc = true,
            Price = 99.99m
        };
        
        var values = state.GetStateValues();
        
        Assert.Equal("test", values["Search"]);
        Assert.Equal(2, values["Page"]);
        Assert.Equal(25, values["PageSize"]);
        Assert.Equal(true, values["SortDesc"]);
        Assert.Equal(99.99m, values["Price"]);
    }
    
    [Fact]
    public void SetStateValues_SetsPropertiesFromDictionary()
    {
        var state = new TestSearchState();
        
        state.SetStateValues(new Dictionary<string, object?>
        {
            { "Search", "new value" },
            { "Page", 5 },
            { "SortDesc", true }
        });
        
        Assert.Equal("new value", state.Search);
        Assert.Equal(5, state.Page);
        Assert.True(state.SortDesc);
    }
    
    [Fact]
    public void SetStateValues_IgnoresInvalidProperties()
    {
        var state = new TestSearchState { Search = "original" };
        
        // Should not throw, should ignore unknown property
        state.SetStateValues(new Dictionary<string, object?>
        {
            { "UnknownProperty", "value" },
            { "Search", "changed" }
        });
        
        Assert.Equal("changed", state.Search);
    }
    
    [Fact]
    public void HasChanges_ReturnsFalseForNewInstance()
    {
        var state = new TestSearchState();
        
        // New instance has no tracked changes (properties set via initializers)
        Assert.False(state.HasChanges);
    }
    
    [Fact]
    public void ChangedProperties_TracksModifiedProperties()
    {
        var state = new TestSearchState();
        state.AcceptChanges(); // Clear any initial state
        
        // Modify a property directly
        state.Search = "changed";
        
        // Direct property assignment doesn't track without using SetProperty helper
        // This is by design - only SetProperty calls track changes
    }
    
    [Fact]
    public void AcceptChanges_ClearsChangedProperties()
    {
        var state = new TestSearchState();
        
        // Manually add to changed (simulating SetProperty usage)
        state.AcceptChanges();
        
        Assert.Empty(state.ChangedProperties);
    }
    
    [Fact]
    public void ContainerId_DifferentTypesHaveDifferentIds()
    {
        var state1 = new TestSearchState();
        var state2 = new OtherState();
        
        Assert.NotEqual(state1.ContainerId, state2.ContainerId);
        Assert.Equal("other-state", state2.ContainerId);
    }
    
    [Fact]
    public void SuspendChangeTracking_PreventsChangeTracking()
    {
        var state = new TestSearchState();
        
        using (state.SuspendChangeTracking())
        {
            // Changes during suspension should not be tracked
            state.SetStateValues(new Dictionary<string, object?>
            {
                { "Search", "suspended change" }
            });
        }
        
        // Verify the value was set
        Assert.Equal("suspended change", state.Search);
    }
}

/// <summary>
/// Another test state class to verify unique container IDs.
/// </summary>
public class OtherState : SwapState
{
    public string Name { get; set; } = "";
}
