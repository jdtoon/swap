namespace NetMX.Ddd.Application.Dtos;

public abstract class EntityDto<TKey>
{
    public TKey Id { get; set; }
}