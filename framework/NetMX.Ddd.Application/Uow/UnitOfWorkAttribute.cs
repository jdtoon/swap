namespace NetMX.Ddd.Application.Uow;

/// <summary>
/// Attribute to mark methods or classes that should be wrapped in a unit of work.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Interface)]
public class UnitOfWorkAttribute : Attribute
{
    /// <summary>
    /// Gets or sets a value indicating whether the unit of work should be transactional.
    /// </summary>
    public bool IsTransactional { get; set; } = true;
    
    /// <summary>
    /// Gets or sets a value indicating whether the unit of work is disabled for this method/class.
    /// </summary>
    public bool IsDisabled { get; set; }
}