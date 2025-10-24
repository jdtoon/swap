using System.ComponentModel.DataAnnotations;

namespace ECommerceDogfood.Web.Dtos;

/// <summary>
/// Review read DTO
/// </summary>
public class ReviewDto
{
    public Guid Id { get; set; }


    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
