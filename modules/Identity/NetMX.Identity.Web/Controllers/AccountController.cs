using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NetMX.AspNetCore.Mvc.Htmx;
using NetMX.Events;
using NetMX.Identity.Contracts.Services;
using NetMX.Identity.Contracts.Users;
using NetMX.Identity.Core.Users;
using System.Security.Claims;

namespace NetMX.Identity.Web.Controllers;

/// <summary>
/// Account management controller with HTMX-powered login, register, and profile flows.
/// Uses ASP.NET Core Identity's SignInManager for authentication.
/// </summary>
public class AccountController : Controller
{
    private readonly IUserAppService _userAppService;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly UserManager<AppUser> _userManager;

    public AccountController(
        IUserAppService userAppService,
        SignInManager<AppUser> signInManager,
        UserManager<AppUser> userManager)
    {
        _userAppService = userAppService;
        _signInManager = signInManager;
        _userManager = userManager;
    }

    /// <summary>
    /// Display login page
    /// </summary>
    [HttpGet("/account/login")]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    /// <summary>
    /// Process login with HTMX support (returns partial for HTMX requests)
    /// </summary>
    [HttpPost("/account/login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login([FromForm] LoginDto model, string? returnUrl = null)
    {
        if (!ModelState.IsValid)
        {
            if (Request.IsHtmx())
            {
                return PartialView("_LoginForm", model);
            }
            return View(model);
        }

        // Use SignInManager for authentication
        var result = await _signInManager.PasswordSignInAsync(
            model.UserName, 
            model.Password, 
            model.RememberMe, 
            lockoutOnFailure: true);

        if (result.Succeeded)
        {
            // Get user for triggering events
            var user = await _userManager.FindByNameAsync(model.UserName);

            if (Request.IsHtmx())
            {
                // Trigger success event and redirect
                this.HxTrigger(DomainEvents.Login.Success, new { userId = user?.Id, userName = user?.UserName });
                this.HxRedirect(returnUrl ?? "/");
                return Ok();
            }

            return LocalRedirect(returnUrl ?? "/");
        }

        if (result.IsLockedOut)
        {
            if (Request.IsHtmx())
            {
                // Trigger account locked event
                this.HxTrigger(DomainEvents.Account.Locked, new { userName = model.UserName });
                // Show lockout notification
                return PartialView("_LockedOut");
            }
            return View("LockedOut");
        }

        if (result.RequiresTwoFactor)
        {
            if (Request.IsHtmx())
            {
                // Trigger redirect to 2FA page
                this.HxRedirect("/account/two-factor");
                return Ok();
            }
            return RedirectToAction(nameof(TwoFactor), new { returnUrl });
        }

        // Failed login
        ModelState.AddModelError(string.Empty, "Invalid username or password");
        
        if (Request.IsHtmx())
        {
            // Trigger failed login event
            this.HxTrigger(DomainEvents.Login.Failed, new { userName = model.UserName, reason = "Invalid credentials" });
            // Return form with error message
            return PartialView("_LoginForm", model);
        }
        return View(model);
    }

    /// <summary>
    /// Display register page
    /// </summary>
    [HttpGet("/account/register")]
    public IActionResult Register()
    {
        return View();
    }

    /// <summary>
    /// Process registration with HTMX support
    /// </summary>
    [HttpPost("/account/register")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register([FromForm] CreateUserDto model)
    {
        if (!ModelState.IsValid)
        {
            if (Request.IsHtmx())
            {
                return PartialView("_RegisterForm", model);
            }
            return View(model);
        }

        try
        {
            var user = await _userAppService.CreateAsync(model);

            if (Request.IsHtmx())
            {
                // Trigger success event and show success message
                this.HxTrigger(DomainEvents.Registration.Success, new 
                { 
                    userId = user.Id, 
                    email = user.Email,
                    userName = user.UserName 
                });
                return PartialView("_RegisterSuccess");
            }

            return RedirectToAction(nameof(Login));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            
            if (Request.IsHtmx())
            {
                this.HxTrigger(DomainEvents.Registration.Failed, new 
                { 
                    email = model.Email, 
                    errors = new[] { ex.Message } 
                });
                return PartialView("_RegisterForm", model);
            }
            return View(model);
        }
    }

    /// <summary>
    /// Display profile page (requires authentication)
    /// </summary>
    [HttpGet("/account/profile")]
    public async Task<IActionResult> Profile()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null)
        {
            return RedirectToAction(nameof(Login));
        }

        var user = await _userAppService.GetAsync(Guid.Parse(userIdClaim));
        return View(user);
    }

    /// <summary>
    /// Update profile with HTMX support
    /// </summary>
    [HttpPost("/account/profile")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProfile([FromForm] UpdateUserDto model)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null)
        {
            return RedirectToAction(nameof(Login));
        }

        var userId = Guid.Parse(userIdClaim);

        if (!ModelState.IsValid)
        {
            if (Request.IsHtmx())
            {
                return PartialView("_ProfileForm", model);
            }
            var user = await _userAppService.GetAsync(userId);
            return View("Profile", user);
        }

        try
        {
            var updatedUser = await _userAppService.UpdateAsync(userId, model);

            if (Request.IsHtmx())
            {
                this.HxTrigger(DomainEvents.Profile.Updated, new 
                { 
                    userId, 
                    changedFields = new[] { "Email", "FirstName", "LastName" } // Simplified
                });
                return PartialView("_ProfileSuccess", updatedUser);
            }

            return RedirectToAction(nameof(Profile));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            
            if (Request.IsHtmx())
            {
                return PartialView("_ProfileForm", model);
            }
            
            var user = await _userAppService.GetAsync(userId);
            return View("Profile", user);
        }
    }

    /// <summary>
    /// Change password with HTMX support
    /// </summary>
    [HttpPost("/account/change-password")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword([FromForm] ChangePasswordDto model)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null)
        {
            return RedirectToAction(nameof(Login));
        }

        var userId = Guid.Parse(userIdClaim);

        if (!ModelState.IsValid)
        {
            if (Request.IsHtmx())
            {
                return PartialView("_ChangePasswordForm", model);
            }
            return RedirectToAction(nameof(Profile));
        }

        try
        {
            await _userAppService.ChangePasswordAsync(userId, model);

            if (Request.IsHtmx())
            {
                this.HxTrigger("password:changed");
                return PartialView("_PasswordChangeSuccess");
            }

            TempData["SuccessMessage"] = "Password changed successfully";
            return RedirectToAction(nameof(Profile));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            
            if (Request.IsHtmx())
            {
                return PartialView("_ChangePasswordForm", model);
            }
            
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Profile));
        }
    }

    /// <summary>
    /// Logout
    /// </summary>
    [HttpPost("/account/logout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();

        if (Request.IsHtmx())
        {
            this.HxTrigger("logout:success");
            this.HxRedirect("/");
            return Ok();
        }

        return RedirectToAction(nameof(Login));
    }

    /// <summary>
    /// Two-factor authentication page (placeholder for now)
    /// </summary>
    [HttpGet("/account/two-factor")]
    public IActionResult TwoFactor(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    /// <summary>
    /// Access denied page
    /// </summary>
    [HttpGet("/account/access-denied")]
    public IActionResult AccessDenied()
    {
        return View();
    }
}
