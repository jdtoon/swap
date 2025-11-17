using Microsoft.AspNetCore.Http;

namespace Swap.Htmx;

/// <summary>
/// Extension methods for displaying toast notifications in HTMX responses
/// </summary>
public static class SwapToastExtensions
{
    /// <summary>
    /// Show a toast notification with the specified message and type
    /// </summary>
    /// <param name="response">The HTTP response</param>
    /// <param name="message">The message to display</param>
    /// <param name="type">The type of toast (success, error, warning, info)</param>
    /// <param name="position">The position of the toast (top-right, top-left, bottom-right, bottom-left)</param>
    public static void ShowToast(this HttpResponse response, string message, ToastType type = ToastType.Info, ToastPosition position = ToastPosition.TopRight)
    {
        // Skip toasts on history restore requests (browser back/forward) to prevent duplicates
        var httpContext = response.HttpContext;
        if (httpContext.Request.IsHtmxHistoryRestoreRequest())
        {
            return;
        }
        
        // Prevent HTMX from caching responses with toasts - defense in depth
        response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
        response.Headers["Pragma"] = "no-cache";
        response.Headers["Expires"] = "0";
        
        var positionStr = position switch
        {
            ToastPosition.TopRight => "top-right",
            ToastPosition.TopLeft => "top-left",
            ToastPosition.BottomRight => "bottom-right",
            ToastPosition.BottomLeft => "bottom-left",
            _ => "top-right"
        };
        
        var toastJson = $"{{\"type\": \"{type.ToString().ToLower()}\", \"message\": \"{EscapeJson(message)}\", \"position\": \"{positionStr}\"}}";
        
        // Check if HX-Trigger header already exists
        if (response.Headers.ContainsKey("HX-Trigger"))
        {
            var existingTrigger = response.Headers["HX-Trigger"].ToString();

            // If it's already a JSON object, merge in showToast
            if (existingTrigger.TrimStart().StartsWith("{"))
            {
                existingTrigger = existingTrigger.TrimEnd('}');
                response.Headers["HX-Trigger"] = $"{existingTrigger}, \"showToast\": {toastJson}}}";
            }
            else
            {
                // Comma-separated event names → convert to JSON object with each key
                var names = existingTrigger
                    .Split(',')
                    .Select(s => s.Trim())
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                var keysJson = names.Length == 0
                    ? string.Empty
                    : string.Join(", ", names.Select(n => $"\"{n}\": null"));

                var merged = string.IsNullOrEmpty(keysJson)
                    ? $"{{\"showToast\": {toastJson}}}"
                    : $"{{{keysJson}, \"showToast\": {toastJson}}}";

                response.Headers["HX-Trigger"] = merged;
            }
        }
        else
        {
            response.Headers.Append("HX-Trigger", $"{{\"showToast\": {toastJson}}}");
        }
    }

    /// <summary>
    /// Show a success toast notification
    /// </summary>
    public static void ShowSuccessToast(this HttpResponse response, string message)
    {
        ShowToast(response, message, ToastType.Success);
    }

    /// <summary>
    /// Show an error toast notification
    /// </summary>
    public static void ShowErrorToast(this HttpResponse response, string message)
    {
        ShowToast(response, message, ToastType.Error);
    }

    /// <summary>
    /// Show a warning toast notification
    /// </summary>
    public static void ShowWarningToast(this HttpResponse response, string message)
    {
        ShowToast(response, message, ToastType.Warning);
    }

    /// <summary>
    /// Show an info toast notification
    /// </summary>
    public static void ShowInfoToast(this HttpResponse response, string message)
    {
        ShowToast(response, message, ToastType.Info);
    }

    private static string EscapeJson(string str)
    {
        return str
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")
            .Replace("\t", "\\t");
    }
}

/// <summary>
/// Types of toast notifications
/// </summary>
public enum ToastType
{
    Success,
    Error,
    Warning,
    Info
}

/// <summary>
/// Positions for toast notifications
/// </summary>
public enum ToastPosition
{
    TopRight,
    TopLeft,
    BottomRight,
    BottomLeft
}
