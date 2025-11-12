using Microsoft.EntityFrameworkCore;
using ProjectHub.Modules.Workspaces.Contracts;
using ProjectHub.Modules.Workspaces.Module.Domain;
using ProjectHub.Modules.Workspaces.Module.Persistence;

namespace ProjectHub.Modules.Workspaces.Module.Services;

internal sealed class EfWorkspaceService : IWorkspaceService
{
    private readonly WorkspacesDbContext _db;

    public EfWorkspaceService(WorkspacesDbContext db)
    {
        _db = db;
    }

    public async Task<WorkspaceDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var workspace = await _db.Workspaces
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return workspace is null ? null : MapToDto(workspace);
    }

    public async Task<IEnumerable<WorkspaceDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Workspaces
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => MapToDto(x))
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<WorkspaceDto>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Workspaces
            .AsNoTracking()
            .Where(x => !x.IsArchived)
            .OrderBy(x => x.Name)
            .Select(x => MapToDto(x))
            .ToListAsync(cancellationToken);
    }

    public async Task<WorkspaceDto> CreateAsync(CreateWorkspaceDto dto, CancellationToken cancellationToken = default)
    {
        var workspace = new Workspace
        {
            Name = dto.Name,
            Description = dto.Description,
            Color = dto.Color,
            IsArchived = false,
            CreatedAt = DateTime.UtcNow
        };

        _db.Workspaces.Add(workspace);
        await _db.SaveChangesAsync(cancellationToken);

        return MapToDto(workspace);
    }

    public async Task<WorkspaceDto> UpdateAsync(int id, UpdateWorkspaceDto dto, CancellationToken cancellationToken = default)
    {
        var workspace = await _db.Workspaces.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (workspace is null)
            throw new InvalidOperationException($"Workspace {id} not found");

        workspace.Name = dto.Name;
        workspace.Description = dto.Description;
        workspace.Color = dto.Color;
        workspace.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        return MapToDto(workspace);
    }

    public async Task ArchiveAsync(int id, CancellationToken cancellationToken = default)
    {
        var workspace = await _db.Workspaces.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (workspace is null)
            throw new InvalidOperationException($"Workspace {id} not found");

        workspace.IsArchived = true;
        workspace.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task UnarchiveAsync(int id, CancellationToken cancellationToken = default)
    {
        var workspace = await _db.Workspaces.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (workspace is null)
            throw new InvalidOperationException($"Workspace {id} not found");

        workspace.IsArchived = false;
        workspace.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
    }

    private static WorkspaceDto MapToDto(Workspace workspace) =>
        new(workspace.Id, workspace.Name, workspace.Description, workspace.Color, 
            workspace.IsArchived, workspace.CreatedAt, workspace.UpdatedAt);
}
