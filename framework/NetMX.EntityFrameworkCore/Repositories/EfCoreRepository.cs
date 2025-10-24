using Microsoft.EntityFrameworkCore;
using NetMX.Ddd.Domain;
using NetMX.Ddd.Domain.Entities;
using NetMX.Ddd.Domain.Repositories;

namespace NetMX.EntityFrameworkCore.Repositories;

/// <summary>
/// Base repository implementation using Entity Framework Core.
/// Provides automatic soft delete filtering and audit field population.
/// </summary>
public class EfCoreRepository<TDbContext, TEntity, TKey> : IQueryableRepository<TEntity, TKey>
    where TDbContext : DbContext
    where TEntity : Entity<TKey>
{
    /// <summary>
    /// The database context.
    /// </summary>
    protected readonly TDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="EfCoreRepository{TDbContext, TEntity, TKey}"/> class.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    public EfCoreRepository(TDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Gets the DbSet for the entity.
    /// </summary>
    protected virtual DbSet<TEntity> DbSet => _dbContext.Set<TEntity>();

    /// <summary>
    /// Gets a queryable for this repository with soft delete filtering applied.
    /// </summary>
    public virtual async Task<IQueryable<TEntity>> GetQueryableAsync()
    {
        return await Task.FromResult(ApplySoftDeleteFilter(DbSet.AsQueryable()));
    }

    /// <summary>
    /// Gets an entity by its identifier.
    /// </summary>
    /// <param name="id">The entity identifier.</param>
    /// <returns>The entity, or null if not found or soft deleted.</returns>
    public virtual async Task<TEntity> GetAsync(TKey id)
    {
        var entity = await DbSet.FindAsync(id);
        
        // Check if soft deleted
        if (entity is ISoftDelete softDelete && softDelete.IsDeleted)
        {
            return null!;
        }
        
        return entity!;
    }

    /// <summary>
    /// Gets all entities with soft delete filtering applied.
    /// </summary>
    /// <returns>A list of all non-deleted entities.</returns>
    public virtual async Task<List<TEntity>> GetListAsync()
    {
        return await ApplySoftDeleteFilter(DbSet).ToListAsync();
    }

    /// <summary>
    /// Inserts a new entity, setting creation audit properties.
    /// </summary>
    /// <param name="entity">The entity to insert.</param>
    /// <returns>The inserted entity.</returns>
    public virtual async Task<TEntity> InsertAsync(TEntity entity)
    {
        // Set creation audit fields
        SetCreationAuditProperties(entity);
        
        var entry = await DbSet.AddAsync(entity);
        return entry.Entity;
    }

    /// <summary>
    /// Updates an existing entity, setting modification audit properties.
    /// </summary>
    /// <param name="entity">The entity to update.</param>
    /// <returns>The updated entity.</returns>
    public virtual async Task<TEntity> UpdateAsync(TEntity entity)
    {
        // Set modification audit fields
        SetModificationAuditProperties(entity);
        
        _dbContext.Attach(entity);
        var updatedEntry = _dbContext.Update(entity);
        return updatedEntry.Entity;
    }

    /// <summary>
    /// Deletes an entity (soft delete if supported, otherwise hard delete).
    /// </summary>
    /// <param name="id">The identifier of the entity to delete.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public virtual async Task DeleteAsync(TKey id)
    {
        var entity = await DbSet.FindAsync(id);
        if (entity != null)
        {
            // Soft delete if supported
            if (entity is ISoftDelete softDelete)
            {
                softDelete.IsDeleted = true;
                
                if (entity is IHasDeletionTime deletionTime)
                {
                    deletionTime.DeletedAt = DateTime.UtcNow;
                }
                
                _dbContext.Update(entity);
            }
            else
            {
                // Hard delete
                DbSet.Remove(entity);
            }
        }
    }

    /// <summary>
    /// Applies soft delete filter to the queryable.
    /// </summary>
    protected virtual IQueryable<TEntity> ApplySoftDeleteFilter(IQueryable<TEntity> query)
    {
        if (typeof(ISoftDelete).IsAssignableFrom(typeof(TEntity)))
        {
            return query.Where(e => !((ISoftDelete)e).IsDeleted);
        }
        
        return query;
    }

    /// <summary>
    /// Sets creation audit properties.
    /// </summary>
    protected virtual void SetCreationAuditProperties(TEntity entity)
    {
        if (entity is IHasCreationTime creationTime)
        {
            if (creationTime.CreatedAt == default)
            {
                creationTime.CreatedAt = DateTime.UtcNow;
            }
        }
    }

    /// <summary>
    /// Sets modification audit properties.
    /// </summary>
    protected virtual void SetModificationAuditProperties(TEntity entity)
    {
        if (entity is IHasModificationTime modificationTime)
        {
            modificationTime.UpdatedAt = DateTime.UtcNow;
        }
    }
}