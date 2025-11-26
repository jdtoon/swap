namespace SwapLab.Models;

/// <summary>
/// Represents a pattern demo in SwapLab.
/// </summary>
public record PatternInfo(
    string Id,
    string Title,
    string Description,
    string Category,
    string[] Tags
);

/// <summary>
/// View model for pattern demos.
/// </summary>
public class PatternViewModel
{
    public required PatternInfo Pattern { get; init; }
    public required string ControllerCode { get; init; }
    public required string ViewCode { get; init; }
    public string? EventCode { get; init; }
    public string? Notes { get; init; }
}

/// <summary>
/// Available pattern categories.
/// </summary>
public static class PatternCategories
{
    public const string Basics = "Basics";
    public const string State = "State Management";
    public const string Events = "Event Chains";
    public const string Components = "Multi-Component";
    public const string Forms = "Forms & Validation";
    public const string Realtime = "Realtime";
}
