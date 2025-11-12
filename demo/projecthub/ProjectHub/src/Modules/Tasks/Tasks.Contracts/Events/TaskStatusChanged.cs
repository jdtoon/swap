namespace ProjectHub.Modules.Tasks.Contracts.Events;

public record TaskStatusChanged(
    int TaskId,
    int ProjectId,
    TaskStatus OldStatus,
    TaskStatus NewStatus,
    DateTimeOffset ChangedAt
);
