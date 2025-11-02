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
    }

    public static class Entity
    {
        public static string Created(string name) => $"{name}.created";
        public static string Updated(string name) => $"{name}.updated";
        public static string Deleted(string name) => $"{name}.deleted";
    }

    public static class Auth
    {
        public const string LoggedIn = "auth.loggedIn";
        public const string LoggedOut = "auth.loggedOut";
        public const string SessionExpired = "auth.sessionExpired";
    }
}
