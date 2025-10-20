using System.Diagnostics;
using Authorization.Contracts.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace Authorization.Web.Authorization;

/// <summary>
/// Authorization handler that checks if the user has the required permission.
/// Integrates with the PermissionChecker service and includes full observability.
/// </summary>
public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly IPermissionChecker _permissionChecker;
    private readonly ILogger<PermissionAuthorizationHandler> _logger;
    private static readonly ActivitySource ActivitySource = new("NetMX.Authorization.Handler");

    public PermissionAuthorizationHandler(
        IPermissionChecker permissionChecker,
        ILogger<PermissionAuthorizationHandler> logger)
    {
        _permissionChecker = permissionChecker;
        _logger = logger;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        using var activity = ActivitySource.StartActivity("HandlePermissionRequirement");
        activity?.SetTag("permission.name", requirement.Permission);
        activity?.SetTag("user.authenticated", context.User?.Identity?.IsAuthenticated);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            if (context.User?.Identity?.IsAuthenticated != true)
            {
                _logger.LogDebug(
                    "Permission {Permission} denied: User not authenticated",
                    requirement.Permission);

                activity?.SetTag("authorization.result", "denied_unauthenticated");
                return;
            }

            var hasPermission = await _permissionChecker.IsGrantedAsync(requirement.Permission);

            stopwatch.Stop();

            if (hasPermission)
            {
                context.Succeed(requirement);

                _logger.LogInformation(
                    "Permission {Permission} granted in {DurationMs}ms",
                    requirement.Permission, stopwatch.ElapsedMilliseconds);

                activity?.SetTag("authorization.result", "granted");
                activity?.SetTag("duration.ms", stopwatch.ElapsedMilliseconds);
            }
            else
            {
                _logger.LogWarning(
                    "Permission {Permission} denied: User does not have permission. Duration: {DurationMs}ms",
                    requirement.Permission, stopwatch.ElapsedMilliseconds);

                activity?.SetTag("authorization.result", "denied_no_permission");
                activity?.SetTag("duration.ms", stopwatch.ElapsedMilliseconds);
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(ex,
                "Permission check failed for {Permission} after {DurationMs}ms",
                requirement.Permission, stopwatch.ElapsedMilliseconds);

            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);

            // Fail closed: Don't grant access if permission check throws
            // context.Fail() is implicit if we don't call context.Succeed()
        }
    }
}
