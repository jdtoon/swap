using Swap.Htmx.Attributes;

namespace SwapWebSockets.Events;

[SwapEventSource]
public partial class AppEvents
{
    public const string UserClicked = "user.clicked";
}
