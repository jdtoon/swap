using Audit.Contracts.Dtos;

namespace Audit.Contracts.Services;

public interface IAuditLogService
{
    Task<List<AuditLogDto>> GetAllAsync();
    Task<AuditLogDto?> GetByIdAsync(Guid id);
    Task<AuditLogDto> CreateAsync(CreateAuditLogDto dto);
    Task<AuditLogDto> UpdateAsync(UpdateAuditLogDto dto);
    Task DeleteAsync(Guid id);
}