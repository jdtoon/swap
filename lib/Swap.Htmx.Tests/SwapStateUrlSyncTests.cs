using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Swap.Htmx.State;
using Xunit;

namespace Swap.Htmx.Tests;

public class SwapStateUrlSyncTests
{
    private class TestState : SwapState
    {
        public string Tab { get; set; } = "all";
        public int Page { get; set; } = 1;
        public string? Search { get; set; }
        public bool ShowDeleted { get; set; } = false;

        public override bool UrlSync => true;
    }

    private class PrefixedState : SwapState
    {
        public string Filter { get; set; } = "none";
        public int Limit { get; set; } = 10;

        public override bool UrlSync => true;
        public override string UrlPrefix => "inv";
    }

    private class ExcludedPropsState : SwapState
    {
        public string PublicProp { get; set; } = "visible";
        public string SensitiveProp { get; set; } = "secret";

        public override bool UrlSync => true;
        protected override IEnumerable<string> UrlExcludedProperties => new[] { "SensitiveProp" };
    }

    private class NoSyncState : SwapState
    {
        public string Value { get; set; } = "test";
        
        // UrlSync is false by default
    }

    [Fact]
    public void ToQueryString_Returns_NonDefault_Values()
    {
        var state = new TestState
        {
            Tab = "active",
            Page = 3,
            Search = "widget"
        };

        var queryString = state.ToQueryString();

        Assert.Contains("Tab=active", queryString);
        Assert.Contains("Page=3", queryString);
        Assert.Contains("Search=widget", queryString);
        Assert.DoesNotContain("ShowDeleted", queryString); // Still at default
    }

    [Fact]
    public void ToQueryString_Returns_Empty_When_All_Default()
    {
        var state = new TestState();

        var queryString = state.ToQueryString();

        Assert.Empty(queryString);
    }

    [Fact]
    public void ToQueryString_Returns_Empty_When_UrlSync_Disabled()
    {
        var state = new NoSyncState { Value = "changed" };

        var queryString = state.ToQueryString();

        Assert.Empty(queryString);
    }

    [Fact]
    public void ToQueryString_Uses_Prefix()
    {
        var state = new PrefixedState
        {
            Filter = "active",
            Limit = 25
        };

        var queryString = state.ToQueryString();

        Assert.Contains("invFilter=active", queryString);
        Assert.Contains("invLimit=25", queryString);
    }

    [Fact]
    public void ToQueryString_Excludes_Specified_Properties()
    {
        var state = new ExcludedPropsState
        {
            PublicProp = "changed-visible",  // Change from default to include in URL
            SensitiveProp = "should-not-appear"
        };

        var queryString = state.ToQueryString();

        Assert.Contains("PublicProp=changed-visible", queryString);
        Assert.DoesNotContain("SensitiveProp", queryString);
    }

    [Fact]
    public void FromQueryString_Populates_State()
    {
        var state = new TestState();
        var query = CreateQueryCollection(new Dictionary<string, StringValues>
        {
            { "Tab", "completed" },
            { "Page", "5" },
            { "Search", "test" },
            { "ShowDeleted", "true" }
        });

        state.FromQueryString(query);

        Assert.Equal("completed", state.Tab);
        Assert.Equal(5, state.Page);
        Assert.Equal("test", state.Search);
        Assert.True(state.ShowDeleted);
    }

    [Fact]
    public void FromQueryString_Uses_Prefix()
    {
        var state = new PrefixedState();
        var query = CreateQueryCollection(new Dictionary<string, StringValues>
        {
            { "invFilter", "pending" },
            { "invLimit", "50" },
            { "Filter", "should-ignore" } // Wrong prefix
        });

        state.FromQueryString(query);

        Assert.Equal("pending", state.Filter);
        Assert.Equal(50, state.Limit);
    }

    [Fact]
    public void FromQueryString_Does_Nothing_When_UrlSync_Disabled()
    {
        var state = new NoSyncState();
        var query = CreateQueryCollection(new Dictionary<string, StringValues>
        {
            { "Value", "modified" }
        });

        state.FromQueryString(query);

        Assert.Equal("test", state.Value); // Unchanged
    }

    [Fact]
    public void FromQueryString_Ignores_Excluded_Properties()
    {
        var state = new ExcludedPropsState();
        var query = CreateQueryCollection(new Dictionary<string, StringValues>
        {
            { "PublicProp", "modified" },
            { "SensitiveProp", "hacked" }
        });

        state.FromQueryString(query);

        Assert.Equal("modified", state.PublicProp);
        Assert.Equal("secret", state.SensitiveProp); // Unchanged
    }

    [Fact]
    public void AppendToUrl_Adds_QueryString()
    {
        var state = new TestState
        {
            Tab = "active",
            Page = 2
        };

        var url = state.AppendToUrl("/products");

        Assert.StartsWith("/products?", url);
        Assert.Contains("Tab=active", url);
        Assert.Contains("Page=2", url);
    }

    [Fact]
    public void AppendToUrl_Appends_To_Existing_QueryString()
    {
        var state = new TestState
        {
            Tab = "active"
        };

        var url = state.AppendToUrl("/products?category=electronics");

        Assert.StartsWith("/products?category=electronics&", url);
        Assert.Contains("Tab=active", url);
    }

    [Fact]
    public void AppendToUrl_Returns_Original_When_All_Default()
    {
        var state = new TestState();

        var url = state.AppendToUrl("/products");

        Assert.Equal("/products", url);
    }

    [Fact]
    public void ToQueryString_Handles_Boolean_Values()
    {
        var state = new TestState
        {
            ShowDeleted = true
        };

        var queryString = state.ToQueryString();

        Assert.Contains("ShowDeleted=true", queryString);
    }

    [Fact]
    public void FromQueryString_Handles_Boolean_Variations()
    {
        var states = new[]
        {
            ("true", true),
            ("True", true),
            ("TRUE", true),
            ("1", true),
            ("on", true),
            ("false", false),
            ("0", false),
            ("", false)
        };

        foreach (var (input, expected) in states)
        {
            var state = new TestState();
            var query = CreateQueryCollection(new Dictionary<string, StringValues>
            {
                { "ShowDeleted", input }
            });

            state.FromQueryString(query);

            Assert.Equal(expected, state.ShowDeleted);
        }
    }

    [Fact]
    public void ToQueryString_UrlEncodes_Special_Characters()
    {
        var state = new TestState
        {
            Search = "hello world & more"
        };

        var queryString = state.ToQueryString();

        Assert.Contains("Search=hello%20world%20%26%20more", queryString);
    }

    private static QueryCollection CreateQueryCollection(Dictionary<string, StringValues> values)
    {
        return new QueryCollection(values);
    }
}
