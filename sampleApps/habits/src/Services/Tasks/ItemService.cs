using Microsoft.EntityFrameworkCore;
using habits.Data;
using habits.Data.Models;
using habits.Dtos;

namespace habits.Services.Tasks
{
    public class ItemService : IItemService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ItemService(ApplicationDbContext context,
                           IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public List<ItemDto> GetItems(int taskListId)
        {
            return _context.TaskListItem.AsNoTracking()
                                        .Include(x => x.TaskList)
                                        .Include(x => x.AssignedUsers)
                                            .ThenInclude(x => x.User)
                                        .Where(x => x.TaskList.Id == taskListId)
                                        .Select(x => ItemDto.FromModelWithTaskListId(x, taskListId))
                                        .ToList();
        }


        public ItemDto GetItem(int id)
        {
            var taskList = _context.TaskListItem.AsNoTracking()
                                                .Include(x => x.AssignedUsers)
                                                    .ThenInclude(x => x.User)
                                                .Include(x => x.TaskList)
                                                .FirstOrDefault(x => x.Id == id);

            if (taskList == null)
                return new ItemDto();

            return ItemDto.FromModel(taskList);
        }

        public ItemDto CreateItem(ItemDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Task))
                throw new Exception();

            var model = ItemDto.ToCreateModel(dto);
            model.CreatedBy = _context.Users.FirstOrDefault(x => x.Email == _httpContextAccessor.HttpContext!.User.Identity!.Name)!;
            model.UpdatedBy = _context.Users.FirstOrDefault(x => x.Email == _httpContextAccessor.HttpContext!.User.Identity!.Name)!;

            using var transaction = _context.Database.BeginTransaction();
            try
            {
                IncrementOrders(dto.TaskListId);

                var taskList = _context.TaskList.FirstOrDefault(x => x.Id == dto.TaskListId);
                if(taskList != null)
                {
                    taskList.TaskListItems.Add(model);
                    _context.SaveChanges();
                    transaction.Commit();
                }
            }
            catch
            {
                transaction.Rollback();
                throw;
            }

