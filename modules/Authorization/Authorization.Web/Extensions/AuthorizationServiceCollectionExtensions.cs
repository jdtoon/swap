using Authorization.Web.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Authorization.Web.Extensions;

/// <summary>
/// Extension methods for configuring permission-based authorization in the service collection.
/// </summary>
public static class AuthorizationServiceCollectionExtensions
{
    /// <summary>
    /// Adds permission-based authorization to the service collection.
    /// This includes:
    /// - Dynamic policy provider for [RequirePermission] attributes
    /// - Authorization handlers for single, all, and any permission checks
    /// - Full observability with distributed tracing and logging
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddPermissionAuthorization(this IServiceCollection services)
    {
        // Register the dynamic policy provider
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();

        // Register authorization handlers
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
        services.AddScoped<IAuthorizationHandler, AllPermissionsAuthorizationHandler>();
        services.AddScoped<IAuthorizationHandler, AnyPermissionsAuthorizationHandler>();

        return services;
    }
}
