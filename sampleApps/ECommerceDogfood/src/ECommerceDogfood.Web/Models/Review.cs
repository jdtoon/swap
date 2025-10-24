using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NetMX.Ddd.Domain.Entities;

namespace ECommerceDogfood.Web.Models;

/// <summary>
/// Review entity
/// </summary>
public class Review : AggregateRoot<Guid>
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
    private Review() { }

    /// <summary>
    /// Creates a new Review
    /// </summary>
    public Review(Guid id)
    {
        Id = id;
    }

}
