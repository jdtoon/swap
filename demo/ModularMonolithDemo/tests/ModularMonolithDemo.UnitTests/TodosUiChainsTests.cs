using System;
using System.Linq;
using ModularMonolithDemo.Modules.Todos.Contracts;
using ModularMonolithDemo.Modules.Todos.Web.Events;
using Swap.Htmx.Events;
using Xunit;

namespace ModularMonolithDemo.UnitTests;

public class TodosUiChainsTests
{
    [Fact]
    public void Configure_ShouldRegisterExpectedChains()
    {
        var opts = new SwapEventBusOptions();

        // Act
        TodosUiChains.Configure(opts);

        var map = opts.GetChainsSnapshot();

        // Assert triggers exist
        Assert.Contains(TodoEvents.Domain.Created, map.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Contains(TodoEvents.Domain.Deleted, map.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.Contains(TodoEvents.Domain.Toggled, map.Keys, StringComparer.OrdinalIgnoreCase);

        // created/deleted chain to four UI events
        Assert.True(map[TodoEvents.Domain.Created].Contains(TodoEvents.Ui.RefreshList));
        Assert.True(map[TodoEvents.Domain.Created].Contains(TodoEvents.Ui.ToastSuccess));
        Assert.True(map[TodoEvents.Domain.Created].Contains(TodoEvents.Ui.StatsRefresh));
        Assert.True(map[TodoEvents.Domain.Created].Contains(TodoEvents.Ui.ActivityAppend));

        Assert.True(map[TodoEvents.Domain.Deleted].Contains(TodoEvents.Ui.RefreshList));
        Assert.True(map[TodoEvents.Domain.Deleted].Contains(TodoEvents.Ui.ToastSuccess));
        Assert.True(map[TodoEvents.Domain.Deleted].Contains(TodoEvents.Ui.StatsRefresh));
        Assert.True(map[TodoEvents.Domain.Deleted].Contains(TodoEvents.Ui.ActivityAppend));

        // toggled only refreshes stats
        Assert.Single(map[TodoEvents.Domain.Toggled]);
        Assert.Contains(TodoEvents.Ui.StatsRefresh, map[TodoEvents.Domain.Toggled]);
    }

    [Theory]
    [InlineData(TodoEvents.Domain.Created)]
    [InlineData(TodoEvents.Domain.Deleted)]
    [InlineData(TodoEvents.Domain.Toggled)]
    public void Domain_Event_Names_Are_Lowercase_With_Dots(string name)
    {
        // ^[a-z][a-z0-9]*(\.[a-z][A-Za-z0-9]*)+$
        var pattern = new System.Text.RegularExpressions.Regex("^[a-z][a-z0-9]*(\\.[a-z][A-Za-z0-9]*)+$");
        Assert.Matches(pattern, name);
    }
}
