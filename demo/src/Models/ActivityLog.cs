namespace TaskFlow.Models;

public class ActivityLog
{
    public int Id { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Icon { get; set; } = "fas fa-info-circle";
    public string ColorClass { get; set; } = "is-info";
}
