using Swap.Htmx.Events;
using Xunit;

namespace Swap.Htmx.Tests;

public class SwapEventValidationTests
{
    private static class TestEvents
    {
        public static readonly EventKey TodoCreated = new("TodoCreated");
        public static readonly EventKey UiRefreshList = new("ui.refreshList");
        public static readonly EventKey UiToastSuccess = new("ui.toast.success");
        public static readonly EventKey UiRefreshUpper = new("Ui.Refresh");
        public static readonly EventKey AB = new("a.b");
        public static readonly EventKey BC = new("b.c");
        public static readonly EventKey CD = new("c.d");
        public static readonly EventKey TodoCreatedValid = new("todo.created");
        public static readonly EventKey TodoDeleted = new("todo.deleted");
        public static readonly EventKey UiTodoRefreshList = new("ui.todo.refreshList");
        public static readonly EventKey UiStatsRefresh = new("ui.stats.refresh");
    }

    [Fact]
    public void Invalid_Names_Are_Flagged()
    {
        var opts = new SwapEventBusOptions()
            .Chain(TestEvents.TodoCreated, TestEvents.UiRefreshList) // invalid: uppercase + missing dots
            .Chain(TestEvents.UiToastSuccess, TestEvents.UiRefreshUpper); // invalid: uppercase segment

        var diag = opts.Validate();
        Assert.True(diag.HasErrors);
        Assert.Contains(diag.Errors, e => e.Contains("Invalid event name", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Cycles_Are_Detected()
    {
        var opts = new SwapEventBusOptions()
            .Chain(TestEvents.AB, TestEvents.BC)
            .Chain(TestEvents.BC, TestEvents.CD)
            .Chain(TestEvents.CD, TestEvents.AB); // cycle

        var diag = opts.Validate();
        Assert.True(diag.HasErrors);
        Assert.Contains(diag.Errors, e => e.Contains("Cycle detected", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Valid_Config_Passes()
    {
        var opts = new SwapEventBusOptions()
            .Chain(TestEvents.TodoCreatedValid, TestEvents.UiTodoRefreshList, TestEvents.UiStatsRefresh)
            .Chain(TestEvents.TodoDeleted, TestEvents.UiStatsRefresh);

        var diag = opts.Validate();
        Assert.False(diag.HasErrors);
    }
}