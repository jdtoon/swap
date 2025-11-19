using TaskFlow.Models;
using TaskStatus = TaskFlow.Models.TaskStatus;

namespace TaskFlow.Services;

/// <summary>
/// In-memory project service implementation
/// </summary>
public class ProjectService : IProjectService
{
    private readonly List<Project> _projects = new();
    private readonly ITaskService _taskService;
    private int _nextId = 1;
    private readonly object _lock = new();

    public ProjectService(ITaskService taskService)
    {
        _taskService = taskService;
        SeedData();
    }

    public List<Project> GetAll()
    {
        lock (_lock)
        {
            return _projects.ToList();
        }
    }

    public Project? GetById(int id)
    {
        lock (_lock)
        {
            return _projects.FirstOrDefault(p => p.Id == id);
        }
    }

    public Project? Get(int id) => GetById(id);

    public Project Create(ProjectInput input)
    {
        lock (_lock)
        {
            var project = new Project
            {
                Id = _nextId++,
                Name = input.Name,
                Description = input.Description,
                Color = input.Color,
                CreatedAt = DateTime.UtcNow
            };
            _projects.Add(project);
            return project;
        }
    }

    public Project Update(int id, ProjectInput input)
    {
        lock (_lock)
        {
            var project = _projects.FirstOrDefault(p => p.Id == id);
            if (project == null) throw new InvalidOperationException($"Project {id} not found");

            project.Name = input.Name;
            project.Description = input.Description;
            project.Color = input.Color;
            return project;
        }
    }

    public void Delete(int id)
    {
        lock (_lock)
        {
            _projects.RemoveAll(p => p.Id == id);
        }
    }

    public double GetProgress(int projectId)
    {
        var tasks = _taskService.GetByProject(projectId);
        if (tasks.Count == 0) return 0;

        var completedTasks = tasks.Count(t => t.Status == TaskStatus.Done);
        return (double)completedTasks / tasks.Count * 100;
    }

    public Dictionary<TaskStatus, int> GetTaskBreakdown(int projectId)
    {
        var tasks = _taskService.GetByProject(projectId);
        return new Dictionary<TaskStatus, int>
        {
            { TaskStatus.Todo, tasks.Count(t => t.Status == TaskStatus.Todo) },
            { TaskStatus.InProgress, tasks.Count(t => t.Status == TaskStatus.InProgress) },
            { TaskStatus.Review, tasks.Count(t => t.Status == TaskStatus.Review) },
            { TaskStatus.Done, tasks.Count(t => t.Status == TaskStatus.Done) }
        };
    }

    private void SeedData()
    {
        _projects.AddRange(new[]
        {
            new Project
            {
                Id = _nextId++,
                Name = "Website Redesign",
                Description = "Complete overhaul of the company website",
                Color = "#3b82f6", // Blue
                CreatedAt = DateTime.UtcNow.AddDays(-30)
            },
            new Project
            {
                Id = _nextId++,
                Name = "Mobile App",
                Description = "Build iOS and Android mobile applications",
                Color = "#8b5cf6", // Purple
                CreatedAt = DateTime.UtcNow.AddDays(-15)
            },
            new Project
            {
                Id = _nextId++,
                Name = "API v2",
                Description = "Design and implement the next version of our API",
                Color = "#10b981", // Green
                CreatedAt = DateTime.UtcNow.AddDays(-7)
            }
        });
    }
}
