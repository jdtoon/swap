namespace ProjectHub.Modules.Tasks.Contracts.Events;

public record TaskUpdated(
    int TaskId,
    int ProjectId,
    DateTimeOffset UpdatedAt
);
