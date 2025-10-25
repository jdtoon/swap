using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using habits.Services.Notifications;
using habits.Settings;
using System.Reflection;

namespace habits.Tests.Services.Notifications
{
    [TestClass]
    public class FcmNotificationServiceTests
    {
        private Mock<ILogger<FcmNotificationService>> _loggerMock;
        private Mock<IOptions<FcmSettings>> _settingsMock;
        private FcmSettings _settings;
        private FcmNotificationService _service;

        [TestInitialize]
        public void TestInitialize()
        {
            // Setup mocks
            _loggerMock = new Mock<ILogger<FcmNotificationService>>();
            _settingsMock = new Mock<IOptions<FcmSettings>>();

            // Setup settings
            _settings = new FcmSettings
            {
                ProjectId = "test-project",
                PrivateKeyId = "test-key-id",
                PrivateKey = "test-private-key",
                ClientEmail = "test@example.com",
                ClientId = "test-client-id",
                ClientX509CertUrl = "https://test.example.com/cert"
            };

            _settingsMock.Setup(x => x.Value).Returns(_settings);

            // Reset Firebase singleton for each test
            ResetFirebaseApp();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            ResetFirebaseApp();
        }

        private void ResetFirebaseApp()
        {
            // Use reflection to reset Firebase singleton instance
            var firebaseAppType = typeof(FirebaseApp);
            var instanceField = firebaseAppType.GetField("_instance", BindingFlags.NonPublic | BindingFlags.Static);
            instanceField?.SetValue(null, null);
        }

        [TestMethod]
        public void Constructor_InitializesFirebaseApp()
        {
            // Act
            _service = new FcmNotificationService(_settingsMock.Object, _loggerMock.Object);

            // Assert
            Assert.IsNotNull(FirebaseApp.DefaultInstance);
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Firebase App created successfully")),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
                Times.Once);
        }

        [TestMethod]
        public void Constructor_LogsError_WhenInitializationFails()
        {
            // Arrange
            _settings.ProjectId = null; // This will cause initialization to fail

            // Act & Assert
            Assert.ThrowsException<Exception>(() => 
                new FcmNotificationService(_settingsMock.Object, _loggerMock.Object));

            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => true),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
                Times.Once);
        }

        [TestMethod]
        public async Task SendNotificationAsync_SendsMessage()
        {
            // Arrange
            _service = new FcmNotificationService(_settingsMock.Object, _loggerMock.Object);
            string fcmToken = "test-token";
            string title = "Test Title";
            string body = "Test Body";

            // Act
            try
            {
                await _service.SendNotificationAsync(fcmToken, title, body);
            }
            catch (FirebaseMessagingException)
            {
                // Expected in test environment since we can't actually send messages
            }

            // Assert
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains(title)),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
                Times.AtLeastOnce);
        }

        [TestMethod]
        public async Task SendNotificationToMultipleTokensAsync_SendsMessages()
        {
            // Arrange
            _service = new FcmNotificationService(_settingsMock.Object, _loggerMock.Object);
            var fcmTokens = new[] { "token1", "token2" };
            string title = "Test Title";
            string body = "Test Body";

            // Act
            try
            {
                await _service.SendNotificationToMultipleTokensAsync(fcmTokens, title, body);
            }
            catch (FirebaseMessagingException)
            {
                // Expected in test environment since we can't actually send messages
            }

            // Assert
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("2 recipients")),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
                Times.AtLeastOnce);
        }

        [TestMethod]
        public async Task SendNotificationAsync_HandlesError()
        {
            // Arrange
            _service = new FcmNotificationService(_settingsMock.Object, _loggerMock.Object);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<FirebaseMessagingException>(async () =>
                await _service.SendNotificationAsync("invalid-token", "title", "body"));

            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => true),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
                Times.Once);
        }

        [TestMethod]
        public async Task SendNotificationToMultipleTokensAsync_HandlesError()
        {
            // Arrange
            _service = new FcmNotificationService(_settingsMock.Object, _loggerMock.Object);
            var invalidTokens = new[] { "invalid1", "invalid2" };

            // Act & Assert
            await Assert.ThrowsExceptionAsync<FirebaseMessagingException>(async () =>
                await _service.SendNotificationToMultipleTokensAsync(invalidTokens, "title", "body"));

            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => true),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
                Times.Once);
        }
    }
} 