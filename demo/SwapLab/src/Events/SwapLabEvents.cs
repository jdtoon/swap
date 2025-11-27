using Swap.Htmx;
using Swap.Htmx.Attributes;
using Swap.Htmx.Events;

namespace SwapLab.Events;

/// <summary>
/// Event keys for product demos.
/// The [SwapEventSource] attribute generates EventKey properties from the const values.
/// "product.searched" becomes ProductEvents.Product.Searched
/// </summary>
[SwapEventSource]
public static partial class ProductEvents
{
    public const string Searched = "product.searched";
    public const string TabChanged = "product.tabChanged";
    public const string PageChanged = "product.pageChanged";
    public const string AddedToCart = "product.addedToCart";
}

/// <summary>
/// Event keys for task demos.
/// </summary>
[SwapEventSource]
public static partial class TaskEvents
{
    public const string Created = "task.created";
    public const string Completed = "task.completed";
    public const string Deleted = "task.deleted";
}

/// <summary>
/// Event keys for recipe demos.
/// </summary>
[SwapEventSource]
public static partial class RecipeEvents
{
    public const string SelectionChanged = "recipe.selection.changed";
    public const string ConfigChanged = "recipe.config.changed";
    public const string WizardStepChanged = "recipe.wizard.step.changed";
    public const string ItemUpdated = "recipe.item.updated";
}

/// <summary>
/// Event chain configuration for product demos.
/// </summary>
public class ProductEventConfig : ISwapEventConfiguration
{
    public void Configure(SwapEventBusOptions events)
    {
        // When a product is searched, update the grid AND the count
        // ProductEvents.Product.Searched is generated from the const string
        events.When(ProductEvents.Product.Searched)
            .RefreshPartial("#product-grid", "_ProductGrid")
            .RefreshPartial("#product-count", "_ProductCount")
            .RefreshPartial("#pagination", "_Pagination");

        // When tab changes, update multiple components
        events.When(ProductEvents.Product.TabChanged)
            .RefreshPartial("#product-grid", "_ProductGrid")
            .RefreshPartial("#product-count", "_ProductCount")
            .RefreshPartial("#pagination", "_Pagination");

        // When page changes, just update the grid
        events.When(ProductEvents.Product.PageChanged)
            .RefreshPartial("#product-grid", "_ProductGrid");

        // When a product is added to cart
        events.When(ProductEvents.Product.AddedToCart)
            .RefreshPartial("#cart-badge", "_CartBadge");
    }
}

/// <summary>
/// Event chain configuration for task demos.
/// </summary>
public class TaskEventConfig : ISwapEventConfiguration
{
    public void Configure(SwapEventBusOptions events)
    {
        // When a task is created
        events.When(TaskEvents.Task.Created)
            .RefreshPartial("#task-list", "_TaskList")
            .RefreshPartial("#task-stats", "_TaskStats");

        // When a task is completed
        events.When(TaskEvents.Task.Completed)
            .RefreshPartial("#task-list", "_TaskList")
            .RefreshPartial("#task-stats", "_TaskStats")
            .RefreshPartial("#completion-chart", "_CompletionChart");

        // When a task is deleted
        events.When(TaskEvents.Task.Deleted)
            .RefreshPartial("#task-list", "_TaskList")
            .RefreshPartial("#task-stats", "_TaskStats");
    }
}
