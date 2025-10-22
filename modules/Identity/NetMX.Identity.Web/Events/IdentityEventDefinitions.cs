using NetMX.Events;

namespace NetMX.Identity.Web.Events;

/// <summary>
/// Defines and registers all Identity module events.
/// </summary>
public static class IdentityEventDefinitions
{
    /// <summary>
    /// Registers all Identity events with the event registry.
    /// </summary>
    /// <param name="registry">The event registry to register events with.</param>
    public static void Register(IEventRegistry registry)
    {
        // User events
        registry.RegisterEvent(IdentityEvents.User.Registered, new EventMetadata
        {
            Name = IdentityEvents.User.Registered,
            Module = "Identity",
            Category = "User",
            Direction = EventDirection.Upstream,
            Description = "Triggered when a new user registers. Payload: { userId: Guid, email: string, userName: string }"
        });
        
        registry.RegisterEvent(IdentityEvents.User.ProfileUpdated, new EventMetadata
        {
            Name = IdentityEvents.User.ProfileUpdated,
            Module = "Identity",
            Category = "User",
            Direction = EventDirection.Downstream,
            Description = "Triggered when a user updates their profile. Payload: { userId: Guid, changes: string[] }"
        });
        
        registry.RegisterEvent(IdentityEvents.User.EmailConfirmed, new EventMetadata
        {
            Name = IdentityEvents.User.EmailConfirmed,
            Module = "Identity",
            Category = "User",
            Direction = EventDirection.Downstream,
            Description = "Triggered when a user confirms their email. Payload: { userId: Guid, email: string }"
        });
        
        registry.RegisterEvent(IdentityEvents.User.Deleted, new EventMetadata
        {
            Name = IdentityEvents.User.Deleted,
            Module = "Identity",
            Category = "User",
            Direction = EventDirection.Terminal,
            Description = "Triggered when a user is deleted. Payload: { userId: Guid, email: string }"
        });
        
        // Login events
        registry.RegisterEvent(IdentityEvents.Login.Success, new EventMetadata
        {
            Name = IdentityEvents.Login.Success,
            Module = "Identity",
            Category = "Login",
            Direction = EventDirection.Downstream,
            Description = "Triggered when a user successfully logs in. Payload: { userId: Guid, userName: string, ipAddress: string }"
        });
        
        registry.RegisterEvent(IdentityEvents.Login.Failed, new EventMetadata
        {
            Name = IdentityEvents.Login.Failed,
            Module = "Identity",
            Category = "Login",
            Direction = EventDirection.Downstream,
            Description = "Triggered when a login attempt fails. Payload: { userName: string, reason: string, ipAddress: string }"
        });
        
        registry.RegisterEvent(IdentityEvents.Login.LockedOut, new EventMetadata
        {
            Name = IdentityEvents.Login.LockedOut,
            Module = "Identity",
            Category = "Login",
            Direction = EventDirection.Downstream,
            Description = "Triggered when a user is locked out. Payload: { userId: Guid, userName: string, lockoutEnd: DateTime }"
        });
        
        registry.RegisterEvent(IdentityEvents.Login.TwoFactorRequired, new EventMetadata
        {
            Name = IdentityEvents.Login.TwoFactorRequired,
            Module = "Identity",
            Category = "Login",
            Direction = EventDirection.Downstream,
            Description = "Triggered when two-factor authentication is required. Payload: { userId: Guid, userName: string }"
        });
        
        // Session events
        registry.RegisterEvent(IdentityEvents.Session.Created, new EventMetadata
        {
            Name = IdentityEvents.Session.Created,
            Module = "Identity",
            Category = "Session",
            Direction = EventDirection.Downstream,
            Description = "Triggered when a new session is created. Payload: { sessionId: Guid, userId: Guid, ipAddress: string }"
        });
        
        registry.RegisterEvent(IdentityEvents.Session.Renewed, new EventMetadata
        {
            Name = IdentityEvents.Session.Renewed,
            Module = "Identity",
            Category = "Session",
            Direction = EventDirection.Downstream,
            Description = "Triggered when a session is renewed. Payload: { sessionId: Guid, userId: Guid }"
        });
        
        registry.RegisterEvent(IdentityEvents.Session.Expired, new EventMetadata
        {
            Name = IdentityEvents.Session.Expired,
            Module = "Identity",
            Category = "Session",
            Direction = EventDirection.Terminal,
            Description = "Triggered when a session expires. Payload: { sessionId: Guid, userId: Guid }"
        });
        
        // Password events
        registry.RegisterEvent(IdentityEvents.Password.Changed, new EventMetadata
        {
            Name = IdentityEvents.Password.Changed,
            Module = "Identity",
            Category = "Password",
            Direction = EventDirection.Downstream,
            Description = "Triggered when a user changes their password. Payload: { userId: Guid, changedAt: DateTime }"
        });
        
        registry.RegisterEvent(IdentityEvents.Password.ResetRequested, new EventMetadata
        {
            Name = IdentityEvents.Password.ResetRequested,
            Module = "Identity",
            Category = "Password",
            Direction = EventDirection.Downstream,
            Description = "Triggered when a password reset is requested. Payload: { userId: Guid, email: string, token: string }"
        });
        
        registry.RegisterEvent(IdentityEvents.Password.ResetCompleted, new EventMetadata
        {
            Name = IdentityEvents.Password.ResetCompleted,
            Module = "Identity",
            Category = "Password",
            Direction = EventDirection.Downstream,
            Description = "Triggered when a password reset is completed. Payload: { userId: Guid, completedAt: DateTime }"
        });
        
        // Account events
        registry.RegisterEvent(IdentityEvents.Account.Locked, new EventMetadata
        {
            Name = IdentityEvents.Account.Locked,
            Module = "Identity",
            Category = "Account",
            Direction = EventDirection.Downstream,
            Description = "Triggered when an account is locked. Payload: { userId: Guid, reason: string, lockedBy: Guid }"
        });
        
        registry.RegisterEvent(IdentityEvents.Account.Unlocked, new EventMetadata
        {
            Name = IdentityEvents.Account.Unlocked,
            Module = "Identity",
            Category = "Account",
            Direction = EventDirection.Downstream,
            Description = "Triggered when an account is unlocked. Payload: { userId: Guid, unlockedBy: Guid }"
        });
    }
}

