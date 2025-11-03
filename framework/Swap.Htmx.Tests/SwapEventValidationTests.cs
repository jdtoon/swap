using Swap.Htmx.Events;
using Xunit;

namespace Swap.Htmx.Tests;

public class SwapEventValidationTests
{
    [Fact]
    public void Invalid_Names_Are_Flagged()
    {
        var opts = new SwapEventBusOptions()
            .Chain("TodoCreated", "ui.refreshList") // invalid: uppercase + missing dots
            .Chain("ui.toast.success", "Ui.Refresh"); // invalid: uppercase segment

        var diag = opts.Validate();
        Assert.True(diag.HasErrors);
        Assert.Contains(diag.Errors, e => e.Contains("Invalid event name", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Cycles_Are_Detected()
    {
        var opts = new SwapEventBusOptions()
            .Chain("a.b", "b.c")
            .Chain("b.c", "c.d")
            .Chain("c.d", "a.b"); // cycle

        var diag = opts.Validate();
        Assert.True(diag.HasErrors);
        Assert.Contains(diag.Errors, e => e.Contains("Cycle detected", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Valid_Config_Passes()
    {
        var opts = new SwapEventBusOptions()
            .Chain("todo.created", "ui.todo.refreshList", "ui.stats.refresh")
            .Chain("todo.deleted", "ui.stats.refresh");

        var diag = opts.Validate();
        Assert.False(diag.HasErrors);
    }
}
