namespace ModularMonolithDemo.Modules.Todos.Contracts;

public static class TodoEvents
{
    public static class Domain
    {
        public const string Created = "todo.created";
        public const string Deleted = "todo.deleted";
        public const string Toggled = "todo.toggled";
    }

    // UI events owned by the module
    public static class Ui
    {
        public const string RefreshList = "ui.todo.refreshList";
        public const string ToastSuccess = "ui.toast.success";
        public const string StatsRefresh = "ui.stats.refresh";
        public const string ActivityAppend = "ui.activity.append";
    }
}

public record TodoItemDto(int Id, string Title, bool IsComplete);
