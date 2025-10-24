using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NetMX.Ddd.Domain.Entities;

namespace ECommerceDogfood.Web.Models;

/// <summary>
/// Product entity
/// </summary>
public class Product : AggregateRoot<Guid>
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
    private Product() { }

    /// <summary>
    /// Creates a new Product
    /// </summary>
    public Product(Guid id)
    {
        Id = id;
    }

}
