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
    protected readonly TDbContext _dbContext;

    public EfCoreRepository(TDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    protected virtual DbSet<TEntity> DbSet => _dbContext.Set<TEntity>();

    /// <summary>
    /// Gets a queryable for this repository with soft delete filtering applied.
    /// </summary>
    public virtual async Task<IQueryable<TEntity>> GetQueryableAsync()
    {
        return await Task.FromResult(ApplySoftDeleteFilter(DbSet.AsQueryable()));
    }

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

    public virtual async Task<List<TEntity>> GetListAsync()
    {
        return await ApplySoftDeleteFilter(DbSet).ToListAsync();
    }

    public virtual async Task<TEntity> InsertAsync(TEntity entity)
    {
        // Set creation audit fields
        SetCreationAuditProperties(entity);
        
        var entry = await DbSet.AddAsync(entity);
        return entry.Entity;
    }

    public virtual async Task<TEntity> UpdateAsync(TEntity entity)
    {
        // Set modification audit fields
        SetModificationAuditProperties(entity);
        
        _dbContext.Attach(entity);
        var updatedEntry = _dbContext.Update(entity);
        return updatedEntry.Entity;
    }

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