using NetMX.Ddd.Domain.Events;
using NetMX.DependencyInjection;

namespace NetMX.Ddd.Application.Uow;

/// <summary>
/// Default implementation of IUnitOfWorkManager.
/// Manages the current unit of work using AsyncLocal for async flow.
/// </summary>
public class UnitOfWorkManager : IUnitOfWorkManager, IScopedDependency
{
    private static readonly AsyncLocal<IUnitOfWork?> _current = new();
    private readonly IDomainEventDispatcher? _eventDispatcher;

    public UnitOfWorkManager(IDomainEventDispatcher? eventDispatcher = null)
    {
        _eventDispatcher = eventDispatcher;
    }

    /// <inheritdoc />
    public IUnitOfWork? Current => _current.Value;

    /// <inheritdoc />
    public IUnitOfWork Begin(bool requiresNew = false, bool isTransactional = true)
    {
        // If there's already a UoW and we don't require a new one, return the current
        if (!requiresNew && _current.Value != null)
        {
            return _current.Value;
        }

        var uow = new UnitOfWork(_eventDispatcher);
        
        // Save previous UoW
        var previousUow = _current.Value;
        
        // Set as current UoW
        _current.Value = uow;

        // Register cleanup on dispose
        var originalDispose = new Action(() =>
        {
            // Restore previous UoW when this one is disposed
            if (_current.Value == uow)
            {
                _current.Value = previousUow;
            }
        });

        // Hook into the disposal
        uow.OnCompleted(() =>
        {
            // Keep current until disposed
            return Task.CompletedTask;
        });

        // We need a way to call originalDispose when UoW is disposed
        // For now, let's use a different approach - expose a Dispose event
        uow.OnDisposed(originalDispose);

        return uow;
    }
}
