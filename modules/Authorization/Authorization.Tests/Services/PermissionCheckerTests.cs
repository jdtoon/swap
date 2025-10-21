using Authorization.Application.Services;using Authorization.Application.Services;using Microsoft.Extensions.Caching.Memory;

using Authorization.Contracts.Services;

using Authorization.Core.Entities;using Authorization.Contracts.Services;using Microsoft.Extensions.Logging;

using Microsoft.Extensions.Caching.Memory;

using Microsoft.Extensions.Logging;using Authorization.Core.Entities;using Moq;

using Moq;

using NetMX.Ddd.Domain.Repositories;using Microsoft.Extensions.Caching.Memory;using Authorization.Core.Entities;

using Xunit;

using Microsoft.Extensions.Logging;using Authorization.Application.Services;

namespace Authorization.Tests.Services;

using Moq;using Authorization.Contracts.Services;

/// <summary>

/// Unit tests for PermissionChecker service.using NetMX.Ddd.Domain.Repositories;using NetMX.Ddd.Domain.Repositories;

/// These tests focus on the core authorization logic and caching behavior.

/// </summary>using Xunit;using System.Linq.Expressions;

public class PermissionCheckerTests

{

    private readonly Mock<ILogger<PermissionChecker>> _mockLogger;

    private readonly Mock<IQueryableRepository<RolePermission, Guid>> _mockRolePermissionRepository;namespace Authorization.Tests.Services;namespace Authorization.Tests.Services;

    private readonly Mock<IQueryableRepository<Permission, Guid>> _mockPermissionRepository;

    private readonly Mock<ICurrentUser> _mockCurrentUser;

    private readonly IMemoryCache _cache;

    private readonly PermissionChecker _permissionChecker;/// <summary>/// <summary>



    public PermissionCheckerTests()/// Unit tests for PermissionChecker service./// Unit tests for PermissionChecker service.

