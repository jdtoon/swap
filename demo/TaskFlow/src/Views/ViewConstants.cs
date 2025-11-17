namespace TaskFlow.Views;

/// <summary>
/// Constants for task view names
/// </summary>
public static class TaskViews
{
    public const string Board = "_TaskBoard";
    public const string Card = "_TaskCard";
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
    public static string Comments(int taskId) => $"task-comments-{taskId}";
    public static string CommentCount(int taskId) => $"task-comment-count-{taskId}";
}

/// <summary>
/// Constants for project view names
/// </summary>
public static class ProjectViews
{
    public const string List = "_ProjectList";
    public const string Card = "_ProjectCard";
    public const string ProgressBar = "_ProgressBar";
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
    public const string Thread = "_CommentThread";
    public const string Single = "_Comment";
    public const string Form = "_CommentForm";
    public const string Count = "_CommentCount";
}

/// <summary>
/// Constants for comment element IDs
/// </summary>
public static class CommentElements
{
    public static string Thread(int taskId) => $"comment-thread-{taskId}";
    public static string Single(int commentId) => $"comment-{commentId}";
}

/// <summary>
/// Constants for dashboard view names
/// </summary>
public static class DashboardViews
{
    public const string Stats = "_TeamStats";
    public const string Activity = "_ActivityFeed";
    public const string Presence = "_TeamPresence";
}

/// <summary>
/// Constants for dashboard element IDs
/// </summary>
public static class DashboardElements
{
    public const string Stats = "dashboard-stats";
    public const string Activity = "dashboard-activity";
    public const string Presence = "team-presence";
}

/// <summary>
/// Constants for notification view names
/// </summary>
public static class NotificationViews
{
    public const string Bell = "_NotificationBell";
    public const string List = "_NotificationList";
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
}
