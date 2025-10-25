using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using habits.Dtos;
using habits.Services.Tasks;

namespace habits.Controllers
{
    [Authorize]
    public class TaskController : Controller
    {
        private ITaskService _taskService;

        public TaskController(ITaskService taskService)
        {
            _taskService = taskService;
        }

        public IActionResult Index()
        {
            ViewBag.IsHTMXRequest = HttpContext.Request.Headers.ContainsKey("HX-Request");

            if (HttpContext.Request.Headers.ContainsKey("HX-Request"))
                return PartialView();

            return View();
        }

        public IActionResult GetTaskLists()
        {
            var data = _taskService.GetTaskLists();
            return PartialView("_TaskLists", data);
        }

        public IActionResult AddTaskList()
        {
            return PartialView("_AddTaskListModal", new TaskListDto());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateTaskList(TaskListDto model)
        {
            var result = _taskService.CreateTaskList(model);
            return PartialView("_TaskListsPartial", result);
        }

        [HttpGet]
        public IActionResult EditTaskList(int id)
        {
            var taskList = _taskService.GetTaskListById(id);
            return PartialView("_EditTaskListModal", taskList);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateTaskList(int id, string name, string description)
        {
            var updatedTaskList = _taskService.UpdateTaskList(id, name, description);
            return PartialView("_TaskListContent", updatedTaskList);
        }

        [HttpDelete]
        public IActionResult DeleteTaskList(int id)
        {
            var success = _taskService.DeleteTaskList(id);
            if (success)
            {
                return Ok();
            }
            return BadRequest();
        }

        [HttpPost]
        public async Task<IActionResult> UpdateTaskListOrder([FromForm] string id, [FromForm] string newPosition)
        {
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(newPosition))
            {
                return BadRequest("Request data is null");
            }

            try
            {
                var result = await _taskService.UpdateTaskListOrderAsync(Convert.ToInt32(id), Convert.ToInt32(newPosition));
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
    }
}