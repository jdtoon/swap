using System.Diagnostics;
using Authorization.Contracts.Services;
using Authorization.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using NetMX.DependencyInjection;
using NetMX.Ddd.Domain.Repositories;

namespace Authorization.Application.Services;

/// <summary>
/// Service for checking user permissions with caching and observability.
/// Implements the core authorization logic for the application.
/// </summary>
public class PermissionChecker : IPermissionChecker, IScopedDependency
{
    private readonly ILogger<PermissionChecker> _logger;
    private readonly IQueryableRepository<RolePermission, Guid> _rolePermissionRepository;
    private readonly IQueryableRepository<Permission, Guid> _permissionRepository;
    private readonly ICurrentUser _currentUser;
    private readonly IMemoryCache _cache;
    
    private static readonly ActivitySource ActivitySource = new("NetMX.Authorization");
    private const string CacheKeyPrefix = "Authorization:Permissions:User:";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(15);

    public PermissionChecker(
        ILogger<PermissionChecker> logger,
        IQueryableRepository<RolePermission, Guid> rolePermissionRepository,
        IQueryableRepository<Permission, Guid> permissionRepository,
        ICurrentUser currentUser,
        IMemoryCache cache)
    {
        _logger = logger;
        _rolePermissionRepository = rolePermissionRepository;
        _permissionRepository = permissionRepository;
        _currentUser = currentUser;
        _cache = cache;
    }

    /// <inheritdoc />
    public Task<bool> IsGrantedAsync(string permissionName)
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.Id.HasValue)
        {
            _logger.LogWarning("Permission check attempted for unauthenticated user");
            return Task.FromResult(false);
        }

        return IsGrantedForUserAsync(_currentUser.Id.Value, permissionName);
    }

    /// <inheritdoc />
    public async Task<bool> IsGrantedForUserAsync(Guid userId, string permissionName)
    {
        using var activity = ActivitySource.StartActivity("CheckPermission");
        activity?.SetTag("permission.name", permissionName);
        activity?.SetTag("user.id", userId);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Get all permissions for user (with caching)
            var grantedPermissions = await GetGrantedPermissionsForUserAsync(userId);
            
            var isGranted = grantedPermissions.Contains(permissionName);
            
            stopwatch.Stop();
            
            _logger.LogInformation(
                "Permission check completed: User={UserId}, Permission={Permission}, Granted={IsGranted}, Duration={DurationMs}ms",
                userId, permissionName, isGranted, stopwatch.ElapsedMilliseconds);
            
            activity?.SetTag("permission.granted", isGranted);
            activity?.SetTag("duration.ms", stopwatch.ElapsedMilliseconds);

            return isGranted;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            _logger.LogError(ex, 
                "Permission check failed: User={UserId}, Permission={Permission}, Duration={DurationMs}ms",
                userId, permissionName, stopwatch.ElapsedMilliseconds);
            
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            
            throw;
        }
    }

    /// <inheritdoc />
    public Task<List<string>> GetGrantedPermissionsAsync()
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.Id.HasValue)
        {
            return Task.FromResult(new List<string>());
        }

        return GetGrantedPermissionsForUserAsync(_currentUser.Id.Value);
    }

    /// <inheritdoc />
    public async Task<List<string>> GetGrantedPermissionsForUserAsync(Guid userId)
    {
        using var activity = ActivitySource.StartActivity("GetGrantedPermissions");
        activity?.SetTag("user.id", userId);

        var cacheKey = $"{CacheKeyPrefix}{userId}";

        // Try to get from cache first
        if (_cache.TryGetValue(cacheKey, out List<string>? cachedPermissions) && cachedPermissions != null)
        {
            _logger.LogDebug("Retrieved {Count} permissions from cache for user {UserId}", 
                cachedPermissions.Count, userId);
            
            activity?.SetTag("cache.hit", true);
            activity?.SetTag("permissions.count", cachedPermissions.Count);
            
            return cachedPermissions;
        }

        activity?.SetTag("cache.hit", false);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Query permissions from database using user's role IDs
            var roleIds = _currentUser.RoleIds;

            var queryable = await _rolePermissionRepository.GetQueryableAsync();
            
            var permissions = await queryable
                .Include(rp => rp.Permission)
                .Where(rp => roleIds.Contains(rp.RoleId) && rp.Permission.IsActive)
                .Select(rp => rp.Permission.Name)
                .Distinct()
                .ToListAsync();

            stopwatch.Stop();

            _logger.LogInformation(
                "Loaded {Count} permissions for user {UserId} from database in {DurationMs}ms",
                permissions.Count, userId, stopwatch.ElapsedMilliseconds);

            activity?.SetTag("permissions.count", permissions.Count);
            activity?.SetTag("duration.ms", stopwatch.ElapsedMilliseconds);

            // Cache the result
            _cache.Set(cacheKey, permissions, CacheDuration);

            return permissions;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            _logger.LogError(ex,
                "Failed to load permissions for user {UserId} after {DurationMs}ms",
                userId, stopwatch.ElapsedMilliseconds);
            
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> IsGrantedAllAsync(params string[] permissionNames)
    {
        if (permissionNames.Length == 0)
            return true;

        using var activity = ActivitySource.StartActivity("CheckAllPermissions");
        activity?.SetTag("permissions.count", permissionNames.Length);

        var grantedPermissions = await GetGrantedPermissionsAsync();
        var result = permissionNames.All(p => grantedPermissions.Contains(p));

        activity?.SetTag("permissions.granted", result);

        _logger.LogDebug(
            "Checked {Count} permissions (ALL): {Result}",
            permissionNames.Length, result);

        return result;
    }

    /// <inheritdoc />
    public async Task<bool> IsGrantedAnyAsync(params string[] permissionNames)
    {
        if (permissionNames.Length == 0)
            return false;

        using var activity = ActivitySource.StartActivity("CheckAnyPermissions");
        activity?.SetTag("permissions.count", permissionNames.Length);

        var grantedPermissions = await GetGrantedPermissionsAsync();
        var result = permissionNames.Any(p => grantedPermissions.Contains(p));

        activity?.SetTag("permissions.granted", result);

        _logger.LogDebug(
            "Checked {Count} permissions (ANY): {Result}",
            permissionNames.Length, result);

        return result;
    }
}
