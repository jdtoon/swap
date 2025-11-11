namespace Swap.Htmx.Events;

/// <summary>
/// Example event patterns for apps to follow when defining their own event classes.
/// Apps should create their own static event classes (e.g., ProductEvents, OrderEvents)
/// following these naming conventions: {domain}.{action}
/// </summary>
public static class SwapEvents
{
    /// <summary>
    /// Example UI events for client-side actions.
    /// Apps define their own UI events specific to their needs.
    /// </summary>
    public static class UI
    {
        public static readonly EventKey RefreshList = new("ui.refreshList");
        public static readonly EventKey OpenModal = new("ui.openModal");
        public static readonly EventKey CloseModal = new("ui.closeModal");
        public static readonly EventKey ShowToast = new("ui.showToast");
    }

    /// <summary>
    /// Example entity event helpers for domain events.
    /// Apps define their own entity-specific events (e.g., ProductEvents.Created).
    /// </summary>
    public static class Entity
    {
        public static EventKey Created(string name) => new($"{name}.created");
        public static EventKey Updated(string name) => new($"{name}.updated");
        public static EventKey Deleted(string name) => new($"{name}.deleted");
    }

    /// <summary>
    /// Example authentication events.
    /// Apps define their own auth events based on their security requirements.
    /// </summary>
    public static class Auth
    {
        public static readonly EventKey LoggedIn = new("auth.loggedIn");
        public static readonly EventKey LoggedOut = new("auth.loggedOut");
        public static readonly EventKey SessionExpired = new("auth.sessionExpired");
    }
}
