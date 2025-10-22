namespace NetMX.Events;

/// <summary>
/// Identity module events (partial extension of global Events class).
/// </summary>
public static partial class Events
{
    /// <summary>
    /// User-related events from Identity module.
    /// </summary>
    public static class User
    {
        /// <summary>Event: "user.registered"</summary>
        public const string Registered = "user.registered";
        
        /// <summary>Event: "user.profile.updated"</summary>
        public const string ProfileUpdated = "user.profile.updated";
        
        /// <summary>Event: "user.email.confirmed"</summary>
        public const string EmailConfirmed = "user.email.confirmed";
        
        /// <summary>Event: "user.deleted"</summary>
        public const string Deleted = "user.deleted";
        
        /// <summary>
        /// Login-related sub-events.
        /// </summary>
        public static class Login
        {
            /// <summary>Event: "user.login.success"</summary>
            public const string Success = "user.login.success";
            
            /// <summary>Event: "user.login.failed"</summary>
            public const string Failed = "user.login.failed";
            
            /// <summary>Event: "user.login.lockedout"</summary>
            public const string LockedOut = "user.login.lockedout";
            
            /// <summary>Event: "user.login.twofactor.required"</summary>
            public const string TwoFactorRequired = "user.login.twofactor.required";
        }
        
        /// <summary>
        /// Session-related sub-events.
        /// </summary>
        public static class Session
        {
            /// <summary>Event: "user.session.created"</summary>
            public const string Created = "user.session.created";
            
            /// <summary>Event: "user.session.renewed"</summary>
            public const string Renewed = "user.session.renewed";
            
            /// <summary>Event: "user.session.expired"</summary>
            public const string Expired = "user.session.expired";
        }
        
        /// <summary>
        /// Password-related sub-events.
        /// </summary>
        public static class Password
        {
            /// <summary>Event: "user.password.changed"</summary>
            public const string Changed = "user.password.changed";
            
            /// <summary>Event: "user.password.reset.requested"</summary>
            public const string ResetRequested = "user.password.reset.requested";
            
            /// <summary>Event: "user.password.reset.completed"</summary>
            public const string ResetCompleted = "user.password.reset.completed";
        }
        
        /// <summary>
        /// Account-related sub-events.
        /// </summary>
        public static class Account
        {
            /// <summary>Event: "user.account.locked"</summary>
            public const string Locked = "user.account.locked";
            
            /// <summary>Event: "user.account.unlocked"</summary>
            public const string Unlocked = "user.account.unlocked";
        }
    }
}
