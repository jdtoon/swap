using NetMX.Events;
using NetMX.Audit.Web.Events;

namespace NetMX.Audit.Web.Extensions;

/// <summary>
/// Extension methods for registering Audit module events.
/// </summary>
public static class AuditEventRegistryExtensions
{
    /// <summary>
    /// Registers Audit module events with the event registry.
    /// Call this during application startup after adding the event registry.
    /// </summary>
    /// <param name="registry">The event registry</param>
    /// <returns>The event registry for chaining</returns>
    public static IEventRegistry AddAuditEvents(this IEventRegistry registry)
    {
        AuditEventDefinitions.Register(registry);
        return registry;
    }
}
