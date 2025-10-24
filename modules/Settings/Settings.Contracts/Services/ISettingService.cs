namespace Settings.Contracts.Services;

using Settings.Contracts.Dtos;

/// <summary>
/// Service interface for Setting operations
/// </summary>
public interface ISettingService
{
    /// <summary>
    /// Gets all Settings
    /// </summary>
    Task<List<SettingDto>> GetAllAsync();

    /// <summary>
    /// Gets Setting by ID
    /// </summary>
    Task<SettingDto?> GetByIdAsync(Guid id);

    /// <summary>
    /// Creates a new Setting
    /// </summary>
    Task<SettingDto> CreateAsync(CreateSettingDto dto);

    /// <summary>
    /// Updates an existing Setting
    /// </summary>
    Task<SettingDto> UpdateAsync(UpdateSettingDto dto);

    /// <summary>
    /// Deletes Setting by ID
    /// </summary>
    Task DeleteAsync(Guid id);
}
