using habits.Data.Models;

namespace habits.Dtos
{
    public class TaskListDto
    {
        public int Id { get; set; }

        public string Name { get; set; } = null!;

        public string? Description { get; set; }

        public int Order { get; set; }

        public int NoOfTasks { get; set; }

        public List<ItemDto> TaskListItems { get; set; } = [];

        public static TaskListDto FromModel(TaskList taskList)
        {
            return new TaskListDto
            {
                Id = taskList.Id,
                Name = taskList.Name,
                Description = taskList.Description,
                Order = taskList.Order,
                NoOfTasks = taskList.TaskListItems.Where(x => !x.IsHeader).Count() > 3 ? 
                    taskList.TaskListItems.Where(x => !x.IsHeader).Count() - 3 : 
                    taskList.TaskListItems.Where(x => !x.IsHeader).Count(),
                TaskListItems = taskList.TaskListItems.Count > 0 ? taskList.TaskListItems.OrderBy(x => x.Order)
                                                                                         .Select(ItemDto.FromModel)
                                                                                         .ToList() : []
            };
        }

        public static TaskList ToCreateModel(TaskListDto taskList)
        {
            return new TaskList
            {
                Name = taskList.Name,
                Description = taskList.Description,
                CreatedDateUTC = DateTime.UtcNow,
                Order = 1,
                CreatedBy = new AppUser(),
                UpdatedBy = new AppUser(),
            };
        }
    }

    public class ItemDto
    {
        public int Id { get; set; }
        public string Task { get; set; } = null!;
        public bool IsHeader { get; set; } = false;
        public bool IsCompleted { get; set; } = false;
        public int Order { get; set; }

        public List<MemberDto> AssignedUsers { get; set; } = [];

        public TaskListDto TaskList { get; set; } = new TaskListDto();

        public int TaskListId { get; set; }

        public static ItemDto FromModel(TaskListItem taskList)
        {
            return new ItemDto
            {
                Id = taskList.Id,
                Task = taskList.Task,
                IsHeader = taskList.IsHeader,
                IsCompleted = taskList.IsCompleted,
                Order = taskList.Order,
                AssignedUsers = taskList.AssignedUsers.Count > 0 ?
                    taskList.AssignedUsers.Select(tu => MemberDto.FromModel(tu.User)).ToList() : 
                    [],
                TaskListId = taskList.TaskList.Id
            };
        }

        public static ItemDto FromModelWithTaskListId(TaskListItem taskList, int taskListId)
        {
            return new ItemDto
            {
                Id = taskList.Id,
                Task = taskList.Task,
                IsCompleted = taskList.IsCompleted,
                IsHeader = taskList.IsHeader,
                Order = taskList.Order,
                TaskListId = taskListId,
                AssignedUsers = taskList.AssignedUsers.Count > 0 ?
                    taskList.AssignedUsers.Select(tu => MemberDto.FromModel(tu.User)).ToList() :
                    [],
                TaskList = TaskListDto.FromModel(taskList.TaskList)
            };
        }

        public static TaskListItem ToCreateModel(ItemDto taskList)
        {
            return new TaskListItem
            {
                Task = taskList.Task,
                CreatedDateUTC = DateTime.UtcNow,
                Order = 1,
                CreatedBy = new AppUser(),
                UpdatedBy = new AppUser(),
                IsHeader = taskList.IsHeader,
            };
        }
    }

    public class ItemListTaskDto
    {
        public string Name { get; set; } = null!;
        public List<ItemDto> Items { get; set; } = [];

    }
}
