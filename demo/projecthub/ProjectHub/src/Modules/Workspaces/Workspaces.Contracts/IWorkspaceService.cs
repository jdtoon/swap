namespace ProjectHub.Modules.Workspaces.Contracts;

public interface IWorkspaceService
{
    Task<WorkspaceDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<WorkspaceDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<WorkspaceDto>> GetActiveAsync(CancellationToken cancellationToken = default);
    Task<WorkspaceDto> CreateAsync(CreateWorkspaceDto dto, CancellationToken cancellationToken = default);
    Task<WorkspaceDto> UpdateAsync(int id, UpdateWorkspaceDto dto, CancellationToken cancellationToken = default);
    Task ArchiveAsync(int id, CancellationToken cancellationToken = default);
    Task UnarchiveAsync(int id, CancellationToken cancellationToken = default);
}

public record WorkspaceDto(
    int Id,
    string Name,
    string? Description,
    string? Color,
    bool IsArchived,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public record CreateWorkspaceDto(
    string Name,
    string? Description,
    string? Color);

public record UpdateWorkspaceDto(
    string Name,
    string? Description,
    string? Color);
