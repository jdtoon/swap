using Swap.Htmx.Attributes;

namespace SwapMvc.Events;

[SwapEventSource]
public partial class AppEvents
{
    public const string UserClicked = "user.clicked";
}
