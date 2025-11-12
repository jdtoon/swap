using ProjectHub.Modules.Workspaces.Contracts;

namespace ProjectHub.Modules.Projects.Contracts;

public interface IProjectService
{
    Task<ProjectDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<ProjectDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<ProjectDto>> GetByWorkspaceAsync(int workspaceId, CancellationToken cancellationToken = default);
    Task<ProjectDto> CreateAsync(CreateProjectDto dto, CancellationToken cancellationToken = default);
    Task<ProjectDto> UpdateAsync(int id, UpdateProjectDto dto, CancellationToken cancellationToken = default);
    Task ArchiveAsync(int id, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}

public record ProjectDto(
    int Id,
    int WorkspaceId,
    string WorkspaceName,
    string Name,
    string? Description,
    string Status,
    DateTime? StartDate,
    DateTime? DueDate,
    bool IsArchived,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public record CreateProjectDto(
    int WorkspaceId,
    string Name,
    string? Description,
    string Status,
    DateTime? StartDate,
    DateTime? DueDate);

public record UpdateProjectDto(
    string Name,
    string? Description,
    string Status,
    DateTime? StartDate,
    DateTime? DueDate);
