using NetMX.Events;

namespace NetMX.Events;

/// <summary>
/// Identity module domain events extending the base DomainEvents class.
/// These events enable event-driven HTMX patterns for authentication and user management.
/// </summary>
public static partial class DomainEvents
{
    /// <summary>
    /// User login/logout events
    /// </summary>
    public static class Login
    {
        /// <summary>
        /// Triggered when a user successfully logs in.
        /// Payload: { userId: Guid, userName: string }
        /// </summary>
        [EventDirection(EventDirection.Upstream)]
        public const string Success = "login.success";

        /// <summary>
        /// Triggered when a login attempt fails.
        /// Payload: { userName: string, reason: string }
        /// </summary>
        [EventDirection(EventDirection.Upstream)]
        public const string Failed = "login.failed";

        /// <summary>
        /// Triggered when a user logs out.
        /// Payload: { userId: Guid }
        /// </summary>
        [EventDirection(EventDirection.Upstream)]
        public const string Logout = "login.logout";
    }

    /// <summary>
    /// User registration events
    /// </summary>
    public static class Registration
    {
        /// <summary>
        /// Triggered when a new user account is created.
        /// Payload: { userId: Guid, email: string, userName: string }
        /// </summary>
        [EventDirection(EventDirection.Upstream)]
        public const string Success = "registration.success";

        /// <summary>
        /// Triggered when registration fails validation.
        /// Payload: { email: string, errors: string[] }
        /// </summary>
        [EventDirection(EventDirection.Upstream)]
        public const string Failed = "registration.failed";

        /// <summary>
        /// Triggered when user confirms their email.
        /// Payload: { userId: Guid }
        /// </summary>
        [EventDirection(EventDirection.Downstream)]
        public const string EmailConfirmed = "registration.email-confirmed";
    }

    /// <summary>
    /// User profile events
    /// </summary>
    public static class Profile
    {
        /// <summary>
        /// Triggered when user profile is updated.
        /// Payload: { userId: Guid, changedFields: string[] }
        /// </summary>
        [EventDirection(EventDirection.Upstream)]
        public const string Updated = "profile.updated";

        /// <summary>
        /// Triggered when user changes their password.
        /// Payload: { userId: Guid }
        /// </summary>
        [EventDirection(EventDirection.Upstream)]
        public const string PasswordChanged = "profile.password-changed";

        /// <summary>
        /// Triggered when user updates their email address.
        /// Payload: { userId: Guid, oldEmail: string, newEmail: string }
        /// </summary>
        [EventDirection(EventDirection.Upstream)]
        public const string EmailChanged = "profile.email-changed";
    }

    /// <summary>
    /// User account events
    /// </summary>
    public static class Account
    {
        /// <summary>
        /// Triggered when an account is locked (too many failed login attempts).
        /// Payload: { userId: Guid, lockoutEnd: DateTime }
        /// </summary>
        [EventDirection(EventDirection.Downstream)]
        public const string Locked = "account.locked";

        /// <summary>
        /// Triggered when an account lockout is lifted.
        /// Payload: { userId: Guid }
        /// </summary>
        [EventDirection(EventDirection.Downstream)]
        public const string Unlocked = "account.unlocked";

        /// <summary>
        /// Triggered when two-factor authentication is enabled.
        /// Payload: { userId: Guid }
        /// </summary>
        [EventDirection(EventDirection.Upstream)]
        public const string TwoFactorEnabled = "account.2fa-enabled";

        /// <summary>
        /// Triggered when two-factor authentication is disabled.
        /// Payload: { userId: Guid }
        /// </summary>
        [EventDirection(EventDirection.Upstream)]
        public const string TwoFactorDisabled = "account.2fa-disabled";
    }

    /// <summary>
    /// User session events
    /// </summary>
    public static class Session
    {
        /// <summary>
        /// Triggered when a user's session expires.
        /// Payload: { userId: Guid, sessionId: string }
        /// </summary>
        [EventDirection(EventDirection.Downstream)]
        public const string Expired = "session.expired";

        /// <summary>
        /// Triggered when a user is forcibly logged out (e.g., by admin).
        /// Payload: { userId: Guid, reason: string }
        /// </summary>
        [EventDirection(EventDirection.Downstream)]
        public const string Terminated = "session.terminated";
    }

    /// <summary>
    /// User role assignment events
    /// </summary>
    public static class UserRole
    {
        /// <summary>
        /// Triggered when a role is assigned to a user.
        /// Payload: { userId: Guid, roleId: Guid, roleName: string }
        /// </summary>
        [EventDirection(EventDirection.Upstream)]
        public const string Assigned = "user-role.assigned";

        /// <summary>
        /// Triggered when a role is removed from a user.
        /// Payload: { userId: Guid, roleId: Guid, roleName: string }
        /// </summary>
        [EventDirection(EventDirection.Upstream)]
        public const string Removed = "user-role.removed";
    }
}
