using Swap.Htmx.Events;

namespace EventSystemDemo;

public static class DemoEvents
{
    public static readonly EventKey Pre = new("pre");
    public static readonly EventKey PreOnly = new("preOnly");
}
