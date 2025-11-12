namespace ProjectHub.Modules.Tasks.Contracts;

public record TaskDto(
    int Id,
    int ProjectId,
    string ProjectName,
    string Title,
    string? Description,
    TaskStatus Status,
    TaskPriority Priority,
    int? AssignedToUserId,
    string? AssignedToUserName,
    DateTimeOffset? DueDate,
    int Position,
    bool IsArchived,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt
);

public record CreateTaskDto(
    int ProjectId,
    string Title,
    string? Description,
    TaskStatus Status,
    TaskPriority Priority,
    int? AssignedToUserId,
    DateTimeOffset? DueDate
);

public record UpdateTaskDto(
    string? Title,
    string? Description,
    TaskStatus? Status,
    TaskPriority? Priority,
    int? AssignedToUserId,
    DateTimeOffset? DueDate,
    bool? IsArchived
);

public record MoveTaskDto(
    TaskStatus NewStatus,
    int NewPosition
);
