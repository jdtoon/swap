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

// Typed server event payloads for AOT-friendly, unambiguous deserialization
public static class TodoEventPayloads
{
    public record Created(int Id);
    public record Deleted(int Id);
    public record Toggled(int Id);
}
