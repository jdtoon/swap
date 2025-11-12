namespace ProjectHub.Modules.Tasks.Contracts.Events;

public record TaskCreated(
    int TaskId,
    int ProjectId,
    string Title,
    TaskStatus Status,
    int? AssignedToUserId,
    DateTimeOffset CreatedAt
);
