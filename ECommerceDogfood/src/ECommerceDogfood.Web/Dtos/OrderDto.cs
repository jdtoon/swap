using System.ComponentModel.DataAnnotations;

namespace ECommerceDogfood.Web.Dtos;

/// <summary>
/// Order read DTO
/// </summary>
public class OrderDto
{
    public Guid Id { get; set; }


    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