/// <summary>
/// Type-safe event name constants for Identity module.
/// Use these instead of magic strings for IntelliSense support.
/// </summary>
public static class IdentityEvents
{
    /// <summary>
    /// User-related events.
    /// </summary>
    public static class User
    {
        public const string Registered = "user.registered";
        public const string ProfileUpdated = "user.profile.updated";
        public const string EmailConfirmed = "user.email.confirmed";
        public const string Deleted = "user.deleted";
    }
    
    /// <summary>
    /// Login-related events.
    /// </summary>
    public static class Login
    {
        public const string Success = "user.login.success";
        public const string Failed = "user.login.failed";
        public const string LockedOut = "user.login.lockedout";
        public const string TwoFactorRequired = "user.login.twofactor.required";
    }
    
    /// <summary>
    /// Session-related events.
    /// </summary>
    public static class Session
    {
        public const string Created = "user.session.created";
        public const string Renewed = "user.session.renewed";
        public const string Expired = "user.session.expired";
    }
    
    /// <summary>
    /// Password-related events.
    /// </summary>
    public static class Password
    {
        public const string Changed = "user.password.changed";
        public const string ResetRequested = "user.password.reset.requested";
        public const string ResetCompleted = "user.password.reset.completed";
    }
    
    /// <summary>
    /// Account-related events.
    /// </summary>
    public static class Account
    {
        public const string Locked = "user.account.locked";
        public const string Unlocked = "user.account.unlocked";
    }
}
