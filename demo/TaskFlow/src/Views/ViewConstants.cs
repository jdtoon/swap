using TaskFlow.Models;

namespace TaskFlow.Views;

/// <summary>
/// Constants for task view names
/// </summary>
public static class TaskViews
{
    public const string Index = "Index";
    public const string Board = "_TaskBoard";
    public const string Card = "_TaskCard";
    public const string TaskCard = "TaskCard";
    public const string TaskColumn = "TaskColumn";
    public const string List = "_TaskList";
    public const string Details = "_TaskDetails";
    public const string Form = "_TaskForm";
    public const string Count = "_TaskCount";
}

/// <summary>
/// Constants for task element IDs
/// </summary>
public static class TaskElements
{
    public const string Board = "task-board";
    public const string TodoColumn = "task-column-todo";
    public const string InProgressColumn = "task-column-inprogress";
    public const string ReviewColumn = "task-column-review";
    public const string DoneColumn = "task-column-done";
    public const string Count = "task-count";
    
    public static string Card(int taskId) => $"task-card-{taskId}";
    public static string Column(Models.TaskStatus status) => $"task-column-{status.ToString().ToLower()}";
    public static string Comments(int taskId) => $"task-comments-{taskId}";
    public static string CommentCount(int taskId) => $"task-comment-count-{taskId}";
}

/// <summary>
/// Constants for project view names
/// </summary>
public static class ProjectViews
{
    public const string Index = "Index";
    public const string Details = "Details";
    public const string List = "_ProjectList";
    public const string Card = "_ProjectCard";
    public const string ProgressBar = "ProgressBar";
    public const string Stats = "_ProjectStats";
}

/// <summary>
/// Constants for project element IDs
/// </summary>
public static class ProjectElements
{
    public const string List = "project-list";
    public static string Card(int projectId) => $"project-card-{projectId}";
    public static string Progress(int projectId) => $"project-progress-{projectId}";
}

/// <summary>
/// Constants for comment view names
/// </summary>
public static class CommentViews
{
    public const string List = "List";
    public const string Thread = "_CommentThread";
    public const string Single = "_Comment";
    public const string CommentCard = "CommentCard";
    public const string Form = "_CommentForm";
    public const string Count = "Count";
}

/// <summary>
/// Constants for comment element IDs
/// </summary>
public static class CommentElements
{
    public static string Thread(int taskId) => $"comment-thread-{taskId}";
    public static string List(int taskId) => $"comment-list-{taskId}";
    public static string Single(int commentId) => $"comment-{commentId}";
    public static string Card(int commentId) => $"comment-card-{commentId}";
    public static string Count(int taskId) => $"comment-count-{taskId}";
}

/// <summary>
/// Constants for dashboard view names
/// </summary>
public static class DashboardViews
{
    public const string Index = "Index";
    public const string Stats = "Stats";
    public const string Activity = "Activity";
    public const string TeamList = "TeamList";
    public const string Presence = "_TeamPresence";
}

/// <summary>
/// Constants for dashboard element IDs
/// </summary>
public static class DashboardElements
{
    public const string Stats = "dashboard-stats";
    public const string Activity = "dashboard-activity";
    public const string TeamList = "team-list";
    public const string Presence = "team-presence";
}

/// <summary>
/// Constants for notification view names
/// </summary>
public static class NotificationViews
{
    public const string Index = "Index";
    public const string Bell = "Bell";
    public const string List = "List";
    public const string Card = "Card";
    public const string Single = "_Notification";
}

/// <summary>
/// Constants for notification element IDs
/// </summary>
public static class NotificationElements
{
    public const string Bell = "notification-bell";
    public const string List = "notification-list";
    public const string Count = "notification-count";
    public static string Card(int notificationId) => $"notification-card-{notificationId}";
}