            return ItemDto.FromModel(model);
        }

        public bool DeleteItem(int id)
        {
            var taskListItem = _context.TaskListItem.Include(x => x.TaskList).FirstOrDefault(x => x.Id == id);

            if (taskListItem == null)
                return false;

            var taskListId = taskListItem.TaskList.Id;

            try
            {
                _context.TaskListItem.Remove(taskListItem);
                _context.SaveChanges();
            }
            catch
            {
                return false;
            }

            ReorderItemsAfterDeletion(taskListId);

            return true;
        }
        public bool DeleteItems(List<int> ids)
        {
            var itemsToDelete = _context.TaskListItem.Include(x => x.TaskList).Where(x => ids.Contains(x.Id)).ToList();
            if (!itemsToDelete.Any())
            {
                return false;
            }

            var taskListId = itemsToDelete.First().TaskList.Id;

            try
            {
                _context.TaskListItem.RemoveRange(itemsToDelete);
                _context.SaveChanges();
            }
            catch
            {
                return false;
            }

            ReorderItemsAfterDeletion(taskListId);

            return true;
        }

        public ItemDto SetAsHeader(int id, bool isHeader)
        {
            var taskListItem = _context.TaskListItem.Include(x => x.TaskList)
                                                    .Include(x => x.AssignedUsers)
                                                        .ThenInclude(x => x.User)
                                                    .FirstOrDefault(x => x.Id == id);

            if (taskListItem == null)
                return new ItemDto();

            try
            {
                taskListItem.IsHeader = isHeader;
                if (isHeader)
                    taskListItem.IsCompleted = false;

                taskListItem.UpdatedDateUTC = DateTime.UtcNow;
                taskListItem.UpdatedBy = _context.Users.FirstOrDefault(x => x.Email == _httpContextAccessor.HttpContext!.User.Identity!.Name)!;

                _context.SaveChanges();
            }
            catch
            {
                return new ItemDto();
            }

            return ItemDto.FromModel(taskListItem);
        }

        public ItemDto SetCompleted(int id, bool isComplete)
        {
            var taskListItem = _context.TaskListItem.Include(x => x.TaskList)
                                                    .Include(x => x.AssignedUsers)
                                                        .ThenInclude(x => x.User)
                                                    .FirstOrDefault(x => x.Id == id);

            if (taskListItem == null)
                return new ItemDto();

            try
            {
                taskListItem.IsCompleted = isComplete;

                taskListItem.UpdatedDateUTC = DateTime.UtcNow;
                taskListItem.UpdatedBy = _context.Users.FirstOrDefault(x => x.Email == _httpContextAccessor.HttpContext!.User.Identity!.Name)!;

                _context.SaveChanges();
            }
            catch
            {
                return new ItemDto();
            }

            return ItemDto.FromModel(taskListItem);
        }

        public ItemDto UpdateItem(ItemDto dto)
        {
            var taskListItem = _context.TaskListItem.Include(x => x.TaskList)
                                                    .Include(x => x.AssignedUsers)
                                                        .ThenInclude(x => x.User)
                                                    .FirstOrDefault(x => x.Id == dto.Id);

            if (taskListItem == null)
                return new ItemDto();

            try
            {
                taskListItem.Task = dto.Task;
                taskListItem.IsHeader = dto.IsHeader;

                taskListItem.UpdatedDateUTC = DateTime.UtcNow;
                taskListItem.UpdatedBy = _context.Users.FirstOrDefault(x => x.Email == _httpContextAccessor.HttpContext!.User.Identity!.Name)!;

                _context.SaveChanges();
            }
            catch
            {
                return new ItemDto();
            }

            return ItemDto.FromModel(taskListItem);
        }

        public async Task<bool> UpdateItemsOrderAsync(int id, int taskListId, int newPosition)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var taskListItem = await _context.TaskListItem.FindAsync(id);
                if (taskListItem == null)
                    return false;

                var oldPosition = taskListItem.Order;

                // Get the total number of items in the task list
                var totalItems = await _context.TaskListItem
                    .Where(t => t.TaskList.Id == taskListId)
                    .CountAsync();

                // Adjust newPosition if it's greater than the total items (e.g., moving to the bottom)
                if (newPosition > totalItems)
                {
                    newPosition = totalItems; // Set to the last position
                }

                // Update the order of items if moving up or down
                if (newPosition > oldPosition)
                {
                    await _context.Database.ExecuteSqlRawAsync(
                        "UPDATE TaskListItem SET [Order] = [Order] - 1 WHERE [Order] > @p0 AND " +
                        "                                                    [Order] <= @p1 AND " +
                        "                                                    [TaskListId] = @p2",
                        oldPosition,
                        newPosition,
                        taskListId);
                }
                else if (newPosition < oldPosition)
                {
                    await _context.Database.ExecuteSqlRawAsync(
                        "UPDATE TaskListItem SET [Order] = [Order] + 1 WHERE [Order] >= @p0 AND " +
                        "                                                    [Order] < @p1 AND " +
                        "                                                    [TaskListId] = @p2",
                        newPosition,
                        oldPosition,
                        taskListId);
                }

                // Update the task list item with the new position
                taskListItem.Order = newPosition;
                taskListItem.UpdatedDateUTC = DateTime.UtcNow;
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

        public List<UserDisplayDto> GetAllUsers()
        {
            return _context.Users.AsNoTracking()
                                 .Select(user => UserDisplayDto.FromModel(user))
                                 .ToList();
        }

        public ItemDto AssignUsersToItem(int itemId, List<string> userIds)
        {
            var item = _context.TaskListItem.Include(x => x.TaskList)
                                            .Include(x => x.AssignedUsers)
                                                .ThenInclude(x => x.User)
                                            .FirstOrDefault(x => x.Id == itemId);

            if (item == null)
                throw new Exception("Item not found");

            item.AssignedUsers.Clear();
            foreach (var userId in userIds)
            {
                var user = _context.Users.Find(userId);
                if (user != null)
                {
                    item.AssignedUsers.Add(new TaskUser { Task = item, User = user });
                }
            }

            _context.SaveChanges();
            return ItemDto.FromModel(item);
        }


        private void IncrementOrders(int taskListId)
        {
            _context.Database.ExecuteSqlRaw("UPDATE TaskListItem SET [Order] = [Order] + 1 WHERE [TaskListId] = @p0", taskListId);
        }

        private void ReorderItemsAfterDeletion(int taskListId)
        {
            _context.Database.ExecuteSqlRaw(@"
                WITH ReorderedItems AS (
                    SELECT 
                        Id,
                        ROW_NUMBER() OVER (ORDER BY [Order]) AS NewOrder
                    FROM TaskListItem
                    WHERE TaskListId = @p0
                )
                UPDATE TaskListItem
                SET [Order] = (
                    SELECT NewOrder 
                    FROM ReorderedItems 
                    WHERE ReorderedItems.Id = TaskListItem.Id
                )
                WHERE TaskListId = @p0;
            ", taskListId);
        }
    }
}
