using Swap.Htmx.Attributes;

namespace SwapRedisDemo.Events;

[SwapEventSource]
public partial class AppEvents
{
    public const string UserClicked = "user.clicked";
}
