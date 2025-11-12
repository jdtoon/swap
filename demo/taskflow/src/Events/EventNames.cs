using Swap.Htmx.Events;

namespace TaskFlow.Events;

public static class EventNames
{
    public static class Domain
    {
        // Original Todo events
        public static readonly EventKey TodoCreated = new("todo.created");
        public static readonly EventKey TodoDeleted = new("todo.deleted");
        public static readonly EventKey ProjectNoteAdded = new("project.note.added");
        public static readonly EventKey BulkCompleted = new("bulk.completed");
        public static readonly EventKey TodoToggled = new("todo.toggled");
        
        // Task Management events
        public static readonly EventKey TaskCreated = new("task.created");
        public static readonly EventKey TaskUpdated = new("task.updated");
        public static readonly EventKey TaskDeleted = new("task.deleted");
        public static readonly EventKey TaskStatusChanged = new("task.status.changed");
        public static readonly EventKey TaskPriorityChanged = new("task.priority.changed");
        public static readonly EventKey TaskAssigned = new("task.assigned");
        
        // Component events
        public static readonly EventKey ComponentAUpdated = new("component.a.updated");
        public static readonly EventKey ComponentBUpdated = new("component.b.updated");
    }

    public static class Ui
    {
        // Original Todo UI events
        public static readonly EventKey TodoRefreshList = new("ui.todo.refreshList");
        public static readonly EventKey ToastSuccess = new("ui.toast.success");
        public static readonly EventKey StatsRefresh = new("ui.stats.refresh");
        public static readonly EventKey ActivityAppend = new("ui.activity.append");
        public static readonly EventKey DetailsRefresh = new("ui.details.refresh");
        public static readonly EventKey SummaryRefresh = new("ui.summary.refresh");
        
        // Task Board UI events
        public static readonly EventKey TaskBoardRefresh = new("ui.taskBoard.refresh");
        public static readonly EventKey TaskStatsRefresh = new("ui.taskStats.refresh");
        public static readonly EventKey TaskActivityRefresh = new("ui.taskActivity.refresh");
        public static readonly EventKey TaskTodoRefresh = new("ui.taskTodo.refresh");
        public static readonly EventKey TaskInProgressRefresh = new("ui.taskInProgress.refresh");
        public static readonly EventKey TaskDoneRefresh = new("ui.taskDone.refresh");
        
        // Component events
        public static readonly EventKey ComponentARefresh = new("ui.components.a.refresh");
        public static readonly EventKey ComponentBRefresh = new("ui.components.b.refresh");
    }
}
