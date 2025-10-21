using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NetMX.Ddd.Domain.Entities;

namespace ECommerce.Web.Models;

/// <summary>
/// Category entity
/// </summary>
public class Category : AggregateRoot<Guid>
{
    /// <summary>
    /// Created date (UTC)
    /// </summary>
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    /// <summary>
    /// Last updated date (UTC)
    /// </summary>
    public DateTime? UpdatedAt { get; private set; }

    /// <summary>
    /// Private constructor for EF Core
    /// </summary>
    private Category() { }

    /// <summary>
    /// Creates a new Category
    /// </summary>
    public Category(Guid id)
    {
        Id = id;
    }

}
