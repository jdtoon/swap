using Microsoft.AspNetCore.Mvc;
using ProjectHub.Modules.Projects.Contracts;
using ProjectHub.Modules.Tasks.Contracts;
using Swap.Htmx;

namespace ProjectHub.Modules.Tasks.Web.Controllers;

[Route("tasks")]
public class TasksController(ITaskService taskService, IProjectService projectService) : SwapController
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

        Response.HxRedirect($"/tasks?projectId={task.ProjectId}");
        return Ok();
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

        Response.HxRedirect($"/tasks/{id}");
        return Ok();
    }

    [HttpPost("{id}/delete")]
    public async Task<IActionResult> Delete(int id)
    {
        await taskService.DeleteAsync(id);

        Response.HxRedirect("/tasks");
        return Ok();
    }

    [HttpPost("{id}/move")]
    public async Task<IActionResult> Move(int id, [FromBody] MoveTaskDto dto)
    {
        var task = await taskService.MoveAsync(id, dto);

        return Ok(task);
    }
}
