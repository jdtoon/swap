namespace Swap.CLI.Infrastructure;

/// <summary>
/// Console output helpers with colors
/// </summary>
public static class ConsoleHelper
{
    public static void WriteSuccess(string message)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"✅ {message}");
        Console.ResetColor();
    }

    public static void WriteError(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"❌ {message}");
        Console.ResetColor();
    }

    public static void WriteWarning(string message)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"⚠️  {message}");
        Console.ResetColor();
    }

    public static void WriteInfo(string message)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"ℹ️  {message}");
        Console.ResetColor();
    }

    public static void WriteHeader(string message)
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.WriteLine($"🚀 {message}");
        Console.WriteLine(new string('═', message.Length + 3));
        Console.ResetColor();
    }

    public static void WriteStep(int step, string message)
    {
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine($"  [{step}] {message}");
        Console.ResetColor();
    }

    public static void WriteProgress(string message)
    {
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.Write($"   → {message}...");
        Console.ResetColor();
    }

    public static void WriteProgressDone()
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine(" Done ✓");
        Console.ResetColor();
    }
}

