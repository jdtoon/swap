namespace Swap.Htmx.Events;

/// <summary>
/// Example event catalog showing recommended patterns for type-safe event definitions.
/// Applications should create their own static classes following these patterns.
/// </summary>
/// <example>
/// <code>
/// // In your application, define domain-specific events:
/// public static class ProductEvents
/// {
///     public static readonly EventKey Created = new("product.created");
///     public static readonly EventKey Updated = new("product.updated");
///     public static readonly EventKey Deleted = new("product.deleted");
///     public static readonly EventKey PriceChanged = new("product.priceChanged");
/// }
/// 
/// public static class CartEvents
/// {
///     public static readonly EventKey ItemAdded = new("cart.itemAdded");
///     public static readonly EventKey ItemRemoved = new("cart.itemRemoved");
///     public static readonly EventKey Cleared = new("cart.cleared");
///     public static readonly EventKey CheckoutStarted = new("cart.checkoutStarted");
/// }
/// 
/// // Then use in your controllers:
/// events.Emit(ProductEvents.Created);
/// events.Emit(CartEvents.ItemAdded);
/// </code>
/// </example>
public static class SwapEvents
{
    /// <summary>
    /// UI-specific events for client-side interactions.
    /// These follow the pattern: ui.{action}
    /// </summary>
    public static class UI
    {
        public static readonly EventKey RefreshList = new("ui.refreshList");
        public static readonly EventKey RefreshPage = new("ui.refreshPage");
        public static readonly EventKey OpenModal = new("ui.openModal");
        public static readonly EventKey CloseModal = new("ui.closeModal");
        public static readonly EventKey ShowToast = new("ui.showToast");
        public static readonly EventKey HideToast = new("ui.hideToast");
        public static readonly EventKey ShowSpinner = new("ui.showSpinner");
        public static readonly EventKey HideSpinner = new("ui.hideSpinner");
        public static readonly EventKey ScrollToTop = new("ui.scrollToTop");
        public static readonly EventKey UpdateCounter = new("ui.updateCounter");
    }

    /// <summary>
    /// Generic entity lifecycle events.
    /// For specific domains, prefer creating dedicated event classes (e.g., ProductEvents, OrderEvents).
    /// </summary>
    public static class Entity
    {
        /// <summary>
        /// Creates a type-safe event key for entity creation.
        /// Prefer defining specific events like ProductEvents.Created over using this helper.
        /// </summary>
        public static EventKey Created(string entityName) => new($"{entityName}.created");
        
        /// <summary>
        /// Creates a type-safe event key for entity updates.
        /// Prefer defining specific events like ProductEvents.Updated over using this helper.
        /// </summary>
        public static EventKey Updated(string entityName) => new($"{entityName}.updated");
        
        /// <summary>
        /// Creates a type-safe event key for entity deletion.
        /// Prefer defining specific events like ProductEvents.Deleted over using this helper.
        /// </summary>
        public static EventKey Deleted(string entityName) => new($"{entityName}.deleted");
    }

    /// <summary>
    /// Authentication and session events.
    /// These follow the pattern: auth.{action}
    /// </summary>
    public static class Auth
    {
        public static readonly EventKey LoggedIn = new("auth.loggedIn");
        public static readonly EventKey LoggedOut = new("auth.loggedOut");
        public static readonly EventKey SessionExpired = new("auth.sessionExpired");
        public static readonly EventKey PasswordChanged = new("auth.passwordChanged");
        public static readonly EventKey ProfileUpdated = new("auth.profileUpdated");
    }

    /// <summary>
    /// Form validation and submission events.
    /// These follow the pattern: form.{action}
    /// </summary>
    public static class Form
    {
        public static readonly EventKey ValidationFailed = new("form.validationFailed");
        public static readonly EventKey ValidationPassed = new("form.validationPassed");
        public static readonly EventKey Submitted = new("form.submitted");
        public static readonly EventKey Reset = new("form.reset");
    }

    /// <summary>
    /// Notification events for user feedback.
    /// These follow the pattern: notification.{type}
    /// </summary>
    public static class Notification
    {
        public static readonly EventKey Success = new("notification.success");
        public static readonly EventKey Error = new("notification.error");
        public static readonly EventKey Warning = new("notification.warning");
        public static readonly EventKey Info = new("notification.info");
    }
}
