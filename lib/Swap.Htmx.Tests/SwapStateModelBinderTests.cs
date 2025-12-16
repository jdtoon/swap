using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Primitives;
using Swap.Htmx.State;
using Xunit;

namespace Swap.Htmx.Tests;

/// <summary>
/// Test state for model binder tests.
/// </summary>
public class FilterState : SwapState
{
    public string Category { get; set; } = "all";
    public int Page { get; set; } = 1;
    public bool ShowInactive { get; set; } = false;
    public string? SearchTerm { get; set; }
}

/// <summary>
/// Tests for SwapStateModelBinder - the [FromSwapState] binding mechanism.
/// </summary>
public class SwapStateModelBinderTests
{
    private static ModelBindingContext CreateBindingContext(
        Dictionary<string, StringValues>? formValues = null,
        Dictionary<string, StringValues>? queryValues = null)
    {
        var httpContext = new DefaultHttpContext();
        
        // Set up form values
        if (formValues != null)
        {
            httpContext.Request.ContentType = "application/x-www-form-urlencoded";
            httpContext.Request.Form = new FormCollection(formValues);
        }
        
        // Set up query values
        if (queryValues != null)
        {
            httpContext.Request.QueryString = new QueryString(
                "?" + string.Join("&", queryValues.Select(kv => $"{kv.Key}={kv.Value}"))
            );
        }

        var actionContext = new ActionContext(httpContext, new Microsoft.AspNetCore.Routing.RouteData(), new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor());
        
        var valueProviders = new List<IValueProvider>();
        
        // Query string values come FIRST (higher priority)
        if (queryValues != null)
        {
            valueProviders.Add(new QueryStringValueProvider(
                BindingSource.Query,
                new QueryCollection(queryValues),
                System.Globalization.CultureInfo.InvariantCulture));
        }
        
        // Form values come SECOND (lower priority - from hx-include)
        if (formValues != null)
        {
            valueProviders.Add(new FormValueProvider(
                BindingSource.Form,
                new FormCollection(formValues),
                System.Globalization.CultureInfo.InvariantCulture));
        }

        var bindingContext = new DefaultModelBindingContext
        {
            ActionContext = actionContext,
            ModelMetadata = new EmptyModelMetadataProvider().GetMetadataForType(typeof(FilterState)),
            ModelName = string.Empty,
            ModelState = new ModelStateDictionary(),
            ValueProvider = new CompositeValueProvider(valueProviders)
        };

        return bindingContext;
    }

    [Fact]
    public async Task BindModelAsync_BindsFromFormValues()
    {
        // Arrange
        var binder = new SwapStateModelBinder();
        var context = CreateBindingContext(formValues: new Dictionary<string, StringValues>
        {
            { "Category", "Electronics" },
            { "Page", "3" },
            { "ShowInactive", "true" }
        });

        // Act
        await binder.BindModelAsync(context);

        // Assert
        Assert.True(context.Result.IsModelSet);
        var state = context.Result.Model as FilterState;
        Assert.NotNull(state);
        Assert.Equal("Electronics", state.Category);
        Assert.Equal(3, state.Page);
        Assert.True(state.ShowInactive);
    }

    [Fact]
    public async Task BindModelAsync_UrlParams_Override_FormValues()
    {
        // Arrange - URL has Category=Books, Form has Category=Electronics
        // URL params should win because they come first in the value provider
        var binder = new SwapStateModelBinder();
        var context = CreateBindingContext(
            queryValues: new Dictionary<string, StringValues>
            {
                { "Category", "Books" }
            },
            formValues: new Dictionary<string, StringValues>
            {
                { "Category", "Electronics" },
                { "Page", "5" }
            });

        // Act
        await binder.BindModelAsync(context);

        // Assert
        Assert.True(context.Result.IsModelSet);
        var state = context.Result.Model as FilterState;
        Assert.NotNull(state);
        Assert.Equal("Books", state.Category); // URL wins over form
        Assert.Equal(5, state.Page); // Only in form, so form value used
    }

