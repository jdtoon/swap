using Swap.Htmx.Events;

namespace Swap.Htmx.Realtime;

/// <summary>
/// Extension methods for integrating SSE events with the Swap event chain system.
/// </summary>
public static class SseEventChainExtensions
{
    /// <summary>
    /// Chains a domain event to an SSE broadcast event.
    /// When the trigger event is emitted, it will automatically broadcast via SSE.
    /// </summary>
    /// <param name="options">The event bus options.</param>
    /// <param name="trigger">The domain event that triggers the SSE broadcast.</param>
    /// <param name="sseEvent">The SSE event to broadcast.</param>
    /// <returns>The event bus options for chaining.</returns>
    /// <example>
    /// <code>
    /// // Define SSE event name constants
    /// public static class TaskSseEvents
    /// {
    ///     public const string Updated = "task-updated";
    ///     public const string Deleted = "task-deleted";
    /// }
    /// 
    /// public static class SseRooms
    /// {
    ///     public const string Dashboard = "dashboard";
    /// }
    /// 
    /// builder.Services.AddSwapHtmx(events =>
    /// {
    ///     events.ChainToSse(TaskEvents.StatusChanged, SseEvents.Broadcast(TaskSseEvents.Updated));
    ///     events.ChainToSse(ProjectEvents.Created, SseEvents.Room(SseRooms.Dashboard, "project-created"));
    /// });
    /// </code>
    /// </example>
    public static SwapEventBusOptions ChainToSse(this SwapEventBusOptions options, EventKey trigger, EventKey sseEvent)
    {
        return options.Chain(trigger, sseEvent);
    }

    /// <summary>
    /// Chains a domain event to multiple SSE events.
    /// When the trigger event is emitted, all SSE events will be broadcast.
    /// </summary>
    /// <param name="options">The event bus options.</param>
    /// <param name="trigger">The domain event that triggers the SSE broadcasts.</param>
    /// <param name="sseEvents">The SSE events to broadcast.</param>
    /// <returns>The event bus options for chaining.</returns>
    /// <example>
    /// <code>
    /// public static class TaskSseEvents
    /// {
    ///     public const string Updated = "task-updated";
    ///     public const string Refresh = "refresh";
    ///     public const string TaskEvents = "task-events";
    /// }
    /// 
    /// builder.Services.AddSwapHtmx(events =>
    /// {
    ///     events.ChainToSse(TaskEvents.StatusChanged, 
    ///         SseEvents.Broadcast(TaskSseEvents.Updated),
    ///         SseEvents.Room(SseRooms.Dashboard, TaskSseEvents.Refresh),
    ///         SseEvents.Subscribers(TaskSseEvents.TaskEvents));
    /// });
    /// </code>
    /// </example>
    public static SwapEventBusOptions ChainToSse(this SwapEventBusOptions options, EventKey trigger, params EventKey[] sseEvents)
    {
        return options.Chain(trigger, sseEvents);
    }

    /// <summary>
    /// Fluent builder for SSE event chains.
    /// </summary>
    /// <param name="options">The event bus options.</param>
    /// <param name="trigger">The domain event that triggers the SSE broadcasts.</param>
    /// <returns>A fluent SSE chain builder.</returns>
    /// <example>
    /// <code>
    /// builder.Services.AddSwapHtmx(events =>
    /// {
    ///     events.OnEvent(TaskEvents.StatusChanged)
    ///           .BroadcastSse(TaskSseEvents.Updated)
    ///           .ToRoom(SseRooms.Dashboard, TaskSseEvents.Refresh)
    ///           .ToSubscribers(TaskSseEvents.Notifications);
    /// });
    /// </code>
    /// </example>
    public static SseChainBuilder OnEvent(this SwapEventBusOptions options, EventKey trigger)
    {
        return new SseChainBuilder(options, trigger);
    }
}

/// <summary>
/// Fluent builder for creating SSE event chains.
/// </summary>
public sealed class SseChainBuilder
{
    private readonly SwapEventBusOptions _options;
    private readonly EventKey _trigger;
    private readonly List<EventKey> _sseEvents;

    internal SseChainBuilder(SwapEventBusOptions options, EventKey trigger)
    {
        _options = options;
        _trigger = trigger;
        _sseEvents = new List<EventKey>();
    }

