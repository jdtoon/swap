using TaskFlow.Models;
using TaskStatus = TaskFlow.Models.TaskStatus;

namespace TaskFlow.Services;

/// <summary>
/// In-memory task service implementation
/// </summary>
public class TaskService : ITaskService
{
    private readonly List<TaskItem> _tasks = new();
    private int _nextId = 1;
    private readonly object _lock = new();

    public TaskService()
    {
        // Seed with sample data
        SeedData();
    }

    public List<TaskItem> GetAll()
    {
        lock (_lock)
        {
            return _tasks.OrderBy(t => t.Order).ToList();
        }
    }

    public List<TaskItem> GetByProject(int projectId)
    {
        lock (_lock)
        {
            return _tasks.Where(t => t.ProjectId == projectId).OrderBy(t => t.Order).ToList();
        }
    }

    public List<TaskItem> GetByStatus(TaskStatus status)
    {
        lock (_lock)
        {
            return _tasks.Where(t => t.Status == status).OrderBy(t => t.Order).ToList();
        }
    }

    public List<TaskItem> GetByAssignee(string userId)
    {
        lock (_lock)
        {
            return _tasks.Where(t => t.AssignedTo == userId).OrderBy(t => t.Order).ToList();
        }
    }

    public TaskItem? GetById(int id)
    {
        lock (_lock)
        {
            return _tasks.FirstOrDefault(t => t.Id == id);
        }
    }

    public TaskItem? Get(int id) => GetById(id);

    public TaskItem Create(TaskInput input, string creatorId)
    {
        lock (_lock)
        {
            var task = new TaskItem
            {
                Id = _nextId++,
                Title = input.Title,
                Description = input.Description,
                Priority = input.Priority,
                Status = TaskStatus.Todo,
                ProjectId = input.ProjectId,
                AssignedTo = input.AssignedTo,
                CreatedAt = DateTime.UtcNow,
                DueDate = input.DueDate,
                Order = _tasks.Count(t => t.Status == TaskStatus.Todo)
            };
            _tasks.Add(task);
            return task;
        }
    }

    public TaskItem Update(int id, TaskInput input)
    {
        lock (_lock)
        {
            var task = _tasks.FirstOrDefault(t => t.Id == id);
            if (task == null) throw new InvalidOperationException($"Task {id} not found");

            task.Title = input.Title;
            task.Description = input.Description;
            task.Priority = input.Priority;
            task.ProjectId = input.ProjectId;
            task.AssignedTo = input.AssignedTo;
            task.DueDate = input.DueDate;
            return task;
        }
    }

    public void Delete(int id)
    {
        lock (_lock)
        {
            _tasks.RemoveAll(t => t.Id == id);
        }
    }

    public TaskItem ChangeStatus(int id, TaskStatus newStatus)
    {
        lock (_lock)
        {
            var task = _tasks.FirstOrDefault(t => t.Id == id);
            if (task == null) throw new InvalidOperationException($"Task {id} not found");

            task.Status = newStatus;
            if (newStatus == TaskStatus.Done && task.CompletedAt == null)
            {
                task.CompletedAt = DateTime.UtcNow;
            }
            else if (newStatus != TaskStatus.Done)
            {
                task.CompletedAt = null;
            }
            return task;
        }
    }

    public void UpdateStatus(int id, TaskStatus newStatus)
    {
        ChangeStatus(id, newStatus);
    }

    public TaskItem ChangePriority(int id, TaskPriority newPriority)
    {
        lock (_lock)
        {
            var task = _tasks.FirstOrDefault(t => t.Id == id);
            if (task == null) throw new InvalidOperationException($"Task {id} not found");

            task.Priority = newPriority;
            return task;
        }
    }

    public void UpdatePriority(int id, TaskPriority newPriority)
    {
        ChangePriority(id, newPriority);
    }

    public TaskItem Assign(int id, string userId)
    {
        lock (_lock)
        {
            var task = _tasks.FirstOrDefault(t => t.Id == id);
            if (task == null) throw new InvalidOperationException($"Task {id} not found");

            task.AssignedTo = userId;
            return task;
        }
    }

    public TaskItem Unassign(int id)
    {
        lock (_lock)
        {
            var task = _tasks.FirstOrDefault(t => t.Id == id);
            if (task == null) throw new InvalidOperationException($"Task {id} not found");

            task.AssignedTo = null;
            return task;
        }
    }

    public List<TaskItem> GetOverdue()
    {
        lock (_lock)
        {
            var now = DateTime.UtcNow;
            return _tasks
                .Where(t => t.DueDate.HasValue && t.DueDate.Value < now && t.Status != TaskStatus.Done)
                .ToList();
        }
    }

    public int GetCountByStatus(TaskStatus status)
    {
        lock (_lock)
        {
            return _tasks.Count(t => t.Status == status);
        }
    }

    private void SeedData()
    {
        // Sample tasks for demo
        _tasks.AddRange(new[]
        {
            new TaskItem
            {
                Id = _nextId++,
                Title = "Design new landing page",
                Description = "Create mockups for the new landing page",
                Priority = TaskPriority.High,
                Status = TaskStatus.InProgress,
                ProjectId = 1,
                AssignedTo = "alice",
                CreatedAt = DateTime.UtcNow.AddDays(-5),
                DueDate = DateTime.UtcNow.AddDays(2),
                Order = 0
            },
            new TaskItem
            {
                Id = _nextId++,
                Title = "Fix authentication bug",
                Description = "Users are getting logged out unexpectedly",
                Priority = TaskPriority.Critical,
                Status = TaskStatus.Todo,
                ProjectId = 1,
                AssignedTo = "bob",
                CreatedAt = DateTime.UtcNow.AddDays(-2),
                DueDate = DateTime.UtcNow.AddDays(1),
                Order = 0
            },
            new TaskItem
            {
                Id = _nextId++,
                Title = "Update documentation",
                Description = "Document the new API endpoints",
                Priority = TaskPriority.Medium,
                Status = TaskStatus.Todo,
                ProjectId = 1,
                CreatedAt = DateTime.UtcNow.AddDays(-3),
                DueDate = DateTime.UtcNow.AddDays(7),
                Order = 1
            },
            new TaskItem
            {
                Id = _nextId++,
                Title = "Review pull request #42",
                Description = "Code review for the notification system",
                Priority = TaskPriority.High,
                Status = TaskStatus.Review,
                ProjectId = 2,
                AssignedTo = "charlie",
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                DueDate = DateTime.UtcNow.AddDays(1),
                Order = 0
            },
            new TaskItem
            {
                Id = _nextId++,
                Title = "Deploy to staging",
                Description = "Deploy latest changes to staging environment",
                Priority = TaskPriority.Medium,
                Status = TaskStatus.Done,
                ProjectId = 2,
                AssignedTo = "alice",
                CreatedAt = DateTime.UtcNow.AddDays(-4),
                CompletedAt = DateTime.UtcNow.AddDays(-1),
                Order = 0
            }
        });
    }
}
