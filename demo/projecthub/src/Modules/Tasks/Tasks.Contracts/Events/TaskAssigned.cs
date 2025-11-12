namespace ProjectHub.Modules.Tasks.Contracts.Events;

public record TaskAssigned(
    int TaskId,
    int ProjectId,
    int? OldAssigneeId,
    int? NewAssigneeId,
    DateTimeOffset AssignedAt
);
