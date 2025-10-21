using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NetMX.Ddd.Domain.Entities;

namespace ECommerce.Web.Models;

/// <summary>
/// Order entity
/// </summary>
public class Order : AggregateRoot<Guid>
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
    private Order() { }

    /// <summary>
    /// Creates a new Order
    /// </summary>
    public Order(Guid id)
    {
        Id = id;
    }

}
