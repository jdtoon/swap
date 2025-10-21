using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NetMX.Ddd.Domain.Entities;

namespace ECommerce.Web.Models;

/// <summary>
/// TestEntity entity
/// </summary>
public class TestEntity : AggregateRoot<Guid>
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
    private TestEntity() { }

    /// <summary>
    /// Creates a new TestEntity
    /// </summary>
    public TestEntity(Guid id)
    {
        Id = id;
    }

}
