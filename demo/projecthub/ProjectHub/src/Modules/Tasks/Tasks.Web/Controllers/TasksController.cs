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

        return View(tasks);
    }

    [HttpGet("create")]
    public async Task<IActionResult> Create(int? projectId)
    {
        var projects = await projectService.GetAllAsync();
        ViewBag.Projects = projects;
        ViewBag.SelectedProjectId = projectId;

        return View();
    }

    [HttpPost("create")]
    public async Task<IActionResult> Create(CreateTaskDto dto)
    {
        if (!ModelState.IsValid)
        {
            var projects = await projectService.GetAllAsync();
            ViewBag.Projects = projects;
            return View(dto);
        }

        var task = await taskService.CreateAsync(dto);

        return RedirectToAction(nameof(Index), new { projectId = task.ProjectId });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Details(int id)
    {
        var task = await taskService.GetByIdAsync(id);
        if (task is null) return NotFound();

        return View(task);
    }

    [HttpGet("{id}/edit")]
    public async Task<IActionResult> Edit(int id)
    {
        var task = await taskService.GetByIdAsync(id);
        if (task is null) return NotFound();

        var projects = await projectService.GetAllAsync();
        ViewBag.Projects = projects;

        return View(task);
    }

    [HttpPost("{id}/edit")]
    public async Task<IActionResult> Edit(int id, UpdateTaskDto dto)
    {
        if (!ModelState.IsValid)
        {
            var task = await taskService.GetByIdAsync(id);
            var projects = await projectService.GetAllAsync();
            ViewBag.Projects = projects;
            return View(task);
        }

        await taskService.UpdateAsync(id, dto);

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost("{id}/delete")]
    public async Task<IActionResult> Delete(int id)
    {
        await taskService.DeleteAsync(id);

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("{id}/move")]
    public async Task<IActionResult> Move(int id, [FromBody] MoveTaskDto dto)
    {
        var task = await taskService.MoveAsync(id, dto);

        return Ok(task);
    }
}
