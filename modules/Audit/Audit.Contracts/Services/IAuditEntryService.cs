using Audit.Contracts.Dtos;

namespace Audit.Contracts.Services;

public interface IAuditEntryService
{
    Task<List<AuditEntryDto>> GetAllAsync();
    Task<AuditEntryDto?> GetByIdAsync(Guid id);
    Task<AuditEntryDto> CreateAsync(CreateAuditEntryDto dto);
    Task<AuditEntryDto> UpdateAsync(UpdateAuditEntryDto dto);
    Task DeleteAsync(Guid id);
}