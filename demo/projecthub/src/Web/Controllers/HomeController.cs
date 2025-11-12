using Microsoft.AspNetCore.Mvc;
using ProjectHub.Modules.Workspaces.Contracts;
using ProjectHub.Modules.Projects.Contracts;
using ProjectHub.Modules.Tasks.Contracts;
using Swap.Htmx;
using TaskStatus = ProjectHub.Modules.Tasks.Contracts.TaskStatus;

namespace ProjectHub.Web.Controllers;

public class HomeController : SwapController
{
    private readonly IWorkspaceService _workspaceService;
    private readonly IProjectService _projectService;
    private readonly ITaskService _taskService;

    public HomeController(
        IWorkspaceService workspaceService,
        IProjectService projectService,
        ITaskService taskService)
    {
        _workspaceService = workspaceService;
        _projectService = projectService;
        _taskService = taskService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var workspaces = await _workspaceService.GetAllAsync();
        var projects = await _projectService.GetAllAsync();
        var tasks = await _taskService.GetAllAsync();

        var stats = new
        {
            WorkspaceCount = workspaces.Count(),
            ProjectCount = projects.Count(),
            TaskCount = tasks.Count(),
            CompletedTaskCount = tasks.Count(t => t.Status == TaskStatus.Done),
            InProgressTaskCount = tasks.Count(t => t.Status == TaskStatus.InProgress)
        };

        ViewBag.Stats = stats;
        ViewBag.RecentProjects = projects.OrderByDescending(p => p.CreatedAt).Take(6);

        return SwapView();
    }
}
