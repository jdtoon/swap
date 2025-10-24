namespace NetMX.Ddd.Application.Uow;

/// <summary>
/// Represents a unit of work pattern for managing database transactions.
/// </summary>
public interface IUnitOfWork : IDisposable
{
    /// <summary>
    /// Gets the unique identifier for this unit of work instance.
    /// </summary>
    Guid Id { get; }
    
    /// <summary>
    /// Saves changes made within this unit of work to the database.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Completes the unit of work by committing the transaction.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task CompleteAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Rolls back all changes made within this unit of work.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RollbackAsync(CancellationToken cancellationToken = default);
}