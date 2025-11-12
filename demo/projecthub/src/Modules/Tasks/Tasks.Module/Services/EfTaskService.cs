using Microsoft.EntityFrameworkCore;
using ProjectHub.Modules.Projects.Contracts;
using ProjectHub.Modules.Tasks.Contracts;
using ProjectHub.Modules.Tasks.Module.Persistence;
using TaskStatus = ProjectHub.Modules.Tasks.Contracts.TaskStatus;

namespace ProjectHub.Modules.Tasks.Module.Services;

internal class EfTaskService(TasksDbContext db, IProjectService projectService) : ITaskService
{
    public async Task<IEnumerable<TaskDto>> GetAllAsync()
    {
        var tasks = await db.Tasks
            .Where(t => !t.IsArchived)
            .OrderBy(t => t.ProjectId)
            .ThenBy(t => t.Status)
            .ThenBy(t => t.Position)
            .ToListAsync();

        return await MapToDtosAsync(tasks);
    }

    public async Task<IEnumerable<TaskDto>> GetByProjectIdAsync(int projectId)
    {
        var tasks = await db.Tasks
            .Where(t => t.ProjectId == projectId && !t.IsArchived)
            .OrderBy(t => t.Status)
            .ThenBy(t => t.Position)
            .ToListAsync();

        return await MapToDtosAsync(tasks);
    }

    public async Task<IEnumerable<TaskDto>> GetByStatusAsync(TaskStatus status)
    {
        var tasks = await db.Tasks
            .Where(t => t.Status == status && !t.IsArchived)
            .OrderBy(t => t.Position)
            .ToListAsync();

        return await MapToDtosAsync(tasks);
    }

    public async Task<IEnumerable<TaskDto>> GetByAssignedUserAsync(int userId)
    {
        var tasks = await db.Tasks
            .Where(t => t.AssignedToUserId == userId && !t.IsArchived)
            .OrderBy(t => t.Status)
            .ThenBy(t => t.Position)
            .ToListAsync();

        return await MapToDtosAsync(tasks);
    }

    public async Task<TaskDto?> GetByIdAsync(int id)
    {
        var task = await db.Tasks.FindAsync(id);
        if (task is null) return null;

        return await MapToDtoAsync(task);
    }

    public async Task<TaskDto> CreateAsync(CreateTaskDto dto)
    {
        // Get the max position for the status
        var maxPosition = await db.Tasks
            .Where(t => t.ProjectId == dto.ProjectId && t.Status == dto.Status)
            .MaxAsync(t => (int?)t.Position) ?? -1;

        var task = new Models.Task
        {
            ProjectId = dto.ProjectId,
            Title = dto.Title,
            Description = dto.Description,
            Status = dto.Status,
            Priority = dto.Priority,
            AssignedToUserId = dto.AssignedToUserId,
            DueDate = dto.DueDate,
            Position = maxPosition + 1,
            CreatedAt = DateTimeOffset.UtcNow
        };

        db.Tasks.Add(task);
        await db.SaveChangesAsync();

        return await MapToDtoAsync(task);
    }

    public async Task<TaskDto> UpdateAsync(int id, UpdateTaskDto dto)
    {
        var task = await db.Tasks.FindAsync(id);
        if (task is null) throw new InvalidOperationException($"Task {id} not found");

        var oldStatus = task.Status;

        if (dto.Title is not null) task.Title = dto.Title;
        if (dto.Description is not null) task.Description = dto.Description;
        if (dto.Priority is not null) task.Priority = dto.Priority.Value;
        if (dto.AssignedToUserId is not null) task.AssignedToUserId = dto.AssignedToUserId;
        if (dto.DueDate is not null) task.DueDate = dto.DueDate;
        if (dto.IsArchived is not null) task.IsArchived = dto.IsArchived.Value;
        
        if (dto.Status is not null && dto.Status.Value != oldStatus)
        {
            // When status changes, move to end of new column
            var maxPosition = await db.Tasks
                .Where(t => t.ProjectId == task.ProjectId && t.Status == dto.Status.Value)
                .MaxAsync(t => (int?)t.Position) ?? -1;
            
            task.Status = dto.Status.Value;
            task.Position = maxPosition + 1;
        }

        task.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync();

        return await MapToDtoAsync(task);
    }

    public async Task<TaskDto> MoveAsync(int id, MoveTaskDto dto)
    {
        var task = await db.Tasks.FindAsync(id);
        if (task is null) throw new InvalidOperationException($"Task {id} not found");

        var oldStatus = task.Status;
        var oldPosition = task.Position;

        // If moving to a different column
        if (dto.NewStatus != oldStatus)
        {
            task.Status = dto.NewStatus;
            task.Position = dto.NewPosition;
        }
        else
        {
            // Same column, just reorder
            task.Position = dto.NewPosition;
        }

        task.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync();

        return await MapToDtoAsync(task);
    }

    public async System.Threading.Tasks.Task DeleteAsync(int id)
    {
        var task = await db.Tasks.FindAsync(id);
        if (task is null) return;

        db.Tasks.Remove(task);
        await db.SaveChangesAsync();
    }

    private async Task<TaskDto> MapToDtoAsync(Models.Task task)
    {
        string projectName;
        try
        {
            var project = await projectService.GetByIdAsync(task.ProjectId);
            projectName = project?.Name ?? "Unknown Project";
        }
        catch
        {
            projectName = "Unknown Project";
        }

        return new TaskDto(
            task.Id,
            task.ProjectId,
            projectName,
            task.Title,
            task.Description,
            task.Status,
            task.Priority,
            task.AssignedToUserId,
            null, // TODO: Get user name from Users module when implemented
            task.DueDate,
            task.Position,
            task.IsArchived,
            task.CreatedAt,
            task.UpdatedAt
        );
    }

    private async Task<IEnumerable<TaskDto>> MapToDtosAsync(IEnumerable<Models.Task> tasks)
    {
        var result = new List<TaskDto>();
        foreach (var task in tasks)
        {
            result.Add(await MapToDtoAsync(task));
        }
        return result;
    }
}
