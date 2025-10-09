namespace NetMX.Ddd.Application.Uow;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Interface)]
public class UnitOfWorkAttribute : Attribute
{
    public bool IsTransactional { get; set; } = true;
    public bool IsDisabled { get; set; }
}