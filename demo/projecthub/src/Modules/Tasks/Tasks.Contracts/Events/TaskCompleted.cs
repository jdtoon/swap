namespace ProjectHub.Modules.Tasks.Contracts.Events;

public record TaskCompleted(
    int TaskId,
    int ProjectId,
    int? CompletedByUserId,
    DateTimeOffset CompletedAt
);
