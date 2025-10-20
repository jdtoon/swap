using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Authorization.Core.Entities;
using Authorization.Application.Services;
using NetMX.Ddd.Domain.Repositories;
using System.Linq.Expressions;

namespace Authorization.Tests.Services;

/// <summary>
/// Unit tests for PermissionChecker service.
/// Tests permission checking logic, caching, and error handling.
/// </summary>
public class PermissionCheckerTests : IDisposable
{
    private readonly Mock<IRepository<Permission, Guid>> _permissionRepository;
    private readonly Mock<IRepository<Role, Guid>> _roleRepository;
    private readonly Mock<IRepository<RolePermission, Guid>> _rolePermissionRepository;
    private readonly Mock<ICurrentUser> _currentUser;
    private readonly Mock<ILogger<PermissionChecker>> _logger;
    private readonly IMemoryCache _cache;
    private readonly PermissionChecker _permissionChecker;

    public PermissionCheckerTests()
    {
        _permissionRepository = new Mock<IRepository<Permission, Guid>>();
        _roleRepository = new Mock<IRepository<Role, Guid>>();
        _rolePermissionRepository = new Mock<IRepository<RolePermission, Guid>>();
        _currentUser = new Mock<ICurrentUser>();
        _logger = new Mock<ILogger<PermissionChecker>>();
        _cache = new MemoryCache(new MemoryCacheOptions());

        _permissionChecker = new PermissionChecker(
            _permissionRepository.Object,
            _roleRepository.Object,
            _rolePermissionRepository.Object,
            _currentUser.Object,
            _cache,
            _logger.Object);
    }

