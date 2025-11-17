namespace TaskFlow.Models;

/// <summary>
/// Task priority levels
/// </summary>
public enum TaskPriority
{
    Low,
    Medium,
    High,
    Critical
}

/// <summary>
/// Task status in workflow
/// </summary>
public enum TaskStatus
{
    Todo,
    InProgress,
    Review,
    Done
}

/// <summary>
/// Core task entity
/// </summary>
public class TaskItem
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TaskPriority Priority { get; set; }
    public TaskStatus Status { get; set; }
    public int ProjectId { get; set; }
    public string? AssignedTo { get; set; } // Team member ID
    public DateTime CreatedAt { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? CompletedAt { get; set; }
    public List<Comment> Comments { get; set; } = new();
    public int Order { get; set; } // For ordering within status column
}

/// <summary>
/// Project grouping tasks
/// </summary>
public class Project
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Color { get; set; } = "#3b82f6"; // Blue default
    public DateTime CreatedAt { get; set; }
    public List<TaskItem> Tasks { get; set; } = new();
}

/// <summary>
/// Comment on a task
/// </summary>
public class Comment
{
    public int Id { get; set; }
    public int TaskId { get; set; }
    public string AuthorId { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? EditedAt { get; set; }
}

/// <summary>
/// Team member (simplified - no auth in demo)
/// </summary>
public class TeamMember
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string AvatarColor { get; set; } = "#10b981"; // Green default
    public bool IsOnline { get; set; }
    public DateTime? LastSeen { get; set; }
}

/// <summary>
/// Activity log entry
/// </summary>
public class Activity
{
    public int Id { get; set; }
    public string Type { get; set; } = string.Empty; // "task.created", "comment.added", etc.
    public string Description { get; set; } = string.Empty;
    public string ActorId { get; set; } = string.Empty;
    public string ActorName { get; set; } = string.Empty;
    public int? TaskId { get; set; }
    public int? ProjectId { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Notification for a user
/// </summary>
public class Notification
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // "assignment", "mention", "deadline", etc.
    public string Message { get; set; } = string.Empty;
    public int? TaskId { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Input model for creating/updating tasks
/// </summary>
public class TaskInput
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TaskPriority Priority { get; set; }
    public int ProjectId { get; set; }
    public string? AssignedTo { get; set; }
    public DateTime? DueDate { get; set; }
}

/// <summary>
/// Input model for creating projects
/// </summary>
public class ProjectInput
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Color { get; set; } = "#3b82f6";
}

/// <summary>
/// Input model for creating comments
/// </summary>
public class CommentInput
{
    public string Content { get; set; } = string.Empty;
}

/// <summary>
/// Team statistics for dashboard
/// </summary>
public class TeamStats
{
    public int TotalTasks { get; set; }
    public int CompletedTasks { get; set; }
    public int InProgressTasks { get; set; }
    public int OverdueTasks { get; set; }
    public Dictionary<string, int> TasksByMember { get; set; } = new();
    public Dictionary<TaskPriority, int> TasksByPriority { get; set; } = new();
}
