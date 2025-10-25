using habits.Dtos;

namespace habits.Services.Tasks
{
    public interface ITaskService
    {
        List<TaskListDto> GetTaskLists();
        TaskListDto GetTaskListById(int id);
        TaskListDto CreateTaskList(TaskListDto dto);
        TaskListDto UpdateTaskList(int id, string name, string description);
        bool DeleteTaskList(int id);
        Task<bool> UpdateTaskListOrderAsync(int id, int newPosition);
    }
}
