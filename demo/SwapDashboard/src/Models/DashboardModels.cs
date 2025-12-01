using Swap.Htmx.State;

namespace SwapDashboard.Models;

/// <summary>
/// Dashboard state that coordinates all components.
/// Inherits from SwapState for automatic binding and OOB sync.
/// </summary>
public class DashboardState : SwapState
{
    public int? SelectedProjectId { get; set; }
    public int? SelectedTaskId { get; set; }
    public int? SelectedMemberId { get; set; }
    public string ViewMode { get; set; } = "board"; // board, list, timeline
    public string StatusFilter { get; set; } = "all";
    public string PriorityFilter { get; set; } = "all";
    public string SearchTerm { get; set; } = "";
    public int Page { get; set; } = 1;
}

/// <summary>
/// Project model.
/// </summary>
public class Project
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Color { get; set; } = "#6366f1";
    public int TaskCount { get; set; }
    public int CompletedCount { get; set; }
    public decimal Progress => TaskCount > 0 ? (decimal)CompletedCount / TaskCount * 100 : 0;
    public DateTime CreatedAt { get; set; }
    public DateTime? DueDate { get; set; }
}

/// <summary>
/// Task model.
/// </summary>
public class TaskItem
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public TaskStatus Status { get; set; } = TaskStatus.Todo;
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;
    public int? AssigneeId { get; set; }
    public TeamMember? Assignee { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? DueDate { get; set; }
    public List<string> Tags { get; set; } = new();
    public int CommentCount { get; set; }
    public int AttachmentCount { get; set; }
}

public enum TaskStatus
{
    Todo,
    InProgress,
    Review,
    Done
}

public enum TaskPriority
{
    Low,
    Medium,
    High,
    Urgent
}

/// <summary>
/// Team member model.
/// </summary>
public class TeamMember
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string Avatar { get; set; } = "";
    public string Role { get; set; } = "";
    public int ActiveTaskCount { get; set; }
    public bool IsOnline { get; set; }
}

/// <summary>
/// Activity feed item.
/// </summary>
public class ActivityItem
{
    public int Id { get; set; }
    public string Type { get; set; } = "";
    public string Description { get; set; } = "";
    public int? UserId { get; set; }
    public string UserName { get; set; } = "";
    public string UserAvatar { get; set; } = "";
    public int? TaskId { get; set; }
    public string? TaskTitle { get; set; }
    public int? ProjectId { get; set; }
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Notification model.
/// </summary>
public class Notification
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Message { get; set; } = "";
    public string Type { get; set; } = "info"; // info, warning, success, error
    public bool IsRead { get; set; }
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Comment model.
/// </summary>
public class Comment
{
    public int Id { get; set; }
    public int TaskId { get; set; }
    public int AuthorId { get; set; }
    public string AuthorName { get; set; } = "";
    public string AuthorAvatar { get; set; } = "";
    public string Content { get; set; } = "";
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Dashboard stats.
/// </summary>
public class DashboardStats
{
    public int TotalTasks { get; set; }
    public int CompletedTasks { get; set; }
    public int InProgressTasks { get; set; }
    public int OverdueTasks { get; set; }
    public decimal CompletionRate => TotalTasks > 0 ? (decimal)CompletedTasks / TotalTasks * 100 : 0;
}

/// <summary>
/// View model for the main dashboard.
/// </summary>
public class DashboardViewModel
{
    public DashboardState State { get; set; } = new();
    public List<Project> Projects { get; set; } = new();
    public Project? SelectedProject { get; set; }
    public List<TaskItem> Tasks { get; set; } = new();
    public TaskItem? SelectedTask { get; set; }
    public List<TeamMember> TeamMembers { get; set; } = new();
    public TeamMember? SelectedMember { get; set; }
    public List<ActivityItem> Activities { get; set; } = new();
    public List<Notification> Notifications { get; set; } = new();
    public DashboardStats Stats { get; set; } = new();
    public int UnreadNotificationCount { get; set; }
}

/// <summary>
/// View model for the kanban board.
/// </summary>
public class KanbanViewModel
{
    public List<TaskItem> TodoTasks { get; set; } = new();
    public List<TaskItem> InProgressTasks { get; set; } = new();
    public List<TaskItem> ReviewTasks { get; set; } = new();
    public List<TaskItem> DoneTasks { get; set; } = new();
}

/// <summary>
/// View model for creating a new task.
/// </summary>
public class CreateTaskViewModel
{
    public int ProjectId { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;
    public int? AssigneeId { get; set; }
    public List<Project> Projects { get; set; } = new();
    public List<TeamMember> TeamMembers { get; set; } = new();
}
