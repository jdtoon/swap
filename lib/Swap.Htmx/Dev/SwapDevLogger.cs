using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Swap.Htmx.Dev;

/// <summary>
/// Development-only logging for Swap.Htmx internals.
/// Enable via appsettings.json:
/// {
///   "Logging": {
///     "LogLevel": {
///       "Swap.Htmx": "Debug"
///     }
///   }
/// }
/// Or environment variable: SWAP_DEV_LOGGING=true (for console-only output)
/// </summary>
internal static class SwapDevLogger
{
    private static readonly bool ConsoleEnabled = 
        Environment.GetEnvironmentVariable("SWAP_DEV_LOGGING")?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false;

    [Conditional("DEBUG")]
    public static void LogSwapEvent(ILogger? logger, string eventName, string message)
    {
        if (ConsoleEnabled)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"[SwapEvent] Event: {eventName}, {message}");
            Console.ResetColor();
        }
        logger?.LogDebug("[SwapEvent] Event: {EventName}, {Message}", eventName, message);
    }

    [Conditional("DEBUG")]
    public static void LogEventChain(ILogger? logger, string eventName, int partialCount, int toastCount)
    {
        if (ConsoleEnabled)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"[EventChainExecutor] Event: {eventName}, Partials: {partialCount}, Toasts: {toastCount}");
            Console.ResetColor();
        }
        logger?.LogDebug("[EventChainExecutor] Event: {EventName}, Partials: {PartialCount}, Toasts: {ToastCount}", 
            eventName, partialCount, toastCount);
    }

    [Conditional("DEBUG")]
    public static void LogToast(ILogger? logger, string type, string message)
    {
        if (ConsoleEnabled)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[SwapActionResult] Applying toast: {type} - {message}");
            Console.ResetColor();
        }
        logger?.LogDebug("[SwapActionResult] Applying toast: {Type} - {Message}", type, message);
    }

    [Conditional("DEBUG")]
    public static void LogHeader(ILogger? logger, string headerName, string value)
    {
        if (ConsoleEnabled)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"[SwapActionResult] {headerName}: {value}");
            Console.ResetColor();
        }
        logger?.LogDebug("[SwapActionResult] {HeaderName}: {Value}", headerName, value);
    }

    [Conditional("DEBUG")]
    public static void LogExecutor(ILogger? logger, string message)
    {
        if (ConsoleEnabled)
        {
            Console.WriteLine($"[SwapEvent] {message}");
        }
        logger?.LogDebug("[SwapEvent] {Message}", message);
    }
}