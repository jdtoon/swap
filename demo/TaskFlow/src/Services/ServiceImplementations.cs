using TaskFlow.Models;

namespace TaskFlow.Services;

/// <summary>
/// In-memory team service implementation
/// </summary>
public class TeamService : ITeamService
{
    private readonly List<TeamMember> _members = new();
    private readonly ITaskService _taskService;
    private readonly object _lock = new();

    public TeamService(ITaskService taskService)
    {
        _taskService = taskService;
        SeedData();
    }

    public List<TeamMember> GetAll()
    {
        lock (_lock)
        {
            return _members.ToList();
        }
    }

    public List<TeamMember> GetOnline()
    {
        lock (_lock)
        {
            return _members.Where(m => m.IsOnline).ToList();
        }
    }

    public TeamMember? GetById(string id)
    {
        lock (_lock)
        {
            return _members.FirstOrDefault(m => m.Id == id);
        }
    }

    public TeamMember? Get(string id) => GetById(id);

    public void SetOnline(string id, bool online)
    {
        lock (_lock)
        {
            var member = _members.FirstOrDefault(m => m.Id == id);
            if (member != null)
            {
                member.IsOnline = online;
                member.LastSeen = online ? null : DateTime.UtcNow;
            }
        }
    }

    public int GetTaskCount(string userId)
    {
        return _taskService.GetByAssignee(userId).Count(t => t.Status != TaskStatus.Done);
    }

    public int GetActiveTaskCount(string userId)
    {
        return GetTaskCount(userId);
    }

    public bool IsOverloaded(string userId, int threshold = 10)
    {
        return GetTaskCount(userId) >= threshold;
    }

    public TeamStats GetStats()
    {
        var allTasks = _taskService.GetAll();
        var stats = new TeamStats
        {
            TotalTasks = allTasks.Count,
            CompletedTasks = allTasks.Count(t => t.Status == TaskStatus.Done),
            InProgressTasks = allTasks.Count(t => t.Status == TaskStatus.InProgress),
            OverdueTasks = _taskService.GetOverdue().Count
        };

        // Tasks by member
        foreach (var member in _members)
        {
            stats.TasksByMember[member.Name] = _taskService.GetByAssignee(member.Id).Count;
        }

        // Tasks by priority
        foreach (TaskPriority priority in Enum.GetValues(typeof(TaskPriority)))
        {
            stats.TasksByPriority[priority] = allTasks.Count(t => t.Priority == priority);
        }

        return stats;
    }

    private void SeedData()
    {
        _members.AddRange(new[]
        {
            new TeamMember
            {
                Id = "alice",
                Name = "Alice Johnson",
                Email = "alice@taskflow.dev",
                AvatarColor = "#3b82f6", // Blue
                IsOnline = true
            },
            new TeamMember
            {
                Id = "bob",
                Name = "Bob Smith",
                Email = "bob@taskflow.dev",
                AvatarColor = "#10b981", // Green
                IsOnline = true
            },
            new TeamMember
            {
                Id = "charlie",
                Name = "Charlie Brown",
                Email = "charlie@taskflow.dev",
                AvatarColor = "#f59e0b", // Orange
                IsOnline = false,
                LastSeen = DateTime.UtcNow.AddMinutes(-30)
            }
        });
    }
}

/// <summary>
/// In-memory comment service implementation
/// </summary>
public class CommentService : ICommentService
{
    private readonly List<Comment> _comments = new();
    private int _nextId = 1;
    private readonly object _lock = new();

    public List<Comment> GetByTask(int taskId)
    {
        lock (_lock)
        {
            return _comments.Where(c => c.TaskId == taskId).OrderBy(c => c.CreatedAt).ToList();
        }
    }

    public Comment? GetById(int id)
    {
        lock (_lock)
        {
            return _comments.FirstOrDefault(c => c.Id == id);
        }
    }

    public Comment? Get(int id) => GetById(id);

    public Comment Create(int taskId, CommentInput input, string authorId, string authorName)
    {
        lock (_lock)
        {
            var comment = new Comment
            {
                Id = _nextId++,
                TaskId = taskId,
                AuthorId = authorId,
                AuthorName = authorName,
                Content = input.Content,
                CreatedAt = DateTime.UtcNow
            };
            _comments.Add(comment);
            return comment;
        }
    }

