namespace Swap.Htmx.Events;

/// <summary>
/// Standard event names used by the Swap framework. Organized by domain: {domain}.{action}
/// </summary>
public static class SwapEvents
{
    public static class UI
    {
        public const string RefreshList = "ui.refreshList";
        public const string OpenModal = "ui.openModal";
        public const string CloseModal = "ui.closeModal";
        public const string ShowToast = "ui.showToast";

        // Typed EventKey accessors (backend usage preferred)
        public static EventKey RefreshListKey => new(RefreshList);
        public static EventKey OpenModalKey => new(OpenModal);
        public static EventKey CloseModalKey => new(CloseModal);
        public static EventKey ShowToastKey => new(ShowToast);
    }

    public static class Entity
    {
        public static string Created(string name) => $"{name}.created";
        public static string Updated(string name) => $"{name}.updated";
        public static string Deleted(string name) => $"{name}.deleted";

        // Typed EventKey helpers
        public static EventKey CreatedKey(string name) => new(Created(name));
        public static EventKey UpdatedKey(string name) => new(Updated(name));
        public static EventKey DeletedKey(string name) => new(Deleted(name));
    }

    public static class Auth
    {
        public const string LoggedIn = "auth.loggedIn";
        public const string LoggedOut = "auth.loggedOut";
        public const string SessionExpired = "auth.sessionExpired";

        // Typed EventKey accessors
        public static EventKey LoggedInKey => new(LoggedIn);
        public static EventKey LoggedOutKey => new(LoggedOut);
        public static EventKey SessionExpiredKey => new(SessionExpired);
    }
}
