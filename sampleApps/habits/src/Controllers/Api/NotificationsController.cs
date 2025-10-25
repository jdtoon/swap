using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using habits.Data.Models;
using habits.Services;
using habits.Services.Notifications;
using FirebaseAdmin.Messaging;
using FirebaseAdmin;

namespace habits.Controllers.Api
{
    [Route("api/notifications")]
    [ApiController]
    [Authorize]
    public class NotificationsController : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly ILogger<NotificationsController> _logger;

        public NotificationsController(
            UserManager<AppUser> userManager,
            ILogger<NotificationsController> logger)
        {
            _userManager = userManager;
            _logger = logger;
        }

        [HttpPost("token")]
        public async Task<IActionResult> SaveToken([FromBody] TokenRequest request)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return NotFound();
                }

                user.FcmToken = request.Token;
                user.ReceiveNotifications = true;

                var result = await _userManager.UpdateAsync(user);
                if (!result.Succeeded)
                {
                    return BadRequest(result.Errors);
                }

                _logger.LogInformation("Saving FCM token for user {UserId}. Token length: {Length}",
                    user.Id, request.Token.Length);

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving FCM token");
                return StatusCode(500);
            }
        }

        [HttpDelete("token")]
        public async Task<IActionResult> DeleteToken()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return NotFound();
                }

                user.FcmToken = null;
                user.ReceiveNotifications = false;

                var result = await _userManager.UpdateAsync(user);
                if (!result.Succeeded)
                {
                    return BadRequest(result.Errors);
                }

                _logger.LogInformation("Removing FCM token for user {UserId}", user.Id);

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing FCM token");
                return StatusCode(500);
            }
        }

        [HttpPost("email")]
        public async Task<IActionResult> ToggleEmailNotifications()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            user.ReceiveNotifications = !user.ReceiveNotifications;
            await _userManager.UpdateAsync(user);

            return Ok(new { receiveEmails = user.ReceiveNotifications });
        }

        [HttpGet("status")]
        public async Task<IActionResult> GetNotificationStatus()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            return Ok(new
            {
                emailNotifications = user.ReceiveNotifications,
                pushNotifications = user.ReceivePushNotifications,
                hasFcmToken = !string.IsNullOrEmpty(user.FcmToken)
            });
        }

        [HttpGet("push/status")]
        public async Task<IActionResult> GetPushNotificationStatus()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            return Ok(new { receivePush = user.ReceivePushNotifications });
        }
    }

    public class TokenRequest
    {
        public string Token { get; set; } = string.Empty;
    }
}