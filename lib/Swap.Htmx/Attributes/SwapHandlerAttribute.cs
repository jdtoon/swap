namespace Swap.Htmx.Attributes;

/// <summary>
/// Marks a class as a Swap event handler with optional priority for execution ordering.
/// Handlers are discovered via assembly scanning and executed in priority order.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class SwapHandlerAttribute : Attribute
{
    /// <summary>
    /// Gets the execution priority. Lower values execute first. Default is 0.
    /// </summary>
    public int Priority { get; set; } = 0;
}