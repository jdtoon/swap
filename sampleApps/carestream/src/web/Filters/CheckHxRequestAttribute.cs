using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;

namespace carestream.web.Filters
{
    public class CheckHxRequestAttribute : ActionFilterAttribute
    {
        private readonly string _redirectAction;
        private readonly string _redirectController;

        public CheckHxRequestAttribute(string redirectAction = "Index", string redirectController = "Home")
        {
            _redirectAction = redirectAction;
            _redirectController = redirectController;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var httpContext = context.HttpContext;
            var request = httpContext.Request;

            var isHxRequest = request.Headers.ContainsKey("HX-Request") &&
                              request.Headers["HX-Request"] == "true";

            if (isHxRequest)
            {
                base.OnActionExecuting(context);
                return;
            }

            // It's a full page request
            var controllerActionDescriptor = context.ActionDescriptor as ControllerActionDescriptor;
            var currentAction = controllerActionDescriptor?.ActionName;
            var currentController = controllerActionDescriptor?.ControllerName;

            // 1. Allow if the action explicitly allows anonymous access
            bool isActionAnonymous = controllerActionDescriptor?.MethodInfo.GetCustomAttributes(typeof(AllowAnonymousAttribute), true).Any() ?? false;
            bool isControllerAnonymous = controllerActionDescriptor?.ControllerTypeInfo.GetCustomAttributes(typeof(AllowAnonymousAttribute), true).Any() ?? false;

            if (isActionAnonymous || isControllerAnonymous)
            {
                base.OnActionExecuting(context);
                return;
            }

            // 2. Specifically allow all GET actions within the AccountController to handle OIDC flows.
            //    OIDC often involves multiple GET redirects (to provider, then callbacks to your app).
            //    The Login and Logout actions themselves might trigger redirects that come back as full page GETs.
            if (string.Equals(currentController, "Account", StringComparison.OrdinalIgnoreCase) &&
                request.Method.Equals("GET", StringComparison.OrdinalIgnoreCase))
            {
                // Check if it's the post-logout redirect URI defined in AccountController/Logout
                // This is a common pattern, but the exact path might depend on your OIDC setup
                // and whether Logto adds specific query parameters.
                // For instance, if RedirectUri in Logout() is Url.Action("Index", "Home"),
                // then a request to /Home/Index after logout should be allowed.
                // However, the Home/Index might be protected, leading to another redirect to Login.
                // The key is to allow the initial callbacks from the OIDC provider to the AccountController.
                base.OnActionExecuting(context);
                return;
            }

            // 3. Don't redirect if we are already at the intended default target of this filter
            //    (e.g., Home/Index for logged-in users if the page is part of the HTMX shell)
            //    AND the request method is GET.
            if (request.Method.Equals("GET", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(currentAction, _redirectAction, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(currentController, _redirectController, StringComparison.OrdinalIgnoreCase))
            {
                // This condition assumes that _redirectAction/_redirectController is an action
                // that IS designed to serve a full page (the main HTMX shell).
                base.OnActionExecuting(context);
                return;
            }

            // 4. If it's a GET request to an action that is NOT explicitly anonymous,
            //    NOT the AccountController, and NOT the filter's default redirect target,
            //    then assume it might be an HTMX-only endpoint and redirect.
            //    This is the primary purpose of the filter for protecting partial-only endpoints.
            if (request.Method.Equals("GET", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"[CheckHxRequest] Full page GET request to {currentController}/{currentAction}. Redirecting to default HTMX shell at {_redirectController}/{_redirectAction}.");
                context.Result = new RedirectToActionResult(_redirectAction, _redirectController, null);
            }
            else
            {
                // For non-GET requests (POST, PUT, DELETE etc.) that are not HTMX,
                // let them proceed. The controller action itself should handle
                // if it was expecting an HTMX request (e.g., by checking the header).
                base.OnActionExecuting(context);
            }
        }
    }
}