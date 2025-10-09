namespace NetMX.Ddd.Domain;

public interface IHasConcurrencyStamp
{
    string ConcurrencyStamp { get; set; }
}