    {

        _mockLogger = new Mock<ILogger<PermissionChecker>>();/// These tests focus on the core authorization logic and caching behavior./// Tests permission checking logic, caching, and error handling.

        _mockRolePermissionRepository = new Mock<IQueryableRepository<RolePermission, Guid>>();

        _mockPermissionRepository = new Mock<IQueryableRepository<Permission, Guid>>();/// </summary>/// </summary>

        _mockCurrentUser = new Mock<ICurrentUser>();

        _cache = new MemoryCache(new MemoryCacheOptions());public class PermissionCheckerTestspublic class PermissionCheckerTests : IDisposable



        _permissionChecker = new PermissionChecker({{

            _mockLogger.Object,

            _mockRolePermissionRepository.Object,    private readonly Mock<ILogger<PermissionChecker>> _mockLogger;    private readonly Mock<IRepository<Permission, Guid>> _permissionRepository;

            _mockPermissionRepository.Object,

            _mockCurrentUser.Object,    private readonly Mock<IQueryableRepository<RolePermission, Guid>> _mockRolePermissionRepository;    private readonly Mock<IRepository<Role, Guid>> _roleRepository;

            _cache);

    }    private readonly Mock<IQueryableRepository<Permission, Guid>> _mockPermissionRepository;    private readonly Mock<IRepository<RolePermission, Guid>> _rolePermissionRepository;



    [Fact]    private readonly Mock<ICurrentUser> _mockCurrentUser;    private readonly Mock<ICurrentUser> _currentUser;

    public async Task IsGrantedAsync_WhenUserNotAuthenticated_ReturnsFalse()

    {    private readonly IMemoryCache _cache;    private readonly Mock<ILogger<PermissionChecker>> _logger;

        // Arrange

        _mockCurrentUser.Setup(x => x.IsAuthenticated).Returns(false);    private readonly PermissionChecker _permissionChecker;    private readonly IMemoryCache _cache;

        _mockCurrentUser.Setup(x => x.Id).Returns((Guid?)null);

    private readonly PermissionChecker _permissionChecker;

        // Act

        var result = await _permissionChecker.IsGrantedAsync("Users.View");    public PermissionCheckerTests()



        // Assert    {    public PermissionCheckerTests()

        Assert.False(result);

    }        _mockLogger = new Mock<ILogger<PermissionChecker>>();    {



    [Fact]        _mockRolePermissionRepository = new Mock<IQueryableRepository<RolePermission, Guid>>();        _permissionRepository = new Mock<IRepository<Permission, Guid>>();

    public async Task IsGrantedAsync_WhenUserIdIsNull_ReturnsFalse()

    {        _mockPermissionRepository = new Mock<IQueryableRepository<Permission, Guid>>();        _roleRepository = new Mock<IRepository<Role, Guid>>();

        // Arrange

        _mockCurrentUser.Setup(x => x.IsAuthenticated).Returns(true);        _mockCurrentUser = new Mock<ICurrentUser>();        _rolePermissionRepository = new Mock<IRepository<RolePermission, Guid>>();

        _mockCurrentUser.Setup(x => x.Id).Returns((Guid?)null);

        _cache = new MemoryCache(new MemoryCacheOptions());        _currentUser = new Mock<ICurrentUser>();

        // Act

        var result = await _permissionChecker.IsGrantedAsync("Users.View");        _logger = new Mock<ILogger<PermissionChecker>>();



        // Assert        _permissionChecker = new PermissionChecker(        _cache = new MemoryCache(new MemoryCacheOptions());

        Assert.False(result);

    }            _mockLogger.Object,



    [Fact]            _mockRolePermissionRepository.Object,        _permissionChecker = new PermissionChecker(

    public void Constructor_WithValidDependencies_CreatesInstance()

    {            _mockPermissionRepository.Object,            _permissionRepository.Object,

        // Arrange & Act

        var checker = new PermissionChecker(            _mockCurrentUser.Object,            _roleRepository.Object,

            _mockLogger.Object,

            _mockRolePermissionRepository.Object,            _cache);            _rolePermissionRepository.Object,

            _mockPermissionRepository.Object,

            _mockCurrentUser.Object,    }            _currentUser.Object,

            _cache);

            _cache,

        // Assert

        Assert.NotNull(checker);    [Fact]            _logger.Object);

    }

    public async Task IsGrantedAsync_WhenUserNotAuthenticated_ReturnsFalse()    }

    [Fact]

