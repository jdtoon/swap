namespace SwapPhase15.Events;

/// <summary>
/// Event payload for user click actions.
/// </summary>
public class UserClickedEvent
{
    public string Message { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty; // e.g., "form" or "button"
}