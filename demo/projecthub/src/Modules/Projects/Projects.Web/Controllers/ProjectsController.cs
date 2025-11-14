using Microsoft.AspNetCore.Mvc;
using ProjectHub.Modules.Projects.Contracts;
using ProjectHub.Modules.Workspaces.Contracts;
using ProjectHub.Modules.Tasks.Contracts;
using Swap.Htmx;
using TaskStatus = ProjectHub.Modules.Tasks.Contracts.TaskStatus;

namespace ProjectHub.Modules.Projects.Web.Controllers;

[Route("projects")]
public class ProjectsController : SwapController
{
    private readonly IProjectService _projectService;
    private readonly IWorkspaceService _workspaceService;
    private readonly ITaskService _taskService;

    public ProjectsController(
        IProjectService projectService,
        IWorkspaceService workspaceService,
        ITaskService taskService)
    {
        _projectService = projectService;
        _workspaceService = workspaceService;
        _taskService = taskService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var projects = await _projectService.GetAllAsync();
        return SwapView(projects);
    }

    [HttpGet("workspace/{workspaceId}")]
    public async Task<IActionResult> ByWorkspace(int workspaceId)
    {
        var projects = await _projectService.GetByWorkspaceAsync(workspaceId);
        var workspace = await _workspaceService.GetByIdAsync(workspaceId);

        ViewBag.Workspace = workspace;
        return SwapView("Index", projects);
    }

    [HttpGet("create")]
    public async Task<IActionResult> Create([FromQuery] int? workspaceId)
    {
        var workspaces = await _workspaceService.GetActiveAsync();
        ViewBag.Workspaces = workspaces;
        ViewBag.SelectedWorkspaceId = workspaceId;
        return SwapView();
    }

    [HttpPost("create")]
    public async Task<IActionResult> CreatePost([FromForm] CreateProjectDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var project = await _projectService.CreateAsync(dto);
        var detailsProject = await _projectService.GetByIdAsync(project.Id);

        // Set ViewBag data required by Details view
        var tasks = await _taskService.GetByProjectIdAsync(project.Id);
        ViewBag.Tasks = tasks;
        ViewBag.TaskStats = new
        {
            Total = tasks.Count(),
            Completed = tasks.Count(t => t.Status == TaskStatus.Done),
            InProgress = tasks.Count(t => t.Status == TaskStatus.InProgress),
            Todo = tasks.Count(t => t.Status == TaskStatus.Todo),
            Backlog = tasks.Count(t => t.Status == TaskStatus.Backlog),
            Review = tasks.Count(t => t.Status == TaskStatus.Review)
        };

        return SwapView("Details", detailsProject);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Details(int id)
    {
        var project = await _projectService.GetByIdAsync(id);
        if (project is null)
            return NotFound();

        // Cross-module service call to get tasks for this project
        var tasks = await _taskService.GetByProjectIdAsync(id);
        ViewBag.Tasks = tasks;
        ViewBag.TaskStats = new
        {
            Total = tasks.Count(),
            Completed = tasks.Count(t => t.Status == TaskStatus.Done),
            InProgress = tasks.Count(t => t.Status == TaskStatus.InProgress),
            Todo = tasks.Count(t => t.Status == TaskStatus.Todo),
            Backlog = tasks.Count(t => t.Status == TaskStatus.Backlog),
            Review = tasks.Count(t => t.Status == TaskStatus.Review)
        };

        return SwapView(project);
    }

    [HttpGet("{id}/edit")]
    public async Task<IActionResult> Edit(int id)
    {
        var project = await _projectService.GetByIdAsync(id);
        if (project is null)
            return NotFound();

        return SwapView(project);
    }

    [HttpPost("{id}/edit")]
    public async Task<IActionResult> EditPost(int id, [FromForm] UpdateProjectDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var project = await _projectService.UpdateAsync(id, dto);
        var detailsProject = await _projectService.GetByIdAsync(project.Id);

        // Set ViewBag data required by Details view
        var tasks = await _taskService.GetByProjectIdAsync(id);
        ViewBag.Tasks = tasks;
        ViewBag.TaskStats = new
        {
            Total = tasks.Count(),
            Completed = tasks.Count(t => t.Status == TaskStatus.Done),
            InProgress = tasks.Count(t => t.Status == TaskStatus.InProgress),
            Todo = tasks.Count(t => t.Status == TaskStatus.Todo),
            Backlog = tasks.Count(t => t.Status == TaskStatus.Backlog),
            Review = tasks.Count(t => t.Status == TaskStatus.Review)
        };

        return SwapView("Details", detailsProject);
    }

    [HttpPost("archive/{id}")]
    public async Task<IActionResult> Archive(int id)
    {
        await _projectService.ArchiveAsync(id);
        var projects = await _projectService.GetAllAsync();
        return SwapView("Index", projects);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _projectService.DeleteAsync(id);
        var projects = await _projectService.GetAllAsync();
        return SwapView("Index", projects);
    }
}
