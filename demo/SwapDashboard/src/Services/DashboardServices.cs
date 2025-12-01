using SwapDashboard.Models;
using TaskStatus = SwapDashboard.Models.TaskStatus;

namespace SwapDashboard.Services;

public interface IProjectService
{
    List<Project> GetAll();
    Project? GetById(int id);
    Project Create(string name, string description, string color);
    void Update(int id, string name, string description);
    void Delete(int id);
}

public class ProjectService : IProjectService
{
    private readonly List<Project> _projects = new()
    {
        new Project { Id = 1, Name = "Website Redesign", Description = "Complete overhaul of the company website", Color = "#6366f1", TaskCount = 12, CompletedCount = 5, CreatedAt = DateTime.Now.AddDays(-30), DueDate = DateTime.Now.AddDays(15) },
        new Project { Id = 2, Name = "Mobile App v2", Description = "New features for the mobile application", Color = "#22c55e", TaskCount = 8, CompletedCount = 2, CreatedAt = DateTime.Now.AddDays(-20), DueDate = DateTime.Now.AddDays(45) },
        new Project { Id = 3, Name = "API Integration", Description = "Third-party API integrations", Color = "#f59e0b", TaskCount = 6, CompletedCount = 6, CreatedAt = DateTime.Now.AddDays(-60), DueDate = DateTime.Now.AddDays(-5) },
        new Project { Id = 4, Name = "DevOps Pipeline", Description = "CI/CD pipeline improvements", Color = "#ef4444", TaskCount = 4, CompletedCount = 1, CreatedAt = DateTime.Now.AddDays(-10), DueDate = DateTime.Now.AddDays(30) },
        new Project { Id = 5, Name = "Documentation", Description = "Technical documentation update", Color = "#8b5cf6", TaskCount = 3, CompletedCount = 0, CreatedAt = DateTime.Now.AddDays(-5), DueDate = DateTime.Now.AddDays(60) },
    };

    private int _nextId = 6;

    public List<Project> GetAll() => _projects.ToList();
    
    public Project? GetById(int id) => _projects.FirstOrDefault(p => p.Id == id);

    public Project Create(string name, string description, string color)
    {
        var project = new Project
        {
            Id = _nextId++,
            Name = name,
            Description = description,
            Color = color,
            CreatedAt = DateTime.Now
        };
        _projects.Add(project);
        return project;
    }

    public void Update(int id, string name, string description)
    {
        var project = GetById(id);
        if (project != null)
        {
            project.Name = name;
            project.Description = description;
        }
    }

    public void Delete(int id)
    {
        var project = GetById(id);
        if (project != null) _projects.Remove(project);
    }
}

public interface ITaskService
{
    List<TaskItem> GetAll();
    List<TaskItem> GetByProject(int projectId);
    List<TaskItem> GetFiltered(int? projectId, string? status, string? priority, int? assigneeId, string? search);
    TaskItem? GetById(int id);
    TaskItem Create(int projectId, string title, string description, TaskPriority priority, int? assigneeId);
    void Update(int id, string title, string description, TaskPriority priority);
    void UpdateStatus(int id, TaskStatus status);
    void Assign(int id, int? assigneeId);
    void Delete(int id);
    DashboardStats GetStats(int? projectId = null);
    KanbanViewModel GetKanban(int? projectId);
}

public class TaskService : ITaskService
{
    private readonly ITeamService _teamService;
    private readonly List<TaskItem> _tasks;
    private int _nextId = 21;

    public TaskService(ITeamService teamService)
    {
        _teamService = teamService;
        _tasks = GenerateTasks();
    }

