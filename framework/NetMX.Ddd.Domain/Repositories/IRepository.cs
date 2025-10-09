using NetMX.Ddd.Domain.Entities;

namespace NetMX.Ddd.Domain.Repositories;

public interface IRepository<TEntity, TKey> where TEntity : Entity<TKey>
{
    Task<TEntity> GetAsync(TKey id);
    Task<List<TEntity>> GetListAsync();
    Task<TEntity> InsertAsync(TEntity entity);
    Task<TEntity> UpdateAsync(TEntity entity);
    Task DeleteAsync(TKey id);
}