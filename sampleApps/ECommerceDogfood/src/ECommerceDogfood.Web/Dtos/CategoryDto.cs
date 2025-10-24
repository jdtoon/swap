using System.ComponentModel.DataAnnotations;

namespace ECommerceDogfood.Web.Dtos;

/// <summary>
/// Category read DTO
/// </summary>
public class CategoryDto
{
    public Guid Id { get; set; }


    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
