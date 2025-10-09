using Microsoft.EntityFrameworkCore;
using NetMX.Ddd.Domain;
using System.Linq.Expressions;
using System.Reflection;

namespace NetMX.EntityFrameworkCore;

public abstract class NetMXDbContext<TDbContext> : DbContext where TDbContext : DbContext
{
    protected NetMXDbContext(DbContextOptions<TDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            ConfigureGlobalFilters(entityType.ClrType, modelBuilder);
        }
    }

    protected virtual void ConfigureGlobalFilters(Type entityType, ModelBuilder modelBuilder)
    {
        if (typeof(ISoftDelete).IsAssignableFrom(entityType))
        {
            var parameter = Expression.Parameter(entityType, "e");
            var property = Expression.Property(parameter, nameof(ISoftDelete.IsDeleted));
            var filter = Expression.Lambda(Expression.Equal(property, Expression.Constant(false)), parameter);
            modelBuilder.Entity(entityType).HasQueryFilter(filter);
        }

        // We will add the IMultiTenant filter here once we build the tenancy infrastructure
    }
}