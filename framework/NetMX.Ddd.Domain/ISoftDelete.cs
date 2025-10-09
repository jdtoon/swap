namespace NetMX.Ddd.Domain;

public interface ISoftDelete
{
    bool IsDeleted { get; }
}