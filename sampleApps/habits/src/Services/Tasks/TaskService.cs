using Microsoft.EntityFrameworkCore;
using habits.Data;
using habits.Data.Models;
using habits.Dtos;
using System.Diagnostics;

namespace habits.Services.Tasks
{
    public class TaskService : ITaskService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public TaskService(ApplicationDbContext context, 
                           IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public List<TaskListDto> GetTaskLists()
        {
            return _context.TaskList.AsNoTracking()
                                    .Include(x => x.TaskListItems)
                                    .Select(TaskListDto.FromModel)
                                    .ToList();
        }

        public TaskListDto GetTaskListById(int id)
        {
            var taskList = _context.TaskList.AsNoTracking()
                                            .FirstOrDefault(x => x.Id == id);

            if(taskList == null)
                return new TaskListDto();

            return TaskListDto.FromModel(taskList);
        }

        public TaskListDto CreateTaskList(TaskListDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                throw new Exception();

            var model = TaskListDto.ToCreateModel(dto);
            model.CreatedBy = _context.Users.FirstOrDefault(x => x.Email == _httpContextAccessor.HttpContext!.User.Identity!.Name)!;
            model.UpdatedBy = _context.Users.FirstOrDefault(x => x.Email == _httpContextAccessor.HttpContext!.User.Identity!.Name)!;

            using var transaction = _context.Database.BeginTransaction();
            try
            {
                IncrementOrders();

                _context.TaskList.Add(model);
                _context.SaveChanges();
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }

            return TaskListDto.FromModel(model);
        }

        public TaskListDto UpdateTaskList(int id, string name, string description)
        {
            var taskList = _context.TaskList.FirstOrDefault(x => x.Id == id);

            if (taskList == null)
                return new TaskListDto();

            try
            {
                taskList.Name = name;
                taskList.Description = description;

                taskList.UpdatedDateUTC = DateTime.UtcNow;
                taskList.UpdatedBy = _context.Users.FirstOrDefault(x => x.Email == _httpContextAccessor.HttpContext!.User.Identity!.Name)!;

                _context.SaveChanges();
            }
            catch
            {
                return new TaskListDto();
            }

            return TaskListDto.FromModel(taskList);
        }

        public bool DeleteTaskList(int id)
        {
            var taskList = _context.TaskList.FirstOrDefault(x => x.Id == id);

            if (taskList == null)
                return false;

            try
            {
                _context.TaskList.Remove(taskList);
                _context.SaveChanges();
            }
            catch
            {
                return false;
            }

            ReorderItemsAfterDeletion();

            return true;
        }

        public async Task<bool> UpdateTaskListOrderAsync(int id, int newPosition)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var taskList = await _context.TaskList.FindAsync(id);
                if (taskList == null)
                    return false;

                var oldPosition = taskList.Order;

                // Get the total number of task lists
                var totalTaskLists = await _context.TaskList.CountAsync();

                // Adjust newPosition if it's greater than the total task lists
                if (newPosition > totalTaskLists)
                {
                    newPosition = totalTaskLists; // Set to the last position
                }

                // Update the order of task lists if moving up or down
                if (newPosition > oldPosition)
                {
                    await _context.Database.ExecuteSqlRawAsync(
                        "UPDATE TaskList SET [Order] = [Order] - 1 WHERE [Order] > @p0 AND [Order] <= @p1",
                        oldPosition, newPosition);
                }
                else if (newPosition < oldPosition)
                {
                    await _context.Database.ExecuteSqlRawAsync(
                        "UPDATE TaskList SET [Order] = [Order] + 1 WHERE [Order] >= @p0 AND [Order] < @p1",
                        newPosition, oldPosition);
                }

                // Update the task list with the new position
                taskList.Order = newPosition;
                taskList.UpdatedDateUTC = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                return false;
            }
        }


        private void IncrementOrders()
        {
            _context.Database.ExecuteSqlRaw("UPDATE TaskList SET [Order] = [Order] + 1");
        }

        public void ReorderItemsAfterDeletion()
        {
            _context.Database.ExecuteSqlRaw(@"
                WITH ReorderedItems AS (
                    SELECT 
                        Id,
                        ROW_NUMBER() OVER (ORDER BY [Order]) AS NewOrder
                    FROM TaskList
                )
                UPDATE TaskList
                SET [Order] = (
                    SELECT NewOrder 
                    FROM ReorderedItems 
                    WHERE ReorderedItems.Id = TaskList.Id
                )");
        }
    }
}
