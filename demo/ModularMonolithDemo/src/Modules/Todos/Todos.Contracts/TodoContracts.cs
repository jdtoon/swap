namespace ModularMonolithDemo.Modules.Todos.Contracts;

public static class TodoEvents
{
    public static class Domain
    {
        public const string Created = "Todos.TodoCreated";
        public const string Deleted = "Todos.TodoDeleted";
        public const string Toggled = "Todos.TodoToggled";
    }

    // UI events owned by the module
    public static class Ui
    {
        public const string RefreshList = "ui.todos.refreshList";
        public const string ToastSuccess = "ui.todos.toast.success";
        public const string StatsRefresh = "ui.todos.stats.refresh";
    }
}

public record TodoItemDto(int Id, string Title, bool IsComplete);