    private List<TaskItem> GenerateTasks()
    {
        var members = _teamService.GetAll();
        return new List<TaskItem>
        {
            // Project 1: Website Redesign
            new() { Id = 1, ProjectId = 1, Title = "Design new homepage mockup", Status = TaskStatus.Done, Priority = TaskPriority.High, AssigneeId = 1, CreatedAt = DateTime.Now.AddDays(-25), DueDate = DateTime.Now.AddDays(-10), Tags = new() { "design", "ui" }, CommentCount = 5 },
            new() { Id = 2, ProjectId = 1, Title = "Implement responsive navigation", Status = TaskStatus.Done, Priority = TaskPriority.High, AssigneeId = 2, CreatedAt = DateTime.Now.AddDays(-20), DueDate = DateTime.Now.AddDays(-5), Tags = new() { "frontend" }, CommentCount = 3 },
            new() { Id = 3, ProjectId = 1, Title = "Create product catalog page", Status = TaskStatus.InProgress, Priority = TaskPriority.Medium, AssigneeId = 2, CreatedAt = DateTime.Now.AddDays(-15), DueDate = DateTime.Now.AddDays(5), Tags = new() { "frontend", "catalog" }, CommentCount = 2 },
            new() { Id = 4, ProjectId = 1, Title = "Build contact form", Status = TaskStatus.InProgress, Priority = TaskPriority.Low, AssigneeId = 3, CreatedAt = DateTime.Now.AddDays(-10), DueDate = DateTime.Now.AddDays(10), Tags = new() { "forms" }, CommentCount = 0 },
            new() { Id = 5, ProjectId = 1, Title = "Optimize images for web", Status = TaskStatus.Todo, Priority = TaskPriority.Low, AssigneeId = 1, CreatedAt = DateTime.Now.AddDays(-5), DueDate = DateTime.Now.AddDays(12), Tags = new() { "performance" }, CommentCount = 1 },
            new() { Id = 6, ProjectId = 1, Title = "SEO optimization", Status = TaskStatus.Todo, Priority = TaskPriority.Medium, AssigneeId = null, CreatedAt = DateTime.Now.AddDays(-3), DueDate = DateTime.Now.AddDays(15), Tags = new() { "seo" }, CommentCount = 0 },
            new() { Id = 7, ProjectId = 1, Title = "Implement dark mode", Status = TaskStatus.Review, Priority = TaskPriority.Low, AssigneeId = 2, CreatedAt = DateTime.Now.AddDays(-8), DueDate = DateTime.Now.AddDays(8), Tags = new() { "ui", "feature" }, CommentCount = 4 },
            new() { Id = 8, ProjectId = 1, Title = "Add analytics tracking", Status = TaskStatus.Done, Priority = TaskPriority.Medium, AssigneeId = 4, CreatedAt = DateTime.Now.AddDays(-18), DueDate = DateTime.Now.AddDays(-8), Tags = new() { "analytics" }, CommentCount = 2 },
            new() { Id = 9, ProjectId = 1, Title = "Performance audit", Status = TaskStatus.Todo, Priority = TaskPriority.High, AssigneeId = 4, CreatedAt = DateTime.Now.AddDays(-2), DueDate = DateTime.Now.AddDays(7), Tags = new() { "performance" }, CommentCount = 0 },
            new() { Id = 10, ProjectId = 1, Title = "Browser compatibility testing", Status = TaskStatus.Done, Priority = TaskPriority.High, AssigneeId = 5, CreatedAt = DateTime.Now.AddDays(-12), DueDate = DateTime.Now.AddDays(-2), Tags = new() { "testing" }, CommentCount = 6 },
            new() { Id = 11, ProjectId = 1, Title = "Accessibility improvements", Status = TaskStatus.Review, Priority = TaskPriority.High, AssigneeId = 3, CreatedAt = DateTime.Now.AddDays(-7), DueDate = DateTime.Now.AddDays(3), Tags = new() { "a11y" }, CommentCount = 3 },
            new() { Id = 12, ProjectId = 1, Title = "Launch preparation", Status = TaskStatus.Todo, Priority = TaskPriority.Urgent, AssigneeId = null, CreatedAt = DateTime.Now.AddDays(-1), DueDate = DateTime.Now.AddDays(14), Tags = new() { "launch" }, CommentCount = 0 },

            // Project 2: Mobile App v2
            new() { Id = 13, ProjectId = 2, Title = "Push notification system", Status = TaskStatus.InProgress, Priority = TaskPriority.High, AssigneeId = 3, CreatedAt = DateTime.Now.AddDays(-15), DueDate = DateTime.Now.AddDays(10), Tags = new() { "mobile", "notifications" }, CommentCount = 4 },
            new() { Id = 14, ProjectId = 2, Title = "Offline mode support", Status = TaskStatus.Todo, Priority = TaskPriority.Medium, AssigneeId = 3, CreatedAt = DateTime.Now.AddDays(-10), DueDate = DateTime.Now.AddDays(20), Tags = new() { "mobile", "offline" }, CommentCount = 1 },
            new() { Id = 15, ProjectId = 2, Title = "Biometric authentication", Status = TaskStatus.Done, Priority = TaskPriority.High, AssigneeId = 4, CreatedAt = DateTime.Now.AddDays(-20), DueDate = DateTime.Now.AddDays(-5), Tags = new() { "security", "mobile" }, CommentCount = 7 },
            new() { Id = 16, ProjectId = 2, Title = "App store screenshots", Status = TaskStatus.Review, Priority = TaskPriority.Low, AssigneeId = 1, CreatedAt = DateTime.Now.AddDays(-5), DueDate = DateTime.Now.AddDays(15), Tags = new() { "design", "marketing" }, CommentCount = 2 },
            new() { Id = 17, ProjectId = 2, Title = "Crash reporting integration", Status = TaskStatus.Todo, Priority = TaskPriority.Medium, AssigneeId = 4, CreatedAt = DateTime.Now.AddDays(-3), DueDate = DateTime.Now.AddDays(25), Tags = new() { "monitoring" }, CommentCount = 0 },
            new() { Id = 18, ProjectId = 2, Title = "User onboarding flow", Status = TaskStatus.InProgress, Priority = TaskPriority.High, AssigneeId = 1, CreatedAt = DateTime.Now.AddDays(-8), DueDate = DateTime.Now.AddDays(5), Tags = new() { "ux", "onboarding" }, CommentCount = 5 },
            new() { Id = 19, ProjectId = 2, Title = "In-app purchases", Status = TaskStatus.Todo, Priority = TaskPriority.High, AssigneeId = null, CreatedAt = DateTime.Now.AddDays(-2), DueDate = DateTime.Now.AddDays(35), Tags = new() { "payments" }, CommentCount = 0 },
            new() { Id = 20, ProjectId = 2, Title = "Widget support", Status = TaskStatus.Done, Priority = TaskPriority.Low, AssigneeId = 3, CreatedAt = DateTime.Now.AddDays(-25), DueDate = DateTime.Now.AddDays(-15), Tags = new() { "mobile", "widget" }, CommentCount = 3 },
        };
    }

