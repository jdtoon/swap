using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NetMX.Ddd.Domain.Entities;

namespace Settings.Core.Entities;

/// <summary>
/// Setting entity
/// </summary>
public class Setting : AggregateRoot<Guid>
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
    private Setting() { }

    /// <summary>
    /// Creates a new Setting
    /// </summary>
    public Setting(Guid id)
    {
        Id = id;
    }

}
