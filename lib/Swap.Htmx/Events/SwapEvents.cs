namespace Swap.Htmx.Events;

/// <summary>
/// Example event patterns for apps to follow when defining their own event
/// classes. These are purely convenience helpers; most real applications
/// should define their own static classes (for example
/// <c>ProductEvents.Created</c>, <c>OrderEvents.Updated</c>) following the
/// <c>{domain}.{action}</c> convention.
/// </summary>
public static class SwapEvents
{
    /// <summary>
    /// Example UI events for client-side actions. Your application can
    /// either use these directly or copy the pattern and define its own
    /// event catalog that better reflects your domain.
    /// </summary>
    public static class UI
    {
        public static readonly EventKey RefreshList = new("ui.refreshList");
        public static readonly EventKey OpenModal = new("ui.openModal");
        public static readonly EventKey CloseModal = new("ui.closeModal");
        public static readonly EventKey ShowToast = new("ui.showToast");
    }

    /// <summary>
    /// Example helpers for generating domain events. Most applications will
    /// create their own strongly named helpers instead of passing raw
    /// strings to this method.
    /// </summary>
    public static class Entity
    {
        public static EventKey Created(string name) => new($"{name}.created");
        public static EventKey Updated(string name) => new($"{name}.updated");
        public static EventKey Deleted(string name) => new($"{name}.deleted");
    }

    /// <summary>
    /// Example authentication events. Treat these as suggestions; feel free
    /// to replace them with application-specific events.
    /// </summary>
    public static class Auth
    {
        public static readonly EventKey LoggedIn = new("auth.loggedIn");
        public static readonly EventKey LoggedOut = new("auth.loggedOut");
        public static readonly EventKey SessionExpired = new("auth.sessionExpired");
    }
}
