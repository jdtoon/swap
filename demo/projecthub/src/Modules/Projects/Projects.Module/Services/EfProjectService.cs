using Microsoft.EntityFrameworkCore;
using ProjectHub.Modules.Projects.Contracts;
using ProjectHub.Modules.Projects.Module.Domain;
using ProjectHub.Modules.Projects.Module.Persistence;
using ProjectHub.Modules.Workspaces.Contracts;

namespace ProjectHub.Modules.Projects.Module.Services;

internal sealed class EfProjectService : IProjectService
{
    private readonly ProjectsDbContext _db;
    private readonly IWorkspaceService _workspaceService;

    public EfProjectService(ProjectsDbContext db, IWorkspaceService workspaceService)
    {
        _db = db;
        _workspaceService = workspaceService;
    }

    public async Task<ProjectDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var project = await _db.Projects
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (project is null) return null;

        var workspace = await _workspaceService.GetByIdAsync(project.WorkspaceId, cancellationToken);
        return MapToDto(project, workspace?.Name ?? "Unknown");
    }

    public async Task<IEnumerable<ProjectDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var projects = await _db.Projects
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        return await EnrichWithWorkspaceNames(projects, cancellationToken);
    }

    public async Task<IEnumerable<ProjectDto>> GetByWorkspaceAsync(int workspaceId, CancellationToken cancellationToken = default)
    {
        var projects = await _db.Projects
            .AsNoTracking()
            .Where(x => x.WorkspaceId == workspaceId && !x.IsArchived)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        var workspace = await _workspaceService.GetByIdAsync(workspaceId, cancellationToken);
        return projects.Select(p => MapToDto(p, workspace?.Name ?? "Unknown")).ToList();
    }

    public async Task<ProjectDto> CreateAsync(CreateProjectDto dto, CancellationToken cancellationToken = default)
    {
        var project = new Project
        {
            WorkspaceId = dto.WorkspaceId,
            Name = dto.Name,
            Description = dto.Description,
            Status = dto.Status,
            StartDate = dto.StartDate,
            DueDate = dto.DueDate,
            IsArchived = false,
            CreatedAt = DateTime.UtcNow
        };

        _db.Projects.Add(project);
        await _db.SaveChangesAsync(cancellationToken);

        var workspace = await _workspaceService.GetByIdAsync(project.WorkspaceId, cancellationToken);
        return MapToDto(project, workspace?.Name ?? "Unknown");
    }

    public async Task<ProjectDto> UpdateAsync(int id, UpdateProjectDto dto, CancellationToken cancellationToken = default)
    {
        var project = await _db.Projects.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (project is null)
            throw new InvalidOperationException($"Project {id} not found");

        project.Name = dto.Name;
        project.Description = dto.Description;
        project.Status = dto.Status;
        project.StartDate = dto.StartDate;
        project.DueDate = dto.DueDate;
        project.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        var workspace = await _workspaceService.GetByIdAsync(project.WorkspaceId, cancellationToken);
        return MapToDto(project, workspace?.Name ?? "Unknown");
    }

    public async Task ArchiveAsync(int id, CancellationToken cancellationToken = default)
    {
        var project = await _db.Projects.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (project is null)
            throw new InvalidOperationException($"Project {id} not found");

        project.IsArchived = true;
        project.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var project = await _db.Projects.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (project is null)
            throw new InvalidOperationException($"Project {id} not found");

        _db.Projects.Remove(project);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<List<ProjectDto>> EnrichWithWorkspaceNames(List<Project> projects, CancellationToken cancellationToken)
    {
        var workspaceIds = projects.Select(p => p.WorkspaceId).Distinct().ToList();
        var workspaces = new Dictionary<int, string>();

        foreach (var wsId in workspaceIds)
        {
            var ws = await _workspaceService.GetByIdAsync(wsId, cancellationToken);
            if (ws != null)
                workspaces[wsId] = ws.Name;
        }

        return projects.Select(p => MapToDto(p, workspaces.GetValueOrDefault(p.WorkspaceId, "Unknown"))).ToList();
    }

    private static ProjectDto MapToDto(Project project, string workspaceName) =>
        new(project.Id, project.WorkspaceId, workspaceName, project.Name, project.Description,
            project.Status, project.StartDate, project.DueDate, project.IsArchived,
            project.CreatedAt, project.UpdatedAt);
}
