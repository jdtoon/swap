using NetMX.Ddd.Domain.Events;

namespace NetMX.Ddd.Application.Uow;

/// <summary>
/// Default implementation of IUnitOfWork.
/// Manages transaction lifecycle and completion callbacks.
/// Dispatches domain events on successful completion.
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly List<Func<Task>> _completedCallbacks = new();
    private readonly List<Action> _disposedCallbacks = new();
    private readonly List<DomainEvent> _domainEvents = new();
    private readonly IDomainEventDispatcher? _eventDispatcher;
    private bool _isCompleted;
    private bool _isDisposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="UnitOfWork"/> class.
    /// </summary>
    /// <param name="eventDispatcher">Optional event dispatcher for domain events.</param>
    public UnitOfWork(IDomainEventDispatcher? eventDispatcher = null)
    {
        _eventDispatcher = eventDispatcher;
    }

    /// <inheritdoc />
    public Guid Id { get; } = Guid.NewGuid();

    /// <summary>
    /// Gets a value indicating whether this unit of work has been completed.
    /// </summary>
    public bool IsCompleted => _isCompleted;
    
    /// <summary>
    /// Gets a value indicating whether this unit of work has been disposed.
    /// </summary>
    public bool IsDisposed => _isDisposed;

    /// <summary>
    /// For testing: Hook to execute custom save logic.
    /// </summary>
    public Func<Task>? OnSaveChanges { get; set; }

    /// <summary>
    /// Adds domain events to be dispatched when the UoW completes.
    /// </summary>
    public void AddDomainEvent(DomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    /// <summary>
    /// Adds domain events to be dispatched when the UoW completes.
    /// </summary>
    public void AddDomainEvents(IEnumerable<DomainEvent> domainEvents)
    {
        _domainEvents.AddRange(domainEvents);
    }

    /// <inheritdoc />
    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        if (_isDisposed)
        {
            throw new ObjectDisposedException(nameof(UnitOfWork));
        }

        // Execute save changes (database commit)
        if (OnSaveChanges != null)
        {
            await OnSaveChanges();
        }
    }

    /// <inheritdoc />
    public async Task CompleteAsync(CancellationToken cancellationToken = default)
    {
        if (_isCompleted)
        {
            throw new InvalidOperationException("This unit of work has already been completed.");
        }

        if (_isDisposed)
        {
            throw new ObjectDisposedException(nameof(UnitOfWork));
        }

        // Save changes first
        await SaveChangesAsync(cancellationToken);

        _isCompleted = true;

        // Dispatch domain events AFTER successful save
        if (_eventDispatcher != null && _domainEvents.Any())
        {
            await _eventDispatcher.DispatchAsync(_domainEvents, cancellationToken);
        }

        // Execute completion callbacks
        foreach (var callback in _completedCallbacks)
        {
            await callback();
        }
    }

    /// <inheritdoc />
    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        if (_isDisposed)
        {
            throw new ObjectDisposedException(nameof(UnitOfWork));
        }

        // Rollback - just mark as disposed without saving
        await Task.CompletedTask;
    }

    /// <summary>
    /// Registers a callback to be executed when the unit of work is completed successfully.
    /// </summary>
    public void OnCompleted(Func<Task> callback)
    {
        _completedCallbacks.Add(callback);
    }

    /// <summary>
    /// Registers a callback to be executed when the unit of work is disposed.
    /// </summary>
    public void OnDisposed(Action callback)
    {
        _disposedCallbacks.Add(callback);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;

        // Execute disposal callbacks
        foreach (var callback in _disposedCallbacks)
        {
            callback();
        }

        // If not completed, this is a rollback scenario
        // No need to execute completion callbacks or dispatch events
    }
}
