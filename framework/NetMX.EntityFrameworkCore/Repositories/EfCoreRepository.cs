using Microsoft.EntityFrameworkCore;
using NetMX.Ddd.Domain.Entities;
using NetMX.Ddd.Domain.Repositories;

namespace NetMX.EntityFrameworkCore.Repositories;

public class EfCoreRepository<TDbContext, TEntity, TKey> : IRepository<TEntity, TKey>
    where TDbContext : DbContext
    where TEntity : Entity<TKey>
{
    protected readonly TDbContext _dbContext;

    public EfCoreRepository(TDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    protected virtual DbSet<TEntity> DbSet => _dbContext.Set<TEntity>();

    public virtual async Task<TEntity> GetAsync(TKey id)
    {
        return await DbSet.FindAsync(id);
    }

    public virtual async Task<List<TEntity>> GetListAsync()
    {
        return await DbSet.ToListAsync();
    }

    public virtual async Task<TEntity> InsertAsync(TEntity entity)
    {
        var entry = await DbSet.AddAsync(entity);
        return entry.Entity;
    }

    public virtual async Task<TEntity> UpdateAsync(TEntity entity)
    {
        _dbContext.Attach(entity);
        var updatedEntry = _dbContext.Update(entity);
        return updatedEntry.Entity;
    }

    public virtual async Task DeleteAsync(TKey id)
    {
        var entity = await GetAsync(id);
        if (entity != null)
        {
            DbSet.Remove(entity);
        }
    }
}