    [Fact]
    public async Task IsGrantedAsync_WhenUserNotAuthenticated_ReturnsFalse()
    {
        // Arrange
        _currentUser.Setup(x => x.IsAuthenticated).Returns(false);
        _currentUser.Setup(x => x.Id).Returns((Guid?)null);

        // Act
        var result = await _permissionChecker.IsGrantedAsync("Users.View");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task IsGrantedAsync_WhenPermissionGranted_ReturnsTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var permissionId = Guid.NewGuid();
        var roleId = Guid.NewGuid();

        _currentUser.Setup(x => x.IsAuthenticated).Returns(true);
        _currentUser.Setup(x => x.Id).Returns(userId);
        _currentUser.Setup(x => x.RoleIds).Returns(new[] { roleId });

        var permission = new Permission(permissionId, "Users.View", "View Users", "Users");
        _permissionRepository
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<Expression<Func<Permission, bool>>>()))
            .ReturnsAsync(permission);

        var rolePermission = new RolePermission(roleId, permissionId, userId, DateTime.UtcNow);
        _rolePermissionRepository
            .Setup(x => x.GetListAsync(It.IsAny<Expression<Func<RolePermission, bool>>>()))
            .ReturnsAsync(new List<RolePermission> { rolePermission });

        // Act
        var result = await _permissionChecker.IsGrantedAsync("Users.View");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsGrantedAsync_WhenPermissionNotGranted_ReturnsFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var permissionId = Guid.NewGuid();
        var roleId = Guid.NewGuid();

        _currentUser.Setup(x => x.IsAuthenticated).Returns(true);
        _currentUser.Setup(x => x.Id).Returns(userId);
        _currentUser.Setup(x => x.RoleIds).Returns(new[] { roleId });

        var permission = new Permission(permissionId, "Users.View", "View Users", "Users");
        _permissionRepository
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<Expression<Func<Permission, bool>>>()))
            .ReturnsAsync(permission);

        _rolePermissionRepository
            .Setup(x => x.GetListAsync(It.IsAny<Expression<Func<RolePermission, bool>>>()))
            .ReturnsAsync(new List<RolePermission>()); // No permissions

        // Act
        var result = await _permissionChecker.IsGrantedAsync("Users.View");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task IsGrantedAsync_WhenPermissionDoesNotExist_ReturnsFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _currentUser.Setup(x => x.IsAuthenticated).Returns(true);
        _currentUser.Setup(x => x.Id).Returns(userId);
        _currentUser.Setup(x => x.RoleIds).Returns(new[] { Guid.NewGuid() });

        _permissionRepository
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<Expression<Func<Permission, bool>>>()))
            .ReturnsAsync((Permission?)null); // Permission doesn't exist

        // Act
        var result = await _permissionChecker.IsGrantedAsync("NonExistent.Permission");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task IsGrantedAsync_UsesCaching_OnSecondCall()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var permissionId = Guid.NewGuid();
        var roleId = Guid.NewGuid();

        _currentUser.Setup(x => x.IsAuthenticated).Returns(true);
        _currentUser.Setup(x => x.Id).Returns(userId);
        _currentUser.Setup(x => x.RoleIds).Returns(new[] { roleId });

        var permission = new Permission(permissionId, "Users.View", "View Users", "Users");
        _permissionRepository
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<Expression<Func<Permission, bool>>>()))
            .ReturnsAsync(permission);

        var rolePermission = new RolePermission(roleId, permissionId, userId, DateTime.UtcNow);
        _rolePermissionRepository
            .Setup(x => x.GetListAsync(It.IsAny<Expression<Func<RolePermission, bool>>>()))
            .ReturnsAsync(new List<RolePermission> { rolePermission });

        // Act - First call (should hit database)
        var result1 = await _permissionChecker.IsGrantedAsync("Users.View");
        
        // Act - Second call (should use cache)
        var result2 = await _permissionChecker.IsGrantedAsync("Users.View");

        // Assert
        Assert.True(result1);
        Assert.True(result2);
        
        // Verify repository was only called once (cached on second call)
        _permissionRepository.Verify(
            x => x.FirstOrDefaultAsync(It.IsAny<Expression<Func<Permission, bool>>>()),
            Times.Once);
    }

    [Fact]
    public async Task IsGrantedAllAsync_WhenAllPermissionsGranted_ReturnsTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();

        _currentUser.Setup(x => x.IsAuthenticated).Returns(true);
        _currentUser.Setup(x => x.Id).Returns(userId);
        _currentUser.Setup(x => x.RoleIds).Returns(new[] { roleId });

        var permission1 = new Permission(Guid.NewGuid(), "Users.View", "View Users", "Users");
        var permission2 = new Permission(Guid.NewGuid(), "Users.Edit", "Edit Users", "Users");

        _permissionRepository
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<Expression<Func<Permission, bool>>>()))
            .ReturnsAsync((Expression<Func<Permission, bool>> expr) =>
            {
                // Return different permissions based on name
                var compiled = expr.Compile();
                if (compiled(permission1)) return permission1;
                if (compiled(permission2)) return permission2;
                return null;
            });

        _rolePermissionRepository
            .Setup(x => x.GetListAsync(It.IsAny<Expression<Func<RolePermission, bool>>>()))
            .ReturnsAsync(new List<RolePermission>
            {
                new RolePermission(roleId, permission1.Id, userId, DateTime.UtcNow),
                new RolePermission(roleId, permission2.Id, userId, DateTime.UtcNow)
            });

        // Act
        var result = await _permissionChecker.IsGrantedAllAsync("Users.View", "Users.Edit");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsGrantedAllAsync_WhenOnePermissionMissing_ReturnsFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();

        _currentUser.Setup(x => x.IsAuthenticated).Returns(true);
        _currentUser.Setup(x => x.Id).Returns(userId);
        _currentUser.Setup(x => x.RoleIds).Returns(new[] { roleId });

        var permission1 = new Permission(Guid.NewGuid(), "Users.View", "View Users", "Users");
        var permission2 = new Permission(Guid.NewGuid(), "Users.Edit", "Edit Users", "Users");

        _permissionRepository
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<Expression<Func<Permission, bool>>>()))
            .ReturnsAsync((Expression<Func<Permission, bool>> expr) =>
            {
                var compiled = expr.Compile();
                if (compiled(permission1)) return permission1;
                if (compiled(permission2)) return permission2;
                return null;
            });

        // Only grant permission1, not permission2
        _rolePermissionRepository
            .Setup(x => x.GetListAsync(It.IsAny<Expression<Func<RolePermission, bool>>>()))
            .ReturnsAsync(new List<RolePermission>
            {
                new RolePermission(roleId, permission1.Id, userId, DateTime.UtcNow)
            });

        // Act
        var result = await _permissionChecker.IsGrantedAllAsync("Users.View", "Users.Edit");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task IsGrantedAnyAsync_WhenAnyPermissionGranted_ReturnsTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();

        _currentUser.Setup(x => x.IsAuthenticated).Returns(true);
        _currentUser.Setup(x => x.Id).Returns(userId);
        _currentUser.Setup(x => x.RoleIds).Returns(new[] { roleId });

        var permission1 = new Permission(Guid.NewGuid(), "Users.View", "View Users", "Users");
        var permission2 = new Permission(Guid.NewGuid(), "Users.Edit", "Edit Users", "Users");

        _permissionRepository
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<Expression<Func<Permission, bool>>>()))
            .ReturnsAsync((Expression<Func<Permission, bool>> expr) =>
            {
                var compiled = expr.Compile();
                if (compiled(permission1)) return permission1;
                if (compiled(permission2)) return permission2;
                return null;
            });

        // Grant only permission1
        _rolePermissionRepository
            .Setup(x => x.GetListAsync(It.IsAny<Expression<Func<RolePermission, bool>>>()))
            .ReturnsAsync(new List<RolePermission>
            {
                new RolePermission(roleId, permission1.Id, userId, DateTime.UtcNow)
            });

        // Act
        var result = await _permissionChecker.IsGrantedAnyAsync("Users.View", "Users.Edit");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsGrantedAnyAsync_WhenNoPermissionsGranted_ReturnsFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();

        _currentUser.Setup(x => x.IsAuthenticated).Returns(true);
        _currentUser.Setup(x => x.Id).Returns(userId);
        _currentUser.Setup(x => x.RoleIds).Returns(new[] { roleId });

        var permission1 = new Permission(Guid.NewGuid(), "Users.View", "View Users", "Users");
        var permission2 = new Permission(Guid.NewGuid(), "Users.Edit", "Edit Users", "Users");

        _permissionRepository
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<Expression<Func<Permission, bool>>>()))
            .ReturnsAsync((Expression<Func<Permission, bool>> expr) =>
            {
                var compiled = expr.Compile();
                if (compiled(permission1)) return permission1;
                if (compiled(permission2)) return permission2;
                return null;
            });

        _rolePermissionRepository
            .Setup(x => x.GetListAsync(It.IsAny<Expression<Func<RolePermission, bool>>>()))
            .ReturnsAsync(new List<RolePermission>()); // No permissions

        // Act
        var result = await _permissionChecker.IsGrantedAnyAsync("Users.View", "Users.Edit");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GetGrantedPermissionsAsync_ReturnsAllGrantedPermissions()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();

        _currentUser.Setup(x => x.IsAuthenticated).Returns(true);
        _currentUser.Setup(x => x.Id).Returns(userId);
        _currentUser.Setup(x => x.RoleIds).Returns(new[] { roleId });

        var permission1 = new Permission(Guid.NewGuid(), "Users.View", "View Users", "Users");
        var permission2 = new Permission(Guid.NewGuid(), "Users.Edit", "Edit Users", "Users");

        _rolePermissionRepository
            .Setup(x => x.GetListAsync(It.IsAny<Expression<Func<RolePermission, bool>>>()))
            .ReturnsAsync(new List<RolePermission>
            {
                new RolePermission(roleId, permission1.Id, userId, DateTime.UtcNow),
                new RolePermission(roleId, permission2.Id, userId, DateTime.UtcNow)
            });

        _permissionRepository
            .Setup(x => x.GetListAsync(It.IsAny<Expression<Func<Permission, bool>>>()))
            .ReturnsAsync(new List<Permission> { permission1, permission2 });

        // Act
        var result = await _permissionChecker.GetGrantedPermissionsAsync();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, p => p == "Users.View");
        Assert.Contains(result, p => p == "Users.Edit");
    }

    public void Dispose()
    {
        _cache?.Dispose();
    }
}
