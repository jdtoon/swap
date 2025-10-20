using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Authorization.Web.Authorization;

/// <summary>
/// Dynamic authorization policy provider that creates policies on-the-fly for permission-based authorization.
/// This allows [RequirePermission("Users.View")] to work without pre-registering every policy.
/// </summary>
public class PermissionPolicyProvider : IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider _fallbackPolicyProvider;
    
    private const string PermissionPolicyPrefix = "Permission:";
    private const string PermissionsAllPolicyPrefix = "PermissionsAll:";
    private const string PermissionsAnyPolicyPrefix = "PermissionsAny:";

    public PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
    {
        _fallbackPolicyProvider = new DefaultAuthorizationPolicyProvider(options);
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync()
    {
        return _fallbackPolicyProvider.GetDefaultPolicyAsync();
    }

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync()
    {
        return _fallbackPolicyProvider.GetFallbackPolicyAsync();
    }

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        // Handle single permission: "Permission:Users.View"
        if (policyName.StartsWith(PermissionPolicyPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var permission = policyName[PermissionPolicyPrefix.Length..];
            var policy = new AuthorizationPolicyBuilder()
                .AddRequirements(new PermissionRequirement(permission))
                .Build();
            
            return Task.FromResult<AuthorizationPolicy?>(policy);
        }

        // Handle all permissions: "PermissionsAll:Users.View,Users.Edit"
        if (policyName.StartsWith(PermissionsAllPolicyPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var permissionsString = policyName[PermissionsAllPolicyPrefix.Length..];
            var permissions = permissionsString.Split(',', StringSplitOptions.RemoveEmptyEntries);
            
            var policy = new AuthorizationPolicyBuilder()
                .AddRequirements(new AllPermissionsRequirement(permissions))
                .Build();
            
            return Task.FromResult<AuthorizationPolicy?>(policy);
        }

        // Handle any permissions: "PermissionsAny:Users.View,Users.Edit"
        if (policyName.StartsWith(PermissionsAnyPolicyPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var permissionsString = policyName[PermissionsAnyPolicyPrefix.Length..];
            var permissions = permissionsString.Split(',', StringSplitOptions.RemoveEmptyEntries);
            
            var policy = new AuthorizationPolicyBuilder()
                .AddRequirements(new AnyPermissionsRequirement(permissions))
                .Build();
            
            return Task.FromResult<AuthorizationPolicy?>(policy);
        }

        // Fall back to default provider for non-permission policies
        return _fallbackPolicyProvider.GetPolicyAsync(policyName);
    }
}
