using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Swap.Htmx.Attributes;

namespace Swap.Htmx.Events;

/// <summary>
/// Registry for discovered Swap event handlers.
/// </summary>
public class SwapEventHandlerRegistry
{
    private readonly Dictionary<Type, List<HandlerDescriptor>> _handlers = new();

    /// <summary>
    /// Scans assemblies for handlers and registers them.
    /// </summary>
    public void ScanAndRegisterHandlers(IServiceCollection services, params Assembly[] assemblies)
    {
        foreach (var assembly in assemblies)
        {
            foreach (var type in assembly.GetTypes())
            {
                if (!type.IsClass || type.IsAbstract) continue;

                var handlerInterface = type.GetInterfaces()
                    .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ISwapEventHandler<>));

                if (handlerInterface == null) continue;

                var eventType = handlerInterface.GetGenericArguments()[0];
                var attribute = type.GetCustomAttribute<SwapHandlerAttribute>();
                var priority = attribute?.Priority ?? 0;

                if (!_handlers.TryGetValue(eventType, out var list))
                {
                    list = new List<HandlerDescriptor>();
                    _handlers[eventType] = list;
                }

                list.Add(new HandlerDescriptor(type, priority));
                services.AddScoped(type); // Register handler as scoped
            }
        }

        // Sort by priority
        foreach (var list in _handlers.Values)
        {
            list.Sort((a, b) => a.Priority.CompareTo(b.Priority));
        }
    }

    /// <summary>
    /// Gets handlers for the specified event type.
    /// </summary>
    public IReadOnlyList<HandlerDescriptor> GetHandlers(Type eventType)
    {
        return _handlers.TryGetValue(eventType, out var list) ? list : Array.Empty<HandlerDescriptor>();
    }

    public record HandlerDescriptor(Type HandlerType, int Priority);
}