    public Comment Update(int id, CommentInput input)
    {
        lock (_lock)
        {
            var comment = _comments.FirstOrDefault(c => c.Id == id);
            if (comment == null) throw new InvalidOperationException($"Comment {id} not found");

            comment.Content = input.Content;
            comment.EditedAt = DateTime.UtcNow;
            return comment;
        }
    }

    public void Delete(int id)
    {
        lock (_lock)
        {
            _comments.RemoveAll(c => c.Id == id);
        }
    }

    public int GetCountForTask(int taskId)
    {
        lock (_lock)
        {
            return _comments.Count(c => c.TaskId == taskId);
        }
    }
}

/// <summary>
/// In-memory activity service implementation
/// </summary>
public class ActivityService : IActivityService
{
    private readonly List<Activity> _activities = new();
    private int _nextId = 1;
    private readonly object _lock = new();

    public List<Activity> GetRecent(int count = 20)
    {
        lock (_lock)
        {
            return _activities.OrderByDescending(a => a.CreatedAt).Take(count).ToList();
        }
    }

    public List<Activity> GetByProject(int projectId, int count = 20)
    {
        lock (_lock)
        {
            return _activities
                .Where(a => a.ProjectId == projectId)
                .OrderByDescending(a => a.CreatedAt)
                .Take(count)
                .ToList();
        }
    }

    public List<Activity> GetByTask(int taskId)
    {
        lock (_lock)
        {
            return _activities
                .Where(a => a.TaskId == taskId)
                .OrderByDescending(a => a.CreatedAt)
                .ToList();
        }
    }

    public void Log(string type, string description, string actorId, string actorName, int? taskId = null, int? projectId = null)
    {
        lock (_lock)
        {
            var activity = new Activity
            {
                Id = _nextId++,
                Type = type,
                Description = description,
                ActorId = actorId,
                ActorName = actorName,
                TaskId = taskId,
                ProjectId = projectId,
                CreatedAt = DateTime.UtcNow
            };
            _activities.Add(activity);
        }
    }

    public void LogActivity(string description, int? taskId = null, int? projectId = null, string userId = "demo-user")
    {
        Log("activity", description, userId, userId, taskId, projectId);
    }
}

/// <summary>
/// In-memory notification service implementation
/// </summary>
public class NotificationService : INotificationService
{
    private readonly List<Notification> _notifications = new();
    private int _nextId = 1;
    private readonly object _lock = new();

    public List<Notification> GetForUser(string userId)
    {
        lock (_lock)
        {
            return _notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .ToList();
        }
    }

    public List<Notification> GetUnreadForUser(string userId)
    {
        lock (_lock)
        {
            return _notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .OrderByDescending(n => n.CreatedAt)
                .ToList();
        }
    }

    public Notification? GetById(int id)
    {
        lock (_lock)
        {
            return _notifications.FirstOrDefault(n => n.Id == id);
        }
    }

    public Notification? Get(int id) => GetById(id);

    public int GetUnreadCount(string userId)
    {
        lock (_lock)
        {
            return _notifications.Count(n => n.UserId == userId && !n.IsRead);
        }
    }

    public Notification Create(string userId, string type, string message, int? taskId = null)
    {
        lock (_lock)
        {
            var notification = new Notification
            {
                Id = _nextId++,
                UserId = userId,
                Type = type,
                Message = message,
                TaskId = taskId,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };
            _notifications.Add(notification);
            return notification;
        }
    }

    public void MarkAsRead(int id)
    {
        lock (_lock)
        {
            var notification = _notifications.FirstOrDefault(n => n.Id == id);
            if (notification != null)
            {
                notification.IsRead = true;
            }
        }
    }

    public void MarkAllAsRead(string userId)
    {
        lock (_lock)
        {
            foreach (var notification in _notifications.Where(n => n.UserId == userId))
            {
                notification.IsRead = true;
            }
        }
    }

    public void ClearAll(string userId)
    {
        lock (_lock)
        {
            _notifications.RemoveAll(n => n.UserId == userId);
        }
    }
}
