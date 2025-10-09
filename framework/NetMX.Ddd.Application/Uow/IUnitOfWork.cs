namespace NetMX.Ddd.Application.Uow;

public interface IUnitOfWork : IDisposable
{
    Guid Id { get; }
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
    Task CompleteAsync(CancellationToken cancellationToken = default);
    Task RollbackAsync(CancellationToken cancellationToken = default);
}