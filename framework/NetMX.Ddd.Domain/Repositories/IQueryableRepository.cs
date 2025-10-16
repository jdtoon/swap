using NetMX.Ddd.Domain.Entities;

namespace NetMX.Ddd.Domain.Repositories;

public interface IQueryableRepository<TEntity, TKey> : IRepository<TEntity, TKey> 
    where TEntity : Entity<TKey>
{
    Task<IQueryable<TEntity>> GetQueryableAsync();
}
