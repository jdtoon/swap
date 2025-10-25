using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using habits.Settings;

namespace habits.Services.Notifications
{
    public interface IFcmNotificationService
    {
        Task SendNotificationAsync(string fcmToken, string title, string body);

        Task SendNotificationToMultipleTokensAsync(IEnumerable<string> fcmTokens, string title, string body);
    }

    public class FcmNotificationService : IFcmNotificationService
    {
        private readonly ILogger<FcmNotificationService> _logger;
        private readonly FcmSettings _settings;

        public FcmNotificationService(
            IOptions<FcmSettings> settings,
            ILogger<FcmNotificationService> logger)
        {
            _settings = settings.Value;
            _logger = logger;

            if (FirebaseApp.DefaultInstance == null)
            {
                InitializeFirebase();
            }
        }

        private void InitializeFirebase()
        {
            try
            {
                _logger.LogInformation("Starting Firebase initialization");
                _logger.LogInformation("Initializing Firebase with project ID: {ProjectId}", _settings.ProjectId);
                var credentialJson = GenerateCredentialJson();
                _logger.LogInformation("Generated credential JSON with ProjectId: {ProjectId}", _settings.ProjectId);

                var credentials = GoogleCredential.FromJson(credentialJson);
                _logger.LogInformation("Successfully created GoogleCredential");

                var firebaseApp = FirebaseApp.Create(new AppOptions
                {
                    Credential = credentials
                });
                _logger.LogInformation("Firebase App created successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Firebase. Settings: {@Settings}", new
                {
                    ProjectId = _settings.ProjectId,
                    HasPrivateKey = !string.IsNullOrEmpty(_settings.PrivateKey),
                    HasClientEmail = !string.IsNullOrEmpty(_settings.ClientEmail),
                    HasClientId = !string.IsNullOrEmpty(_settings.ClientId)
                });
                throw;
            }
        }

        private string GenerateCredentialJson()
        {
            try
            {
                var credentialJson = new JObject
                {
                    ["type"] = "service_account",
                    ["project_id"] = _settings.ProjectId,
                    ["private_key_id"] = _settings.PrivateKeyId,
                    ["private_key"] = _settings.PrivateKey,
                    ["client_email"] = _settings.ClientEmail,
                    ["client_id"] = _settings.ClientId,
                    ["auth_uri"] = "https://accounts.google.com/o/oauth2/auth",
                    ["token_uri"] = "https://oauth2.googleapis.com/token",
                    ["auth_provider_x509_cert_url"] = "https://www.googleapis.com/oauth2/v1/certs",
                    ["client_x509_cert_url"] = _settings.ClientX509CertUrl
                };

                // Verify the JSON is valid before returning
                try
                {
                    var jsonString = credentialJson.ToString();
                    _logger.LogInformation("Successfully generated valid credential JSON");
                    return jsonString;
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Generated invalid JSON credential string");
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate credential JSON. Error: {Message}", ex.Message);
                throw;
            }
        }

        public async Task SendNotificationAsync(string fcmToken, string title, string body)
        {
            try
            {
                _logger.LogInformation("Preparing to send FCM notification. Token length: {TokenLength}", fcmToken?.Length);
                _logger.LogInformation("Sending FCM notification to token {TokenPrefix}... Title: {Title}", 
                    fcmToken.Substring(0, 6), title);

                var message = new Message()
                {
                    Notification = new Notification
                    {
                        Title = title,
                        Body = body
                    },
                    Token = fcmToken,
                };

                _logger.LogInformation("Sending FCM message with title: {Title}", title);
                var response = await FirebaseMessaging.DefaultInstance.SendAsync(message);
                _logger.LogInformation("Successfully sent FCM notification. Response: {Response}", response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending FCM notification. Token: {Token}, Title: {Title}",
                    fcmToken?.Substring(0, Math.Min(10, fcmToken?.Length ?? 0)), title);
                throw;
            }
        }

        public async Task SendNotificationToMultipleTokensAsync(IEnumerable<string> fcmTokens, string title, string body)
        {
            try
            {
                _logger.LogInformation("Sending batch FCM notification to {Count} recipients. Title: {Title}", 
                    fcmTokens.Count(), title);

                var message = new MulticastMessage
                {
                    Tokens = fcmTokens.ToList(),
                    Notification = new Notification
                    {
                        Title = title,
                        Body = body
                    }
                };

                var response = await FirebaseMessaging.DefaultInstance.SendEachForMulticastAsync(message);
                _logger.LogInformation("Successfully sent FCM notifications. Success: {0}, Failure: {1}",
                    response.SuccessCount, response.FailureCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending FCM notifications");
                throw;
            }
        }
    }
}