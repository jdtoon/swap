using NetMX.Ddd.Domain.Entities;

namespace NetMX.Ddd.Domain.Repositories;

/// <summary>
/// Repository interface that provides LINQ query capabilities for entities.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <typeparam name="TKey">The type of the entity's identifier.</typeparam>
public interface IQueryableRepository<TEntity, TKey> : IRepository<TEntity, TKey> 
    where TEntity : Entity<TKey>
{
    /// <summary>
    /// Gets an IQueryable for executing LINQ queries against the entity set.
    /// </summary>
    /// <returns>An IQueryable that can be used to construct LINQ queries.</returns>
    Task<IQueryable<TEntity>> GetQueryableAsync();
}
