using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using ttw.Data.Models;
using ttw.Dtos.Identity;

namespace ttw.Controllers
{
    [Authorize]
    public class IdentityController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly ILogger<IdentityController> _logger;
        private readonly UrlEncoder _urlEncoder;
        private readonly IEmailSender _emailSender;

        public IdentityController(
            UserManager<AppUser> userManager,
            SignInManager<AppUser> signInManager,
            ILogger<IdentityController> logger,
            UrlEncoder urlEncoder,
            IEmailSender emailSender)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            _urlEncoder = urlEncoder;
            _emailSender = emailSender;
        }

        #region Login/Logout

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Login(string returnUrl = null!)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (Request.Headers.ContainsKey("HX-Request"))
            {
                Response.Headers.Append("HX-Push-Url", "/Identity/Login");
                return PartialView(new LoginDto());
            }
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> Login(LoginDto model, string returnUrl = null!)
        {
            returnUrl ??= Url.Content("~/");

            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(model.Email!, model.Password!, model.RememberMe, lockoutOnFailure: true);

                if (result.Succeeded)
                {
                    _logger.LogInformation("User logged in.");
                    if (Request.Headers.ContainsKey("HX-Request"))
                    {
                        Response.Headers.Append("HX-Push-Url", returnUrl);
                        return PartialView("_LoginSuccess");
                    }
                    return LocalRedirect(returnUrl);
                }

                if (result.IsLockedOut)
                {
                    _logger.LogWarning("User account locked out.");
                    return RedirectToAction(nameof(Lockout));
                }

                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            }

            if (Request.Headers.ContainsKey("HX-Request"))
            {
                return PartialView("_LoginForm", model);
            }

            return View(model);
        }

        #endregion Login/Logout

        #region Registration

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Register(string returnUrl = null!)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> Register(RegisterDto model, string returnUrl = null!)
        {
            returnUrl ??= Url.Content("~/");
            if (ModelState.IsValid)
            {
                var user = new AppUser { UserName = model.Email, Email = model.Email };
                var result = await _userManager.CreateAsync(user, model.Password!);

                if (result.Succeeded)
                {
                    _logger.LogInformation("User created a new account with password.");

                    var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

                    // You can implement email confirmation here if needed

                    await _signInManager.SignInAsync(user, isPersistent: false);
                    if (Request.Headers.ContainsKey("HX-Request"))
                    {
                        return PartialView("_RegisterSuccess");
                    }
                    return LocalRedirect(returnUrl);
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            if (Request.Headers.ContainsKey("HX-Request"))
            {
                return PartialView(model);
            }
            return View(model);
        }

        #endregion Registration

        #region Password Management

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> ChangePassword()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            if (Request.Headers["HX-Request"] == "true")
            {
                return PartialView();
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword(ChangePasswordDto model)
        {
            if (!ModelState.IsValid)
            {
                if (Request.Headers.ContainsKey("HX-Request"))
                {
                    return PartialView(model);
                }
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var changePasswordResult = await _userManager.ChangePasswordAsync(user,
                model.OldPassword!, model.NewPassword!);

            if (!changePasswordResult.Succeeded)
            {
                foreach (var error in changePasswordResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                if (Request.Headers.ContainsKey("HX-Request"))
                {
                    return PartialView(model);
                }
                return View(model);
            }

            await _signInManager.RefreshSignInAsync(user);
            _logger.LogInformation("User changed their password successfully.");

            if (Request.Headers.ContainsKey("HX-Request"))
            {
                Response.Headers.Append("HX-Push-Url", "/Identity/ChangePassword");
                return PartialView("_ChangePasswordSuccess");
            }
            return RedirectToAction(nameof(ChangePassword));
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            if (Request.Headers.ContainsKey("HX-Request"))
            {
                Response.Headers.Append("HX-Push-Url", "/Identity/ForgotPassword");
                return PartialView();
            }
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(email);
                if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
                {
                    // Don't reveal that the user does not exist or is not confirmed
                    if (Request.Headers.ContainsKey("HX-Request"))
                    {
                        return PartialView("_ForgotPasswordConfirmation");
                    }
                    return RedirectToAction(nameof(ForgotPasswordConfirmation));
                }

                var code = await _userManager.GeneratePasswordResetTokenAsync(user);
                code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

                var callbackUrl = Url.Action(
                    "ResetPassword",
                    "Identity",
                    new { code },
                    protocol: Request.Scheme);

                var emailBody = $@"
                    <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;'>
                        <div style='background-color: #f8f9fa; padding: 20px; border-radius: 10px; margin-bottom: 20px;'>
                            <h1 style='color: #1a1a1a; margin-bottom: 20px;'>Reset Your Password</h1>
                            <p style='color: #4a5568; line-height: 1.6;'>
                                You recently requested to reset your password for your TTW Management account. 
                                Click the button below to reset it.
                            </p>
                            <div style='text-align: center; margin: 30px 0;'>
                                <a href='{HtmlEncoder.Default.Encode(callbackUrl!)}'
                                   style='background-color: #570df8; color: white; padding: 12px 24px; 
                                          text-decoration: none; border-radius: 5px; display: inline-block;'>
                                    Reset Your Password
                                </a>
                            </div>
                            <p style='color: #4a5568; line-height: 1.6;'>
                                If you did not request a password reset, please ignore this email or contact support 
                                if you have concerns.
                            </p>
                            <p style='color: #4a5568; font-size: 0.9em; margin-top: 20px;'>
                                This password reset link will expire in 24 hours.
                            </p>
                        </div>
                        <div style='text-align: center; color: #718096; font-size: 0.8em;'>
                            <p>TTW Management System</p>
                        </div>
                    </div>";

                await _emailSender.SendEmailAsync(
                    email,
                    "Reset Your Password - TTW Management",
                    emailBody);

                if (Request.Headers.ContainsKey("HX-Request"))
                {
                    return PartialView("_ForgotPasswordConfirmation");
                }
                return RedirectToAction(nameof(ForgotPasswordConfirmation));
            }

            if (Request.Headers.ContainsKey("HX-Request"))
            {
                return PartialView();
            }
            return View();
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult ResetPassword(string code = null!)
        {
            if (code == null)
            {
                return BadRequest("A code must be supplied for password reset.");
            }

            var model = new ResetPasswordDto
            {
                Code = code  // Don't decode the code here, it will be decoded in the POST action
            };

            if (Request.Headers.ContainsKey("HX-Request"))
            {
                Response.Headers.Append("HX-Push-Url", "/Identity/ResetPassword");
                return PartialView(model);
            }
            return View(model);
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> ResetPassword(ResetPasswordDto model)
        {
            if (!ModelState.IsValid)
            {
                if (Request.Headers.ContainsKey("HX-Request"))
                {
                    return PartialView(model);
                }
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email!);
            if (user == null)
            {
                if (Request.Headers.ContainsKey("HX-Request"))
                {
                    return PartialView("_ResetPasswordConfirmation");
                }
                return RedirectToAction(nameof(ResetPasswordConfirmation));
            }

            // Decode the code here before resetting the password
            var decodedCode = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(model.Code!));
            var result = await _userManager.ResetPasswordAsync(user, decodedCode, model.Password!);
            
            if (result.Succeeded)
            {
                if (Request.Headers.ContainsKey("HX-Request"))
                {
                    return PartialView("_ResetPasswordConfirmation");
                }
                return RedirectToAction(nameof(ResetPasswordConfirmation));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            if (Request.Headers.ContainsKey("HX-Request"))
            {
                return PartialView(model);
            }
            return View(model);
        }

        #endregion Password Management

        #region Lockout

        [AllowAnonymous]
        [HttpGet]
        public IActionResult AccessDenied()
        {
            if (Request.Headers.ContainsKey("HX-Request"))
            {
                Response.Headers.Append("HX-Push-Url", "/Identity/AccessDenied");
                return PartialView("_AccessDenied");
            }
            return View();
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Lockout()
        {
            if (Request.Headers.ContainsKey("HX-Request"))
            {
                Response.Headers.Append("HX-Push-Url", "/Identity/Lockout");
                return PartialView("_Lockout");
            }
            return View();
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult ResetPasswordConfirmation()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Logout(string returnUrl = null!)
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation("User logged out.");

            if (returnUrl != null)
            {
                return LocalRedirect(returnUrl);
            }
            return RedirectToAction("Index", "Home");
        }

        #endregion Lockout
    }
}