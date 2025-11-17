using System.Diagnostics;

namespace Swap.Htmx.Dev;

/// <summary>
/// Development-only logging for Swap.Htmx internals.
/// Enabled via environment variable: SWAP_DEV_LOGGING=true
/// </summary>
internal static class SwapDevLogger
{
    private static readonly bool IsEnabled = 
        Environment.GetEnvironmentVariable("SWAP_DEV_LOGGING")?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false;

    [Conditional("DEBUG")]
    public static void Log(string category, string message)
    {
        if (IsEnabled)
        {
            Console.WriteLine($"[Swap:{category}] {message}");
        }
    }

    [Conditional("DEBUG")]
    public static void LogEvent(string eventName, string message)
    {
        if (IsEnabled)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"[Swap:Event:{eventName}] {message}");
            Console.ResetColor();
        }
    }

    [Conditional("DEBUG")]
    public static void LogToast(string type, string message)
    {
        if (IsEnabled)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[Swap:Toast:{type}] {message}");
            Console.ResetColor();
        }
    }

    [Conditional("DEBUG")]
    public static void LogHeader(string headerName, string value)
    {
        if (IsEnabled)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"[Swap:Header:{headerName}] {value}");
            Console.ResetColor();
        }
    }
}