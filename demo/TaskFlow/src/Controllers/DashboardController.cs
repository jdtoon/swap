using Microsoft.AspNetCore.Mvc;
using Swap.Htmx;
using Swap.Htmx.Extensions;
using Swap.Htmx.ServerSentEvents;
using TaskFlow.Services;
using TaskFlow.Views;

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

    public DashboardController(
        ITaskService taskService,
        IProjectService projectService,
        ITeamService teamService,
        IActivityService activityService)
    {
        _taskService = taskService;
        _projectService = projectService;
        _teamService = teamService;
        _activityService = activityService;
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
        return ServerSentEvents(async (stream, cancel) =>
        {
            // Demonstrates SSE for real-time dashboard updates
            // In production, subscribe to event bus/Redis/etc.
            
            await stream.SendEventAsync("heartbeat", "connected");

            // Push updates when stats/activity changes
            // Example: stats update event
            var stats = _teamService.GetStats();
            var statsHtml = $@"<div class='stats'>
                <div>Total Tasks: {stats.TotalTasks}</div>
                <div>In Progress: {stats.InProgressTasks}</div>
                <div>Completed: {stats.CompletedTasks}</div>
            </div>";
            await stream.SendEventAsync("stats-update", statsHtml);
            
            // Keep connection alive
            while (!cancel.IsCancellationRequested)
            {
                await Task.Delay(30000, cancel);
                if (!cancel.IsCancellationRequested)
                {
                    await stream.SendKeepAliveAsync();
                }
            }
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
}