    public List<TaskItem> GetAll()
    {
        var members = _teamService.GetAll().ToDictionary(m => m.Id);
        return _tasks.Select(t =>
        {
            t.Assignee = t.AssigneeId.HasValue && members.ContainsKey(t.AssigneeId.Value) 
                ? members[t.AssigneeId.Value] 
                : null;
            return t;
        }).ToList();
    }

    public List<TaskItem> GetByProject(int projectId) => 
        GetAll().Where(t => t.ProjectId == projectId).ToList();

    public List<TaskItem> GetFiltered(int? projectId, string? status, string? priority, int? assigneeId, string? search)
    {
        var query = GetAll().AsEnumerable();
        
        if (projectId.HasValue)
            query = query.Where(t => t.ProjectId == projectId.Value);
        
        if (!string.IsNullOrEmpty(status) && status != "all")
            query = query.Where(t => t.Status.ToString().ToLower() == status.ToLower());
        
        if (!string.IsNullOrEmpty(priority) && priority != "all")
            query = query.Where(t => t.Priority.ToString().ToLower() == priority.ToLower());
        
        if (assigneeId.HasValue)
            query = query.Where(t => t.AssigneeId == assigneeId.Value);
        
        if (!string.IsNullOrEmpty(search))
            query = query.Where(t => t.Title.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                                     t.Description.Contains(search, StringComparison.OrdinalIgnoreCase));
        
        return query.ToList();
    }

    public TaskItem? GetById(int id)
    {
        var task = _tasks.FirstOrDefault(t => t.Id == id);
        if (task != null && task.AssigneeId.HasValue)
        {
            task.Assignee = _teamService.GetById(task.AssigneeId.Value);
        }
        return task;
    }

    public TaskItem Create(int projectId, string title, string description, TaskPriority priority, int? assigneeId)
    {
        var task = new TaskItem
        {
            Id = _nextId++,
            ProjectId = projectId,
            Title = title,
            Description = description,
            Priority = priority,
            AssigneeId = assigneeId,
            Status = TaskStatus.Todo,
            CreatedAt = DateTime.Now
        };
        _tasks.Add(task);
        return task;
    }

    public void Update(int id, string title, string description, TaskPriority priority)
    {
        var task = _tasks.FirstOrDefault(t => t.Id == id);
        if (task != null)
        {
            task.Title = title;
            task.Description = description;
            task.Priority = priority;
        }
    }

