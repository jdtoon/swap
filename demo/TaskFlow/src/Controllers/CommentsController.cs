using Microsoft.AspNetCore.Mvc;
using Swap.Htmx;
using Swap.Htmx.Extensions;
using TaskFlow.Services;
using TaskFlow.Models;
using TaskFlow.Events;
using TaskFlow.Views;

namespace TaskFlow.Controllers;

/// <summary>
/// Demonstrates BeforeEnd swap mode for chronological comment insertion
/// </summary>
public class CommentsController : SwapController
{
    private readonly ICommentService _commentService;
    private readonly ITaskService _taskService;
    private readonly IActivityService _activityService;

    public CommentsController(
        ICommentService commentService,
        ITaskService taskService,
        IActivityService activityService)
    {
        _commentService = commentService;
        _taskService = taskService;
        _activityService = activityService;
    }

    [HttpGet("/tasks/{taskId}/comments")]
    public IActionResult GetComments(int taskId)
    {
        var comments = _commentService.GetByTask(taskId);
        return PartialView(CommentViews.List, comments);
    }

    [HttpPost("/tasks/{taskId}/comments")]
    public IActionResult Create(int taskId, [FromForm] CommentInput input)
    {
        var task = _taskService.Get(taskId);
        if (task == null)
        {
            return this.Toast("Task not found", ToastType.Error);
        }

        if (string.IsNullOrWhiteSpace(input.Content))
        {
            return this.Toast("Comment cannot be empty", ToastType.Error);
        }

        var comment = _commentService.Create(taskId, input);

        _activityService.LogActivity(
            description: $"Commented on '{task.Title}'",
            taskId: taskId,
            projectId: task.ProjectId,
            userId: "demo-user"
        );

        // Demonstrates BeforeEnd swap mode - inserts new comment at end of list
        return this.SwapBuilder()
            .AddPartial(
                targetId: CommentElements.List(taskId),
                viewName: CommentViews.CommentCard,
                model: comment,
                swapMode: SwapMode.BeforeEnd // NEW: Insert before closing tag
            )
            .AlsoUpdateById(
                CommentElements.Count(taskId),
                CommentViews.Count,
                _commentService.GetByTask(taskId).Count
            )
            .TriggerEvent(CommentEvents.Added, comment)
            .Build();
    }

    [HttpPatch("/comments/{id}")]
    public IActionResult Update(int id, [FromForm] CommentInput input)
    {
        var comment = _commentService.Get(id);
        if (comment == null)
        {
            return this.Toast("Comment not found", ToastType.Error);
        }

        if (string.IsNullOrWhiteSpace(input.Content))
        {
            return this.Toast("Comment cannot be empty", ToastType.Error);
        }

        _commentService.Update(id, input);
        comment = _commentService.Get(id)!;

        return this.SwapBuilder()
            .RefreshPartial(CommentElements.Card(id), CommentViews.CommentCard, comment)
            .Toast("Comment updated", ToastType.Info)
            .Build();
    }

    [HttpDelete("/comments/{id}")]
    public IActionResult Delete(int id)
    {
        var comment = _commentService.Get(id);
        if (comment == null)
        {
            return this.Toast("Comment not found", ToastType.Error);
        }

        var taskId = comment.TaskId;
        _commentService.Delete(id);

        // Demonstrates DELETE swap mode
        return this.SwapBuilder()
            .DeleteElement(CommentElements.Card(id))
            .AlsoUpdateById(
                CommentElements.Count(taskId),
                CommentViews.Count,
                _commentService.GetByTask(taskId).Count
            )
            .TriggerEvent(CommentEvents.Deleted)
            .Build();
    }
}
