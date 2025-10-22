using NetMX.Events;
using NetMX.Identity.Web.Events;

namespace NetMX.Identity.Web.Extensions;

/// <summary>
/// Extension methods for registering Identity module events.
/// </summary>
public static class IdentityEventRegistryExtensions
{
    /// <summary>
    /// Registers Identity module events with the event registry.
    /// Call this during application startup after adding the event registry.
    /// </summary>
    /// <param name="registry">The event registry</param>
    /// <returns>The event registry for chaining</returns>
    public static IEventRegistry AddIdentityEvents(this IEventRegistry registry)
    {
        IdentityEventDefinitions.Register(registry);
        return registry;
    }
}
