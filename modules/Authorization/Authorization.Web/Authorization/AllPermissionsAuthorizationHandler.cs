using System.Diagnostics;
using Authorization.Contracts.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace Authorization.Web.Authorization;

/// <summary>
/// Authorization handler that checks if the user has ALL of the required permissions.
/// Integrates with the PermissionChecker service and includes full observability.
/// </summary>
public class AllPermissionsAuthorizationHandler : AuthorizationHandler<AllPermissionsRequirement>
{
    private readonly IPermissionChecker _permissionChecker;
    private readonly ILogger<AllPermissionsAuthorizationHandler> _logger;
    private static readonly ActivitySource ActivitySource = new("NetMX.Authorization.Handler");

    public AllPermissionsAuthorizationHandler(
        IPermissionChecker permissionChecker,
        ILogger<AllPermissionsAuthorizationHandler> logger)
    {
        _permissionChecker = permissionChecker;
        _logger = logger;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        AllPermissionsRequirement requirement)
    {
        using var activity = ActivitySource.StartActivity("HandleAllPermissionsRequirement");
        activity?.SetTag("permissions.count", requirement.Permissions.Length);
        activity?.SetTag("permissions.list", string.Join(", ", requirement.Permissions));
        activity?.SetTag("user.authenticated", context.User?.Identity?.IsAuthenticated);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            if (context.User?.Identity?.IsAuthenticated != true)
            {
                _logger.LogDebug(
                    "Permissions {Permissions} denied: User not authenticated",
                    string.Join(", ", requirement.Permissions));

                activity?.SetTag("authorization.result", "denied_unauthenticated");
                return;
            }

            var hasAllPermissions = await _permissionChecker.IsGrantedAllAsync(requirement.Permissions);

            stopwatch.Stop();

            if (hasAllPermissions)
            {
                context.Succeed(requirement);

                _logger.LogInformation(
                    "All permissions {Permissions} granted in {DurationMs}ms",
                    string.Join(", ", requirement.Permissions), stopwatch.ElapsedMilliseconds);

                activity?.SetTag("authorization.result", "granted");
                activity?.SetTag("duration.ms", stopwatch.ElapsedMilliseconds);
            }
            else
            {
                _logger.LogWarning(
                    "Not all permissions granted. Required: {Permissions}. Duration: {DurationMs}ms",
                    string.Join(", ", requirement.Permissions), stopwatch.ElapsedMilliseconds);

                activity?.SetTag("authorization.result", "denied_missing_permissions");
                activity?.SetTag("duration.ms", stopwatch.ElapsedMilliseconds);
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(ex,
                "Permission check failed for {Permissions} after {DurationMs}ms",
                string.Join(", ", requirement.Permissions), stopwatch.ElapsedMilliseconds);

            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
        }
    }
}
