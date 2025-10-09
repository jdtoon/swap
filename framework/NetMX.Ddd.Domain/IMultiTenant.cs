namespace NetMX.Ddd.Domain;

public interface IMultiTenant
{
    Guid TenantId { get; }
}