using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using carestream.core.interfaces.services;
using carestream.core.interfaces.repositories;
using carestream.core.services;
using carestream.core.dtos.admin;

namespace carestream.tests.unit.services
{
    public class AdminUserServiceTests
    {
        private readonly Mock<IUserRepository> _mockUserRepository;
        private readonly Mock<ILogger<AdminUserService>> _mockLogger;
        private readonly IAdminUserService _adminUserService;
        private readonly Mock<IPasswordHasherService> _mockPasswordHasherService;
        private readonly Mock<IFacilityRepository> _mockFacilityRepository;

        public AdminUserServiceTests()
        {
            _mockUserRepository = new Mock<IUserRepository>();
            _mockPasswordHasherService = new Mock<IPasswordHasherService>();
            _mockLogger = new Mock<ILogger<AdminUserService>>();
            _mockFacilityRepository = new Mock<IFacilityRepository>();

            _adminUserService = new AdminUserService(
                _mockUserRepository.Object,
                _mockLogger.Object,
                _mockPasswordHasherService.Object,
                _mockFacilityRepository.Object
            );
        }

        [Fact]
        public async Task GetUserManagementViewModelAsync_ShouldCallRepositoryAndReturnViewModel()
        {
            // Arrange
            string? searchTerm = "test";
            int pageNumber = 1;
            int pageSize = 10;
            var expectedUsers = new List<AdminUserListItemDto> { new AdminUserListItemDto { UserId = 1, FullName = "Test User" } };
            _mockUserRepository.Setup(repo => repo.GetAllUsersForAdminAsync(searchTerm, pageSize, pageNumber, null, null))
                               .ReturnsAsync(expectedUsers);

            // Act
            var viewModel = await _adminUserService.GetUserManagementViewModelAsync(searchTerm, pageNumber, pageSize);

            // Assert
            Assert.NotNull(viewModel);
            Assert.Equal(searchTerm, viewModel.SearchTerm);
            Assert.NotNull(viewModel.Users);
            Assert.Equal(expectedUsers.Count, viewModel.Users.Count);
            if (expectedUsers.Any())
            {
                Assert.Same(expectedUsers.First(), viewModel.Users.First());
            }
            _mockUserRepository.Verify(repo => repo.GetAllUsersForAdminAsync(searchTerm, pageSize, pageNumber, null, null), Times.Once);
        }

        [Fact]
        public async Task LinkUserToLogtoAsync_ValidInput_RepositorySucceeds_ShouldReturnTrue()
        {
            // Arrange
            int userId = 1;
            string logtoSub = "logto|12345";
            _mockUserRepository.Setup(repo => repo.LinkLogtoSubAsync(userId, logtoSub, null, null))
                               .ReturnsAsync(true);

            // Act
            bool result = await _adminUserService.LinkUserToLogtoAsync(userId, logtoSub);

            // Assert
            Assert.True(result);
            _mockUserRepository.Verify(repo => repo.LinkLogtoSubAsync(userId, logtoSub, null, null), Times.Once);
        }

        [Fact]
        public async Task LinkUserToLogtoAsync_ValidInput_RepositoryFails_ShouldReturnFalse()
        {
            // Arrange
            int userId = 1;
            string logtoSub = "logto|12345";
            _mockUserRepository.Setup(repo => repo.LinkLogtoSubAsync(userId, logtoSub, null, null))
                               .ReturnsAsync(false); // Simulate repository failure

            // Act
            bool result = await _adminUserService.LinkUserToLogtoAsync(userId, logtoSub);

            // Assert
            Assert.False(result);
            _mockUserRepository.Verify(repo => repo.LinkLogtoSubAsync(userId, logtoSub, null, null), Times.Once);
        }

        [Theory]
        [InlineData(0, "logto|123")]
        [InlineData(1, null)]
        [InlineData(1, "")]
        [InlineData(1, "   ")]
        public async Task LinkUserToLogtoAsync_InvalidInput_ShouldReturnFalseAndNotCallRepository(int userId, string? logtoSub)
        {
            // Act
            bool result = await _adminUserService.LinkUserToLogtoAsync(userId, logtoSub!);

            // Assert
            Assert.False(result);
            _mockUserRepository.Verify(repo => repo.LinkLogtoSubAsync(It.IsAny<int>(), It.IsAny<string>(), null, null), Times.Never);
        }

        [Fact]
        public async Task GetUserForAdminByIdAsync_UserExists_ShouldReturnUserDto()
        {
            // Arrange
            int userId = 1;
            var expectedUser = new AdminUserListItemDto { UserId = userId, FullName = "Test User" };
            _mockUserRepository.Setup(repo => repo.GetUserForAdminByIdAsync(userId, null, null))
                               .ReturnsAsync(expectedUser);

            // Act
            var result = await _adminUserService.GetUserForAdminByIdAsync(userId);

            // Assert
            Assert.Same(expectedUser, result);
            _mockUserRepository.Verify(repo => repo.GetUserForAdminByIdAsync(userId, null, null), Times.Once);
        }

