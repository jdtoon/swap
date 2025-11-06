namespace Swap.Htmx.Events;

/// <summary>
/// Strongly-typed event key for Swap.Htmx APIs. Use this instead of raw strings in backend code.
/// </summary>
public readonly record struct EventKey(string Name)
{
    public override string ToString() => Name;

    // Allow seamless use where a string is required (headers, logging, etc.)
    public static implicit operator string(EventKey key) => key.Name;
}
