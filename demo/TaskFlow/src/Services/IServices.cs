using TaskFlow.Models;
using TaskStatus = TaskFlow.Models.TaskStatus;

namespace TaskFlow.Services;

/// <summary>
/// Service for managing tasks
/// </summary>
public interface ITaskService
{
    List<TaskItem> GetAll();
    List<TaskItem> GetByProject(int projectId);
    List<TaskItem> GetByStatus(TaskStatus status);
    List<TaskItem> GetByAssignee(string userId);
    TaskItem? Get(int id); // Added Get alias
    TaskItem? GetById(int id);
    TaskItem Create(TaskInput input, string creatorId);
    TaskItem Update(int id, TaskInput input);
    void Delete(int id);
    void UpdateStatus(int id, TaskStatus newStatus); // Added UpdateStatus
    void UpdatePriority(int id, TaskPriority newPriority); // Added UpdatePriority
    TaskItem ChangeStatus(int id, TaskStatus newStatus);
    TaskItem ChangePriority(int id, TaskPriority newPriority);
    TaskItem Assign(int id, string userId);
    TaskItem Unassign(int id);
    List<TaskItem> GetOverdue();
    int GetCountByStatus(TaskStatus status);
}

/// <summary>
/// Service for managing projects
/// </summary>
public interface IProjectService
{
    List<Project> GetAll();
    Project? Get(int id); // Added Get alias
    Project? GetById(int id);
    Project Create(ProjectInput input);
    Project Update(int id, ProjectInput input);
    void Delete(int id);
    double GetProgress(int projectId);
    Dictionary<TaskStatus, int> GetTaskBreakdown(int projectId);
}

/// <summary>
/// Service for managing team members
/// </summary>
public interface ITeamService
{
    List<TeamMember> GetAll();
    List<TeamMember> GetOnline();
    TeamMember? Get(string id); // Added Get alias
    TeamMember? GetById(string id);
    void SetOnline(string id, bool online);
    int GetTaskCount(string userId);
    int GetActiveTaskCount(string userId); // Added GetActiveTaskCount
    bool IsOverloaded(string userId, int threshold = 10);
    TeamStats GetStats();
}

/// <summary>
/// Service for managing comments
/// </summary>
public interface ICommentService
{
    List<Comment> GetByTask(int taskId);
    Comment? Get(int id); // Added Get alias
    Comment? GetById(int id);
    Comment Create(int taskId, CommentInput input, string authorId, string authorName);
    Comment Update(int id, CommentInput input); // Changed signature
    void Delete(int id);
    int GetCountForTask(int taskId);
}

/// <summary>
/// Service for managing activity log
/// </summary>
public interface IActivityService
{
    List<Activity> GetRecent(int count = 20);
    List<Activity> GetByProject(int projectId, int count = 20);
    List<Activity> GetByTask(int taskId);
    void LogActivity(string description, int? taskId = null, int? projectId = null, string userId = "demo-user"); // Added LogActivity alias
    void Log(string type, string description, string actorId, string actorName, int? taskId = null, int? projectId = null);
}

/// <summary>
/// Service for managing notifications
/// </summary>
public interface INotificationService
{
    List<Notification> GetForUser(string userId);
    List<Notification> GetUnreadForUser(string userId);
    Notification? Get(int id);
    Notification? GetById(int id);
    int GetUnreadCount(string userId);
    Notification Create(string userId, string type, string message, int? taskId = null);
    void MarkAsRead(int id);
    void MarkAllAsRead(string userId);
    void ClearAll(string userId);
}
