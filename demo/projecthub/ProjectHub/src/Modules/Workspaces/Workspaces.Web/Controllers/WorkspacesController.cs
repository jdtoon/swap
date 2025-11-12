using Microsoft.AspNetCore.Mvc;
using ProjectHub.Modules.Workspaces.Contracts;
using Swap.Htmx;

namespace ProjectHub.Modules.Workspaces.Web.Controllers;

[Route("workspaces")]
public class WorkspacesController : SwapController
{
    private readonly IWorkspaceService _service;

    public WorkspacesController(IWorkspaceService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var workspaces = await _service.GetActiveAsync();
        return SwapView(workspaces);
    }

    [HttpGet("create")]
    public IActionResult Create()
    {
        return SwapView();
    }

    [HttpPost("create")]
    public async Task<IActionResult> CreatePost([FromForm] CreateWorkspaceDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var workspace = await _service.CreateAsync(dto);
        var workspaces = await _service.GetActiveAsync();
        return SwapView("Index", workspaces);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Details(int id)
    {
        var workspace = await _service.GetByIdAsync(id);
        if (workspace is null)
            return NotFound();

        return SwapView(workspace);
    }

    [HttpGet("{id}/edit")]
    public async Task<IActionResult> Edit(int id)
    {
        var workspace = await _service.GetByIdAsync(id);
        if (workspace is null)
            return NotFound();

        return SwapView(workspace);
    }

    [HttpPost("{id}/edit")]
    public async Task<IActionResult> EditPost(int id, [FromForm] UpdateWorkspaceDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        await _service.UpdateAsync(id, dto);
        var workspace = await _service.GetByIdAsync(id);
        return SwapView("Details", workspace);
    }

    [HttpPost("archive/{id}")]
    public async Task<IActionResult> Archive(int id)
    {
        await _service.ArchiveAsync(id);
        var workspaces = await _service.GetActiveAsync();
        return SwapView("Index", workspaces);
    }

    [HttpPost("unarchive/{id}")]
    public async Task<IActionResult> Unarchive(int id)
    {
        await _service.UnarchiveAsync(id);
        var workspaces = await _service.GetActiveAsync();
        return SwapView("Index", workspaces);
    }
}
