using System.ComponentModel.DataAnnotations;

namespace ECommerce.Web.Dtos;

/// <summary>
/// Order read DTO
/// </summary>
public class OrderDto
{
    public Guid Id { get; set; }


    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