    public async Task IsGrantedAllAsync_WithEmptyPermissionList_ReturnsTrue()    {

    {

        // Arrange        // Arrange    [Fact]

        _mockCurrentUser.Setup(x => x.IsAuthenticated).Returns(true);

        _mockCurrentUser.Setup(x => x.Id).Returns(Guid.NewGuid());        _mockCurrentUser.Setup(x => x.IsAuthenticated).Returns(false);    public async Task IsGrantedAsync_WhenUserNotAuthenticated_ReturnsFalse()

        var emptyPermissions = Array.Empty<string>();

        _mockCurrentUser.Setup(x => x.Id).Returns((Guid?)null);    {

        // Act

        var result = await _permissionChecker.IsGrantedAllAsync(emptyPermissions);        // Arrange



        // Assert        // Act        _currentUser.Setup(x => x.IsAuthenticated).Returns(false);

        Assert.True(result);

    }        var result = await _permissionChecker.IsGrantedAsync("Users.View");        _currentUser.Setup(x => x.Id).Returns((Guid?)null);



    [Fact]

    public async Task IsGrantedAnyAsync_WithEmptyPermissionList_ReturnsFalse()

    {        // Assert        // Act

        // Arrange

        _mockCurrentUser.Setup(x => x.IsAuthenticated).Returns(true);        Assert.False(result);        var result = await _permissionChecker.IsGrantedAsync("Users.View");

        _mockCurrentUser.Setup(x => x.Id).Returns(Guid.NewGuid());

        var emptyPermissions = Array.Empty<string>();    }



        // Act        // Assert

        var result = await _permissionChecker.IsGrantedAnyAsync(emptyPermissions);

    [Fact]        Assert.False(result);

        // Assert

        Assert.False(result);    public async Task IsGrantedAsync_WhenUserIdIsNull_ReturnsFalse()    }

    }

}    {


        // Arrange    [Fact]

        _mockCurrentUser.Setup(x => x.IsAuthenticated).Returns(true);    public async Task IsGrantedAsync_WhenPermissionGranted_ReturnsTrue()

        _mockCurrentUser.Setup(x => x.Id).Returns((Guid?)null);    {

        // Arrange

        // Act        var userId = Guid.NewGuid();

        var result = await _permissionChecker.IsGrantedAsync("Users.View");        var permissionId = Guid.NewGuid();

        var roleId = Guid.NewGuid();

        // Assert

        Assert.False(result);        _currentUser.Setup(x => x.IsAuthenticated).Returns(true);

    }        _currentUser.Setup(x => x.Id).Returns(userId);

        _currentUser.Setup(x => x.RoleIds).Returns(new[] { roleId });

    [Fact]

    public void Constructor_WithValidDependencies_CreatesInstance()        var permission = new Permission(permissionId, "Users.View", "View Users", "Users");

    {        _permissionRepository

        // Arrange & Act            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<Expression<Func<Permission, bool>>>()))

        var checker = new PermissionChecker(            .ReturnsAsync(permission);

            _mockLogger.Object,

            _mockRolePermissionRepository.Object,        var rolePermission = new RolePermission(roleId, permissionId, userId, DateTime.UtcNow);

            _mockPermissionRepository.Object,        _rolePermissionRepository

            _mockCurrentUser.Object,            .Setup(x => x.GetListAsync(It.IsAny<Expression<Func<RolePermission, bool>>>()))

            _cache);            .ReturnsAsync(new List<RolePermission> { rolePermission });



        // Assert        // Act

        Assert.NotNull(checker);        var result = await _permissionChecker.IsGrantedAsync("Users.View");

    }

        // Assert

    [Fact]        Assert.True(result);

    public async Task IsGrantedAllAsync_WithEmptyPermissionList_ReturnsTrue()    }

    {

        // Arrange    [Fact]

        _mockCurrentUser.Setup(x => x.IsAuthenticated).Returns(true);    public async Task IsGrantedAsync_WhenPermissionNotGranted_ReturnsFalse()

        _mockCurrentUser.Setup(x => x.Id).Returns(Guid.NewGuid());    {

        var emptyPermissions = Array.Empty<string>();        // Arrange

        var userId = Guid.NewGuid();

        // Act        var permissionId = Guid.NewGuid();

        var result = await _permissionChecker.IsGrantedAllAsync(emptyPermissions);        var roleId = Guid.NewGuid();



        // Assert        _currentUser.Setup(x => x.IsAuthenticated).Returns(true);

        Assert.True(result);        _currentUser.Setup(x => x.Id).Returns(userId);

    }        _currentUser.Setup(x => x.RoleIds).Returns(new[] { roleId });



    [Fact]        var permission = new Permission(permissionId, "Users.View", "View Users", "Users");

    public async Task IsGrantedAnyAsync_WithEmptyPermissionList_ReturnsFalse()        _permissionRepository

    {            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<Expression<Func<Permission, bool>>>()))

        // Arrange            .ReturnsAsync(permission);

        _mockCurrentUser.Setup(x => x.IsAuthenticated).Returns(true);

        _mockCurrentUser.Setup(x => x.Id).Returns(Guid.NewGuid());        _rolePermissionRepository

        var emptyPermissions = Array.Empty<string>();            .Setup(x => x.GetListAsync(It.IsAny<Expression<Func<RolePermission, bool>>>()))

            .ReturnsAsync(new List<RolePermission>()); // No permissions

        // Act

        var result = await _permissionChecker.IsGrantedAnyAsync(emptyPermissions);        // Act

        var result = await _permissionChecker.IsGrantedAsync("Users.View");

        // Assert

        Assert.False(result);        // Assert

    }        Assert.False(result);

}    }


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
