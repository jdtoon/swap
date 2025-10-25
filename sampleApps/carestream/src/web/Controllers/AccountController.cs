using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc;

namespace carestream.web.Controllers
{
    public class AccountController : Controller
    {
        private readonly IHostEnvironment _environment;
        private readonly ILogger<AccountController> _logger;

        public AccountController(IHostEnvironment environment, ILogger<AccountController> logger)
        {
            _environment = environment;
            _logger = logger;
        }

        public IActionResult Login(string? returnUrl = "/")
        {
            _logger.LogInformation("Login initiated. ReturnUrl: {ReturnUrl}", returnUrl);

            if (!Url.IsLocalUrl(returnUrl))
            {
                _logger.LogWarning("Non-local ReturnUrl '{ReturnUrl}' detected. Resetting to root.", returnUrl);
                returnUrl = "/";
            }

            var properties = new AuthenticationProperties { RedirectUri = returnUrl };
            return Challenge(properties, OpenIdConnectDefaults.AuthenticationScheme);
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            _logger.LogInformation("Logout POST request received. Environment: {EnvironmentName}", _environment.EnvironmentName);
            
            string redirectUri;
            try
            {
                redirectUri = Url.Action("Index", "Home", null, Request.Scheme)!;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate redirect URI for logout. Defaulting to root path.");
                redirectUri = "/";
            }

            _logger.LogInformation("Configuring OIDC sign-out with final RedirectUri: {RedirectUri}", redirectUri);

            var properties = new AuthenticationProperties
            {
                RedirectUri = redirectUri
            };

            return SignOut(properties,
                CookieAuthenticationDefaults.AuthenticationScheme,
                OpenIdConnectDefaults.AuthenticationScheme);
        }

        public IActionResult AccessDenied(string? message = null)
        {
            _logger.LogWarning("Access Denied action triggered. Message: {Message}", message ?? "No message provided.");
            ViewData["Message"] = message;
            return View();
        }
    }
}