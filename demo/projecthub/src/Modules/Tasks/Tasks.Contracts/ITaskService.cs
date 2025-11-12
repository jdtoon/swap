namespace ProjectHub.Modules.Tasks.Contracts;

public interface ITaskService
{
    Task<IEnumerable<TaskDto>> GetAllAsync();
    Task<IEnumerable<TaskDto>> GetByProjectIdAsync(int projectId);
    Task<IEnumerable<TaskDto>> GetByStatusAsync(TaskStatus status);
    Task<IEnumerable<TaskDto>> GetByAssignedUserAsync(int userId);
    Task<TaskDto?> GetByIdAsync(int id);
    Task<TaskDto> CreateAsync(CreateTaskDto dto);
    Task<TaskDto> UpdateAsync(int id, UpdateTaskDto dto);
    Task<TaskDto> MoveAsync(int id, MoveTaskDto dto);
    Task DeleteAsync(int id);
}