    public void UpdateStatus(int id, TaskStatus status)
    {
        var task = _tasks.FirstOrDefault(t => t.Id == id);
        if (task != null)
        {
            task.Status = status;
        }
    }

    public void Assign(int id, int? assigneeId)
    {
        var task = _tasks.FirstOrDefault(t => t.Id == id);
        if (task != null)
        {
            task.AssigneeId = assigneeId;
        }
    }

    public void Delete(int id)
    {
        var task = _tasks.FirstOrDefault(t => t.Id == id);
        if (task != null) _tasks.Remove(task);
    }

    public DashboardStats GetStats(int? projectId = null)
    {
        var tasks = projectId.HasValue ? GetByProject(projectId.Value) : GetAll();
        return new DashboardStats
        {
            TotalTasks = tasks.Count,
            CompletedTasks = tasks.Count(t => t.Status == TaskStatus.Done),
            InProgressTasks = tasks.Count(t => t.Status == TaskStatus.InProgress),
            OverdueTasks = tasks.Count(t => t.DueDate.HasValue && t.DueDate < DateTime.Now && t.Status != TaskStatus.Done)
        };
    }

    public KanbanViewModel GetKanban(int? projectId)
    {
        var tasks = projectId.HasValue ? GetByProject(projectId.Value) : GetAll();
        return new KanbanViewModel
        {
            TodoTasks = tasks.Where(t => t.Status == TaskStatus.Todo).ToList(),
            InProgressTasks = tasks.Where(t => t.Status == TaskStatus.InProgress).ToList(),
            ReviewTasks = tasks.Where(t => t.Status == TaskStatus.Review).ToList(),
            DoneTasks = tasks.Where(t => t.Status == TaskStatus.Done).ToList()
        };
    }
}

public interface ITeamService
{
    List<TeamMember> GetAll();
    TeamMember? GetById(int id);
}

public class TeamService : ITeamService
{
    private readonly List<TeamMember> _members = new()
    {
        new TeamMember { Id = 1, Name = "Sarah Chen", Email = "sarah@example.com", Avatar = "SC", Role = "Design Lead", ActiveTaskCount = 4, IsOnline = true },
        new TeamMember { Id = 2, Name = "Mike Johnson", Email = "mike@example.com", Avatar = "MJ", Role = "Frontend Developer", ActiveTaskCount = 3, IsOnline = true },
        new TeamMember { Id = 3, Name = "Emily Davis", Email = "emily@example.com", Avatar = "ED", Role = "Mobile Developer", ActiveTaskCount = 5, IsOnline = false },
        new TeamMember { Id = 4, Name = "Alex Kim", Email = "alex@example.com", Avatar = "AK", Role = "Full Stack Developer", ActiveTaskCount = 3, IsOnline = true },
        new TeamMember { Id = 5, Name = "Jordan Lee", Email = "jordan@example.com", Avatar = "JL", Role = "QA Engineer", ActiveTaskCount = 2, IsOnline = true },
    };

    public List<TeamMember> GetAll() => _members.ToList();
    public TeamMember? GetById(int id) => _members.FirstOrDefault(m => m.Id == id);
}

public interface IActivityService
{
    List<ActivityItem> GetRecent(int count = 10);
    void Add(string type, string description, int? userId, string userName, int? taskId, string? taskTitle, int? projectId);
}

