using Microsoft.AspNetCore.Mvc;
using ProjectHub.Modules.Projects.Contracts;
using ProjectHub.Modules.Tasks.Contracts;
using Swap.Htmx;
using Swap.Htmx.Events;

namespace ProjectHub.Modules.Tasks.Web.Controllers;

[Route("tasks")]
public class TasksController(ITaskService taskService, IProjectService projectService, ISwapEventBus eventBus) : SwapController
{
    [HttpGet]
    public async Task<IActionResult> Index(int? projectId)
    {
        var tasks = projectId.HasValue
            ? await taskService.GetByProjectIdAsync(projectId.Value)
            : await taskService.GetAllAsync();

        var projects = await projectService.GetAllAsync();

        ViewBag.Projects = projects;
        ViewBag.SelectedProjectId = projectId;

        return SwapView(tasks);
    }

    [HttpGet("create")]
    public async Task<IActionResult> Create(int? projectId)
    {
        var projects = await projectService.GetAllAsync();
        ViewBag.Projects = projects;
        ViewBag.SelectedProjectId = projectId;

        return SwapView();
    }

    [HttpPost("create")]
    public async Task<IActionResult> Create(CreateTaskDto dto)
    {
        if (!ModelState.IsValid)
        {
            var projects = await projectService.GetAllAsync();
            ViewBag.Projects = projects;
            return SwapView(dto);
        }

        var task = await taskService.CreateAsync(dto);
        eventBus.Emit(TaskEvents.Created, new { taskId = task.Id });
        Response.ShowSuccessToast("Task created successfully");

        var tasks = await taskService.GetByProjectIdAsync(task.ProjectId);
        var projectsList = await projectService.GetAllAsync();
        ViewBag.Projects = projectsList;
        ViewBag.SelectedProjectId = task.ProjectId;

        return SwapView("Index", tasks);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Details(int id)
    {
        var task = await taskService.GetByIdAsync(id);
        if (task is null) return NotFound();

        return SwapView(task);
    }

    [HttpGet("{id}/edit")]
    public async Task<IActionResult> Edit(int id)
    {
        var task = await taskService.GetByIdAsync(id);
        if (task is null) return NotFound();

        var projects = await projectService.GetAllAsync();
        ViewBag.Projects = projects;

        return SwapView(task);
    }

    [HttpPost("{id}/edit")]
    public async Task<IActionResult> Edit(int id, UpdateTaskDto dto)
    {
        if (!ModelState.IsValid)
        {
            var task = await taskService.GetByIdAsync(id);
            var projects = await projectService.GetAllAsync();
            ViewBag.Projects = projects;
            return SwapView(task);
        }

        await taskService.UpdateAsync(id, dto);
        eventBus.Emit(TaskEvents.Updated, new { taskId = id });
        Response.ShowSuccessToast("Task updated successfully");

        var detailsTask = await taskService.GetByIdAsync(id);
        return SwapView("Details", detailsTask);
    }

    [HttpPost("{id}/delete")]
    public async Task<IActionResult> Delete(int id)
    {
        await taskService.DeleteAsync(id);
        eventBus.Emit(TaskEvents.Deleted, new { taskId = id });
        Response.ShowSuccessToast("Task deleted successfully");

        var tasks = await taskService.GetAllAsync();
        var projects = await projectService.GetAllAsync();
        ViewBag.Projects = projects;
        ViewBag.SelectedProjectId = (int?)null;

        return SwapView("Index", tasks);
    }

    [HttpGet("kanban")]
    public async Task<IActionResult> Kanban(int? projectId)
    {
        var tasks = projectId.HasValue
            ? await taskService.GetByProjectIdAsync(projectId.Value)
            : await taskService.GetAllAsync();

        var projects = await projectService.GetAllAsync();

        ViewBag.Projects = projects;
        ViewBag.SelectedProjectId = projectId;

        return SwapView(tasks);
    }

    [HttpPost("{id}/move")]
    public async Task<IActionResult> Move(int id, [FromBody] MoveTaskDto dto)
    {
        var task = await taskService.MoveAsync(id, dto);
        eventBus.Emit(TaskEvents.Moved, new { taskId = id, newStatus = dto.NewStatus });
        Response.ShowSuccessToast($"Task moved to {dto.NewStatus}");

        // Return the updated Kanban columns partial
        var tasks = await taskService.GetAllAsync();
        var projects = await projectService.GetAllAsync();
        ViewBag.Projects = projects;
        ViewBag.SelectedProjectId = (int?)null;

        return PartialView("_KanbanColumns", tasks);
    }
}
