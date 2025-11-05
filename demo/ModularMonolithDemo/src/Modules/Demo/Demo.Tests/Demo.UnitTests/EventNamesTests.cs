using ModularMonolithDemo.Modules.Demo.Contracts;
using Xunit;

namespace ModularMonolithDemo.Modules.Demo.UnitTests;

public class EventNamesTests
{
    [Theory]
    [InlineData(EventNames.Ui.ToastSuccess)]
    [InlineData(EventNames.Ui.StatsRefresh)]
    [InlineData(EventNames.Ui.ActivityAppend)]
    [InlineData(EventNames.Ui.DetailsRefresh)]
    [InlineData(EventNames.Ui.SummaryRefresh)]
    [InlineData(EventNames.Ui.ComponentARefresh)]
    [InlineData(EventNames.Ui.ComponentBRefresh)]
    public void Ui_Event_Names_Are_Lowercase_With_Dots(string name)
    {
        var pattern = new System.Text.RegularExpressions.Regex("^[a-z][a-z0-9]*(\\.[a-z][A-Za-z0-9]*)+$");
        Assert.Matches(pattern, name);
    }
}
