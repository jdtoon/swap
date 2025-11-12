using Microsoft.AspNetCore.Mvc;
using ProjectHub.Modules.Projects.Contracts;
using ProjectHub.Modules.Workspaces.Contracts;
using Swap.Htmx;

namespace ProjectHub.Modules.Projects.Web.Controllers;

[Route("projects")]
public class ProjectsController : SwapController
{
    private readonly IProjectService _projectService;
    private readonly IWorkspaceService _workspaceService;

    public ProjectsController(IProjectService projectService, IWorkspaceService workspaceService)
    {
        _projectService = projectService;
        _workspaceService = workspaceService;
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
        return RedirectToAction(nameof(Details), new { id = project.Id });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Details(int id)
    {
        var project = await _projectService.GetByIdAsync(id);
        if (project is null)
            return NotFound();

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
        return RedirectToAction(nameof(Details), new { id = project.Id });
    }

    [HttpPost("archive/{id}")]
    public async Task<IActionResult> Archive(int id)
    {
        await _projectService.ArchiveAsync(id);
        return RedirectToAction(nameof(Index));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _projectService.DeleteAsync(id);
        return RedirectToAction(nameof(Index));
    }
}
