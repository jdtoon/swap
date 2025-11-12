namespace ProjectHub.Modules.Tasks.Contracts.Events;

public record TaskMoved(
    int TaskId,
    int ProjectId,
    TaskStatus Status,
    int OldPosition,
    int NewPosition,
    DateTimeOffset MovedAt
);
