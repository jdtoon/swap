using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using habits.Dtos;
using habits.Services.Tasks;

namespace habits.Controllers
{
    [Authorize]
    public class ItemController : Controller
    {
        private readonly IItemService _itemService;
        private readonly ITaskService _taskService;

        public ItemController(IItemService itemService, ITaskService taskService)
        {
            _itemService = itemService;
            _taskService = taskService;
        }

        public IActionResult Index(int taskListId)
        {
            ViewData["taskListId"] = taskListId;
            ViewBag.IsHTMXRequest = HttpContext.Request.Headers.ContainsKey("HX-Request");

            if (HttpContext.Request.Headers.ContainsKey("HX-Request"))
                return PartialView();

            return View();
        }

        public IActionResult GetItems(int taskListId)
        {
            var data = _itemService.GetItems(taskListId);
            var taskList = _taskService.GetTaskListById(taskListId);

            ViewData["taskListId"] = taskListId;

            var model = new ItemListTaskDto
            {
                Items = data,
                Name = taskList.Name
            };

            return PartialView("_Items", model);
        }

        public IActionResult AddItem(int taskListId)
        {
            return PartialView("_AddItemModal", new ItemDto
            {
                TaskListId = taskListId
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateItem(ItemDto dto)
        {
            var newItem = _itemService.CreateItem(dto);
            return PartialView("_ItemPartial", newItem);
        }

        [HttpGet]
        public IActionResult EditItem(int id)
        {
            var editItem = _itemService.GetItem(id);
            return PartialView("_EditItemModal", editItem);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateItem(ItemDto dto)
        {
            var updatedItem = _itemService.UpdateItem(dto);
            return PartialView("_ItemPartial", updatedItem);
        }

        [HttpPost]
        public IActionResult ToggleHeader(int id, bool isHeader)
        {
            var updatedItem = _itemService.SetAsHeader(id, isHeader);
            return PartialView("_ItemPartial", updatedItem);
        }

        [HttpPost]
        public IActionResult ToggleCompleted(int id, bool isCompleted)
        {
            var updatedItem = _itemService.SetCompleted(id, isCompleted);
            return PartialView("_ItemPartial", updatedItem);
        }

        [HttpDelete]
        public IActionResult DeleteItem(int id)
        {
            var success = _itemService.DeleteItem(id);
            if (success)
            {
                return Ok();
            }
            return BadRequest();
        }
        [HttpDelete]
        public IActionResult DeleteItems([FromQuery] List<int> ids)
        {
            var success = _itemService.DeleteItems(ids);
            if (success)
            {
                return Ok();
            }
            return BadRequest();
        }

        [HttpPost]
        public async Task<IActionResult> UpdateItemOrders([FromForm] string id, [FromForm] string taskListId, [FromForm] string newPosition)
        {
            if (string.IsNullOrWhiteSpace(id) ||
                string.IsNullOrWhiteSpace(newPosition) ||
                string.IsNullOrWhiteSpace(taskListId))
            {
                return BadRequest("Request data is null");
            }

            try
            {
                var result = await _itemService.UpdateItemsOrderAsync(Convert.ToInt32(id),
                                                                      Convert.ToInt32(taskListId),
                                                                      Convert.ToInt32(newPosition));
                if (result)
                {
                    return Ok();
                }
                return BadRequest("Failed to update order");
            }
            catch (Exception ex)
            {
                return BadRequest($"Error updating order: {ex.Message}");
            }
        }

        [HttpGet]
        public IActionResult AssignUsers(int id)
        {
            var item = _itemService.GetItem(id);
            var users = _itemService.GetAllUsers();
            var model = new AssignUsersDto
            {
                ItemId = id,
                Users = users,
                AssignedUserIds = item.AssignedUsers.Select(u => u.Id).ToList()
            };
            return PartialView("_AssignUsersModal", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AssignUsers(AssignUsersDto dto)
        {
            var updatedItem = _itemService.AssignUsersToItem(dto.ItemId, dto.AssignedUserIds);
            return PartialView("_ItemPartial", updatedItem);
        }

    }
}