public class ActivityService : IActivityService
{
    private readonly List<ActivityItem> _activities = new()
    {
        new ActivityItem { Id = 1, Type = "task_completed", Description = "completed task", UserId = 2, UserName = "Mike Johnson", UserAvatar = "MJ", TaskId = 2, TaskTitle = "Implement responsive navigation", ProjectId = 1, Timestamp = DateTime.Now.AddMinutes(-15) },
        new ActivityItem { Id = 2, Type = "comment", Description = "commented on", UserId = 1, UserName = "Sarah Chen", UserAvatar = "SC", TaskId = 7, TaskTitle = "Implement dark mode", ProjectId = 1, Timestamp = DateTime.Now.AddMinutes(-32) },
        new ActivityItem { Id = 3, Type = "task_assigned", Description = "was assigned to", UserId = 3, UserName = "Emily Davis", UserAvatar = "ED", TaskId = 13, TaskTitle = "Push notification system", ProjectId = 2, Timestamp = DateTime.Now.AddHours(-1) },
        new ActivityItem { Id = 4, Type = "task_created", Description = "created task", UserId = 4, UserName = "Alex Kim", UserAvatar = "AK", TaskId = 12, TaskTitle = "Launch preparation", ProjectId = 1, Timestamp = DateTime.Now.AddHours(-2) },
        new ActivityItem { Id = 5, Type = "task_status", Description = "moved to In Progress", UserId = 1, UserName = "Sarah Chen", UserAvatar = "SC", TaskId = 18, TaskTitle = "User onboarding flow", ProjectId = 2, Timestamp = DateTime.Now.AddHours(-3) },
        new ActivityItem { Id = 6, Type = "task_completed", Description = "completed task", UserId = 5, UserName = "Jordan Lee", UserAvatar = "JL", TaskId = 10, TaskTitle = "Browser compatibility testing", ProjectId = 1, Timestamp = DateTime.Now.AddHours(-5) },
        new ActivityItem { Id = 7, Type = "comment", Description = "commented on", UserId = 2, UserName = "Mike Johnson", UserAvatar = "MJ", TaskId = 11, TaskTitle = "Accessibility improvements", ProjectId = 1, Timestamp = DateTime.Now.AddHours(-6) },
        new ActivityItem { Id = 8, Type = "task_created", Description = "created task", UserId = 1, UserName = "Sarah Chen", UserAvatar = "SC", TaskId = 9, TaskTitle = "Performance audit", ProjectId = 1, Timestamp = DateTime.Now.AddHours(-8) },
    };

    private int _nextId = 9;

    public List<ActivityItem> GetRecent(int count = 10) => 
        _activities.OrderByDescending(a => a.Timestamp).Take(count).ToList();

    public void Add(string type, string description, int? userId, string userName, int? taskId, string? taskTitle, int? projectId)
    {
        _activities.Insert(0, new ActivityItem
        {
            Id = _nextId++,
            Type = type,
            Description = description,
            UserId = userId,
            UserName = userName,
            UserAvatar = userName.Length >= 2 ? $"{userName[0]}{userName.Split(' ').LastOrDefault()?[0]}" : userName[..2],
            TaskId = taskId,
            TaskTitle = taskTitle,
            ProjectId = projectId,
            Timestamp = DateTime.Now
        });
    }
}

public interface INotificationService
{
    List<Notification> GetAll();
    int GetUnreadCount();
    void MarkAsRead(int id);
    void MarkAllAsRead();
    void Add(string title, string message, string type);
}

public class NotificationService : INotificationService
{
    private readonly List<Notification> _notifications = new()
    {
        new Notification { Id = 1, Title = "Task assigned", Message = "You were assigned to 'Push notification system'", Type = "info", IsRead = false, Timestamp = DateTime.Now.AddMinutes(-30) },
        new Notification { Id = 2, Title = "Deadline approaching", Message = "'Create product catalog page' is due in 5 days", Type = "warning", IsRead = false, Timestamp = DateTime.Now.AddHours(-2) },
        new Notification { Id = 3, Title = "Task completed", Message = "'Biometric authentication' was marked complete", Type = "success", IsRead = true, Timestamp = DateTime.Now.AddHours(-5) },
        new Notification { Id = 4, Title = "New comment", Message = "Sarah commented on 'Implement dark mode'", Type = "info", IsRead = false, Timestamp = DateTime.Now.AddHours(-6) },
        new Notification { Id = 5, Title = "Project milestone", Message = "'API Integration' project is 100% complete!", Type = "success", IsRead = true, Timestamp = DateTime.Now.AddDays(-1) },
    };

    private int _nextId = 6;

    public List<Notification> GetAll() => _notifications.OrderByDescending(n => n.Timestamp).ToList();
    
    public int GetUnreadCount() => _notifications.Count(n => !n.IsRead);

    public void MarkAsRead(int id)
    {
        var notification = _notifications.FirstOrDefault(n => n.Id == id);
        if (notification != null) notification.IsRead = true;
    }

    public void MarkAllAsRead()
    {
        foreach (var notification in _notifications)
            notification.IsRead = true;
    }

    public void Add(string title, string message, string type)
    {
        _notifications.Insert(0, new Notification
        {
            Id = _nextId++,
            Title = title,
            Message = message,
            Type = type,
            IsRead = false,
            Timestamp = DateTime.Now
        });
    }
}
