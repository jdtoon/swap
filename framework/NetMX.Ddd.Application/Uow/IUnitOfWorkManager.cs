namespace NetMX.Ddd.Application.Uow;

public interface IUnitOfWorkManager
{
    IUnitOfWork? Current { get; }
    IUnitOfWork Begin(bool requiresNew = false, bool isTransactional = true);
}