    /// <summary>
    /// Adds a broadcast SSE event to the chain.
    /// </summary>
    /// <param name="eventName">The SSE event name for broadcasting.</param>
    /// <returns>The builder for chaining.</returns>
    public SseChainBuilder BroadcastSse(string eventName)
    {
        _sseEvents.Add(SseEvents.Broadcast(eventName));
        return this;
    }

    /// <summary>
    /// Adds a room-specific SSE event to the chain.
    /// </summary>
    /// <param name="room">The room to broadcast to.</param>
    /// <param name="eventName">The SSE event name.</param>
    /// <returns>The builder for chaining.</returns>
    public SseChainBuilder ToRoom(string room, string eventName)
    {
        _sseEvents.Add(SseEvents.Room(room, eventName));
        return this;
    }

    /// <summary>
    /// Adds multiple room-specific SSE events to the chain.
    /// </summary>
    /// <param name="eventName">The SSE event name.</param>
    /// <param name="rooms">The rooms to broadcast to.</param>
    /// <returns>The builder for chaining.</returns>
    public SseChainBuilder ToRooms(string eventName, params string[] rooms)
    {
        _sseEvents.Add(SseEvents.Rooms(rooms).Send(eventName));
        return this;
    }

    /// <summary>
    /// Adds a subscriber-specific SSE event to the chain.
    /// </summary>
    /// <param name="eventName">The SSE event name for subscribers.</param>
    /// <returns>The builder for chaining.</returns>
    public SseChainBuilder ToSubscribers(string eventName)
    {
        _sseEvents.Add(SseEvents.Subscribers(eventName));
        return this;
    }

    /// <summary>
    /// Adds a user-specific SSE event to the chain.
    /// </summary>
    /// <param name="userId">The user ID to broadcast to.</param>
    /// <param name="eventName">The SSE event name.</param>
    /// <returns>The builder for chaining.</returns>
    public SseChainBuilder ToUser(string userId, string eventName)
    {
        _sseEvents.Add(SseEvents.User(eventName, userId));
        return this;
    }

    /// <summary>
    /// Adds a role-specific SSE event to the chain.
    /// </summary>
    /// <param name="role">The role to broadcast to.</param>
    /// <param name="eventName">The SSE event name.</param>
    /// <returns>The builder for chaining.</returns>
    public SseChainBuilder ToRole(string role, string eventName)
    {
        _sseEvents.Add(SseEvents.Roles(eventName, role));
        return this;
    }

    /// <summary>
    /// Adds a multi-role SSE event to the chain.
    /// </summary>
    /// <param name="eventName">The SSE event name.</param>
    /// <param name="roles">The roles to broadcast to.</param>
    /// <returns>The builder for chaining.</returns>
    public SseChainBuilder ToRoles(string eventName, params string[] roles)
    {
        _sseEvents.Add(SseEvents.Roles(eventName, roles));
        return this;
    }

    /// <summary>
    /// Adds a common refresh event to the chain.
    /// </summary>
    /// <returns>The builder for chaining.</returns>
    public SseChainBuilder RefreshAll()
    {
        _sseEvents.Add(SseEvents.Common.Refresh);
        return this;
    }

    /// <summary>
    /// Adds a common update event to the chain.
    /// </summary>
    /// <returns>The builder for chaining.</returns>
    public SseChainBuilder UpdateAll()
    {
        _sseEvents.Add(SseEvents.Common.Update);
        return this;
    }

    /// <summary>
    /// Adds a toast notification event to the chain.
    /// </summary>
    /// <returns>The builder for chaining.</returns>
    public SseChainBuilder ShowToast()
    {
        _sseEvents.Add(SseEvents.Common.Toast);
        return this;
    }

    /// <summary>
    /// Completes the chain configuration.
    /// </summary>
    /// <returns>The original event bus options for further configuration.</returns>
    public SwapEventBusOptions Build()
    {
        _options.Chain(_trigger, _sseEvents.ToArray());
        return _options;
    }

    /// <summary>
    /// Implicit conversion to complete the chain configuration.
    /// </summary>
    public static implicit operator SwapEventBusOptions(SseChainBuilder builder)
    {
        return builder.Build();
    }
}