        [Fact]
        public async Task GetUserForAdminByIdAsync_UserNotExists_ShouldReturnNull()
        {
            // Arrange
            int userId = 99;
            _mockUserRepository.Setup(repo => repo.GetUserForAdminByIdAsync(userId, null, null))
                               .ReturnsAsync((AdminUserListItemDto?)null);

            // Act
            var result = await _adminUserService.GetUserForAdminByIdAsync(userId);

            // Assert
            Assert.Null(result);
            _mockUserRepository.Verify(repo => repo.GetUserForAdminByIdAsync(userId, null, null), Times.Once);
        }

        [Fact]
        public async Task SetUserVerificationCodeAsync_ValidInput_UserExists_ShouldHashAndCallRepository()
        {
            // Arrange
            int userId = 1;
            string newCode = "1234";
            string expectedSalt = "testSalt";
            string expectedHash = "testHash";

            _mockUserRepository.Setup(r => r.GetUserForAdminByIdAsync(userId, null, null))
                               .ReturnsAsync(new AdminUserListItemDto { UserId = userId }); // Simulate user exists
            _mockPasswordHasherService.Setup(h => h.HashPassword(newCode, out expectedSalt))
                                      .Returns(expectedHash);
            _mockUserRepository.Setup(r => r.SetUserVerificationCodeAsync(userId, expectedHash, expectedSalt, null, null))
                               .ReturnsAsync(true); // Simulate successful save

            // Act
            bool result = await _adminUserService.SetUserVerificationCodeAsync(userId, newCode);

            // Assert
            Assert.True(result);
            _mockPasswordHasherService.Verify(h => h.HashPassword(newCode, out It.Ref<string>.IsAny), Times.Once);
            _mockUserRepository.Verify(r => r.SetUserVerificationCodeAsync(userId, expectedHash, expectedSalt, null, null), Times.Once);
        }

        [Fact]
        public async Task SetUserVerificationCodeAsync_UserNotFound_ShouldReturnFalse()
        {
            // Arrange
            int userId = 99; // Non-existent user
            string newCode = "1234";
            _mockUserRepository.Setup(r => r.GetUserForAdminByIdAsync(userId, null, null))
                               .ReturnsAsync((AdminUserListItemDto?)null); // Simulate user not found

            // Act
            bool result = await _adminUserService.SetUserVerificationCodeAsync(userId, newCode);

            // Assert
            Assert.False(result);
            _mockPasswordHasherService.Verify(h => h.HashPassword(It.IsAny<string>(), out It.Ref<string>.IsAny), Times.Never);
            _mockUserRepository.Verify(r => r.SetUserVerificationCodeAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), null, null), Times.Never);
        }

        [Theory]
        [InlineData(0, "1234")]
        [InlineData(1, null)]
        [InlineData(1, "")]
        [InlineData(1, "   ")]
        public async Task SetUserVerificationCodeAsync_InvalidInput_ShouldReturnFalse(int userId, string? newCode)
        {
            // Act
            bool result = await _adminUserService.SetUserVerificationCodeAsync(userId, newCode!);

            // Assert
            Assert.False(result);
            _mockUserRepository.Verify(r => r.GetUserForAdminByIdAsync(It.IsAny<int>(), null, null), Times.Never);
            _mockPasswordHasherService.Verify(h => h.HashPassword(It.IsAny<string>(), out It.Ref<string>.IsAny), Times.Never);
            _mockUserRepository.Verify(r => r.SetUserVerificationCodeAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), null, null), Times.Never);
        }

        [Fact]
        public async Task SetUserVerificationCodeAsync_RepositorySaveFails_ShouldReturnFalse()
        {
            // Arrange
            int userId = 1;
            string newCode = "1234";
            string dummySalt = "salt";
            string dummyHash = "hash";

            _mockUserRepository.Setup(r => r.GetUserForAdminByIdAsync(userId, null, null))
                               .ReturnsAsync(new AdminUserListItemDto { UserId = userId });
            _mockPasswordHasherService.Setup(h => h.HashPassword(newCode, out dummySalt))
                                      .Returns(dummyHash);
            _mockUserRepository.Setup(r => r.SetUserVerificationCodeAsync(userId, dummyHash, dummySalt, null, null))
                               .ReturnsAsync(false); // Simulate repository save failure

            // Act
            bool result = await _adminUserService.SetUserVerificationCodeAsync(userId, newCode);

            // Assert
            Assert.False(result);
        }
    }
}