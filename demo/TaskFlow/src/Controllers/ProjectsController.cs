using Microsoft.AspNetCore.Mvc;
using Swap.Htmx;
using Swap.Htmx.Extensions;
using TaskFlow.Services;
using TaskFlow.Models;
using TaskFlow.Events;
using TaskFlow.Views;

namespace TaskFlow.Controllers;

/// <summary>
/// Demonstrates project management with progress tracking
/// </summary>
public class ProjectsController : SwapController
{
    private readonly IProjectService _projectService;
    private readonly ITaskService _taskService;
    private readonly IActivityService _activityService;

    public ProjectsController(
        IProjectService projectService,
        ITaskService taskService,
        IActivityService activityService)
    {
        _projectService = projectService;
        _taskService = taskService;
        _activityService = activityService;
    }

    [HttpGet("/projects")]
    public IActionResult Index()
    {
        var projects = _projectService.GetAll();
        return View(ProjectViews.Index, projects);
    }

    [HttpGet("/projects/{id}")]
    public IActionResult Details(int id)
    {
        var project = _projectService.Get(id);
        if (project == null)
        {
            return NotFound();
        }

        var tasks = _taskService.GetByProject(id);
        var progress = _projectService.GetProgress(id);
        var breakdown = _projectService.GetTaskBreakdown(id);

        var model = new
        {
            Project = project,
            Tasks = tasks,
            Progress = progress,
            Breakdown = breakdown
        };

        return View(ProjectViews.Details, model);
    }

    [HttpPost("/projects")]
    public IActionResult Create([FromForm] ProjectInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Name))
        {
            return SwapResponse()
                .WithToast("Project name is required", ToastType.Error)
                .Build();
        }

        var project = _projectService.Create(input);

        _activityService.LogActivity(
            description: $"Created project: {project.Name}",
            projectId: project.Id,
            userId: "demo-user"
        );

        return SwapResponse()
            .RefreshPartial(ProjectElements.List, ProjectViews.List, _projectService.GetAll())
            .TriggerEvent(ProjectEvents.Created, project)
            .Build();
    }

    [HttpPatch("/projects/{id}")]
    public IActionResult Update(int id, [FromForm] ProjectInput input)
    {
        var project = _projectService.Get(id);
        if (project == null)
        {
            return SwapResponse()
                .WithToast("Project not found", ToastType.Error)
                .Build();
        }

        _projectService.Update(id, input);
        project = _projectService.Get(id)!;

        _activityService.LogActivity(
            description: $"Updated project: {project.Name}",
            projectId: project.Id,
            userId: "demo-user"
        );

        return SwapResponse()
            .RefreshPartial(ProjectElements.Card(id), ProjectViews.Card, project)
            .WithToast("Project updated", ToastType.Success)
            .Build();
    }

    [HttpDelete("/projects/{id}")]
    public IActionResult Delete(int id)
    {
        var project = _projectService.Get(id);
        if (project == null)
        {
            return SwapResponse()
                .WithToast("Project not found", ToastType.Error)
                .Build();
        }

        _projectService.Delete(id);

        _activityService.LogActivity(
            description: $"Deleted project: {project.Name}",
            userId: "demo-user"
        );

        return SwapResponse()
            .DeleteElement(ProjectElements.Card(id))
            .WithToast("Project deleted", ToastType.Info)
            .Build();
    }

    [HttpGet("/projects/{id}/progress")]
    public IActionResult GetProgress(int id)
    {
        var progress = _projectService.GetProgress(id);
        return PartialView(ProjectViews.ProgressBar, progress);
    }
}
