using Microsoft.AspNetCore.Mvc;
using Swap.Htmx;
using Swap.Htmx.Extensions;
using Swap.Htmx.ServerSentEvents;
using TaskFlow.Services;
using TaskFlow.Views;
using TaskFlow.Events;
using TaskFlow.Models;

namespace TaskFlow.Controllers;

/// <summary>
/// Demonstrates real-time dashboard with SSE, complex OOB swaps, and activity feed
/// </summary>
public class DashboardController : SwapController
{
    private readonly ITaskService _taskService;
    private readonly IProjectService _projectService;
    private readonly ITeamService _teamService;
    private readonly IActivityService _activityService;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(
        ITaskService taskService,
        IProjectService projectService,
        ITeamService teamService,
        IActivityService activityService,
        ILogger<DashboardController> logger)
    {
        _taskService = taskService;
        _projectService = projectService;
        _teamService = teamService;
        _activityService = activityService;
        _logger = logger;
    }

    [HttpGet("/")]
    [HttpGet("/dashboard")]
    public IActionResult Index()
    {
        var stats = _teamService.GetStats();
        var activities = _activityService.GetRecent(10);
        var teamMembers = _teamService.GetAll();
        var projects = _projectService.GetAll();

        var model = new
        {
            Stats = stats,
            Activities = activities,
            TeamMembers = teamMembers,
            Projects = projects
        };

        return SwapView(DashboardViews.Index, model);
    }

    // ================================================================================
    // SSE ENDPOINT - Real-time dashboard updates
    // ================================================================================

    [HttpGet("/dashboard/stream")]
    public IActionResult DashboardStream()
    {
        return ServerSentEvents(async (conn, ct) =>
        {
            // Subscribe to dashboard update events
            conn.WithEvents(
                DashboardSseEvents.StatsUpdate,
                DashboardSseEvents.ActivityUpdate,
                DashboardSseEvents.TeamUpdate
            );

            // Keep connection alive with heartbeats
            await conn.KeepAlive(TimeSpan.FromSeconds(30), ct);
        });
    }

    [HttpGet("/dashboard/stats")]
    public IActionResult GetStats()
    {
        var stats = _teamService.GetStats();
        return PartialView(DashboardViews.Stats, stats);
    }

    [HttpGet("/dashboard/activity")]
    public IActionResult GetActivity()
    {
        var activities = _activityService.GetRecent(10);
        return PartialView(DashboardViews.Activity, activities);
    }

    [HttpGet("/dashboard/team")]
    public IActionResult GetTeam()
    {
        var teamMembers = _teamService.GetAll();
        return PartialView(DashboardViews.TeamList, teamMembers);
    }

    // ================================================================================
    // SSE TEST ENDPOINT - Creates a test task to trigger SSE updates
    // ================================================================================

    [HttpPost("/dashboard/test-sse")]
    public IActionResult TestSse()
    {
        _logger.LogInformation("[Dashboard] TestSse endpoint called");
        
        // Create a test task using the TaskInput model
        var taskInput = new TaskInput
        {
            Title = $"SSE Test Task - {DateTime.Now:HH:mm:ss}",
            Description = "This task was created to test SSE functionality",
            Priority = TaskFlow.Models.TaskPriority.Medium,
            ProjectId = 1,
            AssigneeId = "alice" // Alice from team
        };

        _logger.LogInformation("[Dashboard] Creating task: {Title}", taskInput.Title);
        var task = _taskService.Create(taskInput, "demo-user");
        _logger.LogInformation("[Dashboard] Task created with ID: {TaskId}", task.Id);

        // Trigger events that will broadcast via SSE
        _logger.LogInformation("[Dashboard] Triggering TaskEvents.Created with event name: {EventName}", TaskEvents.Created);
        var response = SwapResponse()
            .WithTrigger(TaskEvents.Created, task)
            .WithToast($"Created task: {task.Title}", ToastType.Success)
            .Build();
        
        _logger.LogInformation("[Dashboard] SwapResponse built, returning to client");
        return response;
    }
}