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
    public static void ShowToast(this HttpResponse response, string message, ToastType type = ToastType.Info)
    {
        var toastJson = $"{{\"type\": \"{type.ToString().ToLower()}\", \"message\": \"{EscapeJson(message)}\"}}";
        
        // Check if HX-Trigger header already exists
        if (response.Headers.ContainsKey("HX-Trigger"))
        {
            var existingTrigger = response.Headers["HX-Trigger"].ToString();
            
            // If it's already a JSON object, merge in showToast
            if (existingTrigger.StartsWith("{"))
            {
                existingTrigger = existingTrigger.TrimEnd('}');
                response.Headers["HX-Trigger"] = $"{existingTrigger}, \"showToast\": {toastJson}}}";
            }
            else
            {
                // Simple event name, convert to JSON object
                response.Headers["HX-Trigger"] = $"{{\"{existingTrigger}\": null, \"showToast\": {toastJson}}}";
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
