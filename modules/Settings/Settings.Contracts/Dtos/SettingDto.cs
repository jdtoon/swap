using System.ComponentModel.DataAnnotations;

namespace Settings.Contracts.Dtos;

/// <summary>
/// Setting read DTO
/// </summary>
public class SettingDto
{
    public Guid Id { get; set; }


    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
