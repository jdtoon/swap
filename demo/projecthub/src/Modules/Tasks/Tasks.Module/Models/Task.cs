using TaskStatus = ProjectHub.Modules.Tasks.Contracts.TaskStatus;
using TaskPriority = ProjectHub.Modules.Tasks.Contracts.TaskPriority;

namespace ProjectHub.Modules.Tasks.Module.Models;

public class Task
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TaskStatus Status { get; set; } = TaskStatus.Backlog;
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;
    public int? AssignedToUserId { get; set; }
    public DateTimeOffset? DueDate { get; set; }
    public int Position { get; set; }
    public bool IsArchived { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
