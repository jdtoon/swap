using Microsoft.EntityFrameworkCore;
using NetMX.Ddd.Domain;
using NetMX.Ddd.Domain.Events;
using System.Linq.Expressions;
using System.Reflection;

namespace NetMX.EntityFrameworkCore;

/// <summary>
/// Base DbContext for NetMX applications providing common functionality like soft delete filtering.
/// </summary>
/// <typeparam name="TDbContext">The concrete DbContext type.</typeparam>
public abstract class NetMXDbContext<TDbContext> : DbContext where TDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NetMXDbContext{TDbContext}"/> class.
    /// </summary>
    /// <param name="options">The options for this context.</param>
    protected NetMXDbContext(DbContextOptions<TDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// Configures the model by applying global filters like soft delete.
    /// </summary>
    /// <param name="modelBuilder">The builder being used to construct the model for this context.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Ignore DomainEvent base class (not a database entity)
        modelBuilder.Ignore<DomainEvent>();

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            ConfigureGlobalFilters(entityType.ClrType, modelBuilder);
        }
    }

    /// <summary>
    /// Configures global filters for an entity type, such as soft delete filtering.
    /// </summary>
    /// <param name="entityType">The entity type to configure filters for.</param>
    /// <param name="modelBuilder">The model builder.</param>
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