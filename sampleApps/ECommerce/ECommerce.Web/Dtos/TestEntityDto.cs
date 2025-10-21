using System.ComponentModel.DataAnnotations;

namespace ECommerce.Web.Dtos;

/// <summary>
/// TestEntity read DTO
/// </summary>
public class TestEntityDto
{
    public Guid Id { get; set; }


    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
