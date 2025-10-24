namespace NetMX.Ddd.Application.Uow;

/// <summary>
/// Manages the lifecycle of unit of work instances.
/// </summary>
public interface IUnitOfWorkManager
{
    /// <summary>
    /// Gets the current active unit of work, if any.
    /// </summary>
    IUnitOfWork? Current { get; }
    
    /// <summary>
    /// Begins a new unit of work.
    /// </summary>
    /// <param name="requiresNew">If true, always creates a new unit of work even if one already exists.</param>
    /// <param name="isTransactional">If true, the unit of work will be transactional.</param>
    /// <returns>The newly created or existing unit of work.</returns>
    IUnitOfWork Begin(bool requiresNew = false, bool isTransactional = true);
}