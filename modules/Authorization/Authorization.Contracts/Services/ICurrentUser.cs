namespace Authorization.Contracts.Services;

/// <summary>
/// Provides access to information about the currently authenticated user.
/// This abstraction allows the permission checker to work independently of the authentication mechanism.
/// </summary>
public interface ICurrentUser
{
    /// <summary>
    /// Gets the ID of the current user
    /// </summary>
    Guid? Id { get; }
    
    /// <summary>
    /// Gets whether a user is currently authenticated
    /// </summary>
    bool IsAuthenticated { get; }
    
    /// <summary>
    /// Gets the username of the current user
    /// </summary>
    string? UserName { get; }
    
    /// <summary>
    /// Gets the roles assigned to the current user
    /// </summary>
    string[] Roles { get; }
}
