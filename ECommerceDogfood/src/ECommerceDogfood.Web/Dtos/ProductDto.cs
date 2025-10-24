using System.ComponentModel.DataAnnotations;

namespace ECommerceDogfood.Web.Dtos;

/// <summary>
/// Product read DTO
/// </summary>
public class ProductDto
{
    public Guid Id { get; set; }


    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