    [Fact]
    public async Task BindModelAsync_MergesValuesFromBothSources()
    {
        // Arrange - different properties from different sources
        var binder = new SwapStateModelBinder();
        var context = CreateBindingContext(
            queryValues: new Dictionary<string, StringValues>
            {
                { "Page", "10" }
            },
            formValues: new Dictionary<string, StringValues>
            {
                { "Category", "Furniture" },
                { "ShowInactive", "true" }
            });

        // Act
        await binder.BindModelAsync(context);

        // Assert
        Assert.True(context.Result.IsModelSet);
        var state = context.Result.Model as FilterState;
        Assert.NotNull(state);
        Assert.Equal("Furniture", state.Category); // From form
        Assert.Equal(10, state.Page); // From URL
        Assert.True(state.ShowInactive); // From form
    }

    [Fact]
    public async Task BindModelAsync_UsesDefaultValues_WhenNotProvided()
    {
        // Arrange - only provide some values
        var binder = new SwapStateModelBinder();
        var context = CreateBindingContext(formValues: new Dictionary<string, StringValues>
        {
            { "Category", "Books" }
        });

        // Act
        await binder.BindModelAsync(context);

        // Assert
        Assert.True(context.Result.IsModelSet);
        var state = context.Result.Model as FilterState;
        Assert.NotNull(state);
        Assert.Equal("Books", state.Category);
        Assert.Equal(1, state.Page); // Default value
        Assert.False(state.ShowInactive); // Default value
        Assert.Null(state.SearchTerm); // Default value
    }

    [Fact]
    public async Task BindModelAsync_ClearsChangeTracking_AfterBinding()
    {
        // Arrange
        var binder = new SwapStateModelBinder();
        var context = CreateBindingContext(formValues: new Dictionary<string, StringValues>
        {
            { "Category", "Books" },
            { "Page", "5" }
        });

        // Act
        await binder.BindModelAsync(context);

        // Assert
        Assert.True(context.Result.IsModelSet);
        var state = context.Result.Model as FilterState;
        Assert.NotNull(state);
        Assert.False(state.HasChanges); // Changes should be cleared after binding
    }

    [Fact]
    public async Task BindModelAsync_HandlesEmptyStringValues()
    {
        // Arrange - explicit empty string
        var binder = new SwapStateModelBinder();
        var context = CreateBindingContext(formValues: new Dictionary<string, StringValues>
        {
            { "SearchTerm", "" },
            { "Category", "Books" }
        });

        // Act
        await binder.BindModelAsync(context);

        // Assert
        Assert.True(context.Result.IsModelSet);
        var state = context.Result.Model as FilterState;
        Assert.NotNull(state);
        Assert.Equal("", state.SearchTerm); // Explicitly set to empty
        Assert.Equal("Books", state.Category);
    }

    [Fact]
    public async Task BindModelAsync_IgnoresUnknownProperties()
    {
        // Arrange - include properties that don't exist on the state
        var binder = new SwapStateModelBinder();
        var context = CreateBindingContext(formValues: new Dictionary<string, StringValues>
        {
            { "Category", "Books" },
            { "UnknownProp", "ShouldBeIgnored" },
            { "AnotherUnknown", "123" }
        });

        // Act
        await binder.BindModelAsync(context);

        // Assert - should succeed without errors
        Assert.True(context.Result.IsModelSet);
        var state = context.Result.Model as FilterState;
        Assert.NotNull(state);
        Assert.Equal("Books", state.Category);
    }

    [Fact]
    public async Task BindModelAsync_HandlesBooleanCheckboxPattern()
    {
        // The checkbox pattern: checkbox + hidden field with opposite value
        // First value wins, so if checkbox is checked it sends "true" first
        // If unchecked, only the hidden "false" is sent
        
        var binder = new SwapStateModelBinder();
        
        // Scenario 1: Checkbox checked - "true" comes first
        var contextChecked = CreateBindingContext(formValues: new Dictionary<string, StringValues>
        {
            { "ShowInactive", new StringValues(new[] { "true", "false" }) } // checkbox + hidden
        });

        await binder.BindModelAsync(contextChecked);
        var stateChecked = contextChecked.Result.Model as FilterState;
        Assert.NotNull(stateChecked);
        Assert.True(stateChecked.ShowInactive);

        // Scenario 2: Checkbox unchecked - only "false" from hidden field
        var contextUnchecked = CreateBindingContext(formValues: new Dictionary<string, StringValues>
        {
            { "ShowInactive", "false" }
        });

        await binder.BindModelAsync(contextUnchecked);
        var stateUnchecked = contextUnchecked.Result.Model as FilterState;
        Assert.NotNull(stateUnchecked);
        Assert.False(stateUnchecked.ShowInactive);
    }
}
