using NetMX.Ddd.Domain.Entities;

namespace NetMX.Ddd.Domain.Repositories;

/// <summary>
/// Standard repository interface for basic CRUD operations on entities.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <typeparam name="TKey">The type of the entity's identifier.</typeparam>
public interface IRepository<TEntity, TKey> where TEntity : Entity<TKey>
{
    /// <summary>
    /// Gets an entity by its identifier.
    /// </summary>
    /// <param name="id">The entity identifier.</param>
    /// <returns>The entity with the specified identifier.</returns>
    Task<TEntity> GetAsync(TKey id);
    
    /// <summary>
    /// Gets all entities of the specified type.
    /// </summary>
    /// <returns>A list of all entities.</returns>
    Task<List<TEntity>> GetListAsync();
    
    /// <summary>
    /// Inserts a new entity.
    /// </summary>
    /// <param name="entity">The entity to insert.</param>
    /// <returns>The inserted entity.</returns>
    Task<TEntity> InsertAsync(TEntity entity);
    
    /// <summary>
    /// Updates an existing entity.
    /// </summary>
    /// <param name="entity">The entity to update.</param>
    /// <returns>The updated entity.</returns>
    Task<TEntity> UpdateAsync(TEntity entity);
    
    /// <summary>
    /// Deletes an entity by its identifier.
    /// </summary>
    /// <param name="id">The identifier of the entity to delete.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DeleteAsync(TKey id);
}