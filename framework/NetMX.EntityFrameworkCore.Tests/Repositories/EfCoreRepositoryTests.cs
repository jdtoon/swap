using Microsoft.EntityFrameworkCore;
using NetMX.Ddd.Domain;
using NetMX.Ddd.Domain.Entities;
using NetMX.EntityFrameworkCore.Repositories;
using Xunit;

namespace NetMX.EntityFrameworkCore.Tests.Repositories;

/// <summary>
/// Critical tests for EfCoreRepository - soft delete and audit fields
/// </summary>
public class EfCoreRepositoryTests : IDisposable
{
    private readonly TestDbContext _dbContext;
    private readonly EfCoreRepository<TestDbContext, TestEntity, Guid> _repository;

    public EfCoreRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new TestDbContext(options);
        _repository = new EfCoreRepository<TestDbContext, TestEntity, Guid>(_dbContext);
    }

    [Fact]
    public async Task InsertAsync_Should_SetCreationTime()
    {
        // Arrange
        var entity = new TestEntity(Guid.NewGuid(), "Test");

        // Act
        var result = await _repository.InsertAsync(entity);
        await _dbContext.SaveChangesAsync();

        // Assert
        Assert.NotEqual(default, result.CreatedAt);
        Assert.True(result.CreatedAt <= DateTime.UtcNow);
    }

    [Fact]
    public async Task UpdateAsync_Should_SetModificationTime()
    {
        // Arrange
        var entity = new TestEntity(Guid.NewGuid(), "Test");
        await _repository.InsertAsync(entity);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        var originalUpdatedAt = entity.UpdatedAt;

        // Act
        entity.Name = "Updated";
        await _repository.UpdateAsync(entity);
        await _dbContext.SaveChangesAsync();

        // Assert
        Assert.NotNull(entity.UpdatedAt);
        Assert.NotEqual(originalUpdatedAt, entity.UpdatedAt);
    }

    [Fact]
    public async Task DeleteAsync_Should_SoftDelete_WhenEntitySupportsSoftDelete()
    {
        // Arrange
        var entity = new TestEntity(Guid.NewGuid(), "Test");
        await _repository.InsertAsync(entity);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        // Act
        await _repository.DeleteAsync(entity.Id);
        await _dbContext.SaveChangesAsync();

        // Assert - Entity should be soft deleted, not removed
        var allEntities = await _dbContext.TestEntities.IgnoreQueryFilters().ToListAsync();
        Assert.Single(allEntities);
        Assert.True(allEntities[0].IsDeleted);
        Assert.NotNull(allEntities[0].DeletedAt);
    }

    [Fact]
    public async Task GetListAsync_Should_FilterSoftDeletedEntities()
    {
        // Arrange
        var entity1 = new TestEntity(Guid.NewGuid(), "Active");
        var entity2 = new TestEntity(Guid.NewGuid(), "Deleted");
        
        await _repository.InsertAsync(entity1);
        await _repository.InsertAsync(entity2);
        await _dbContext.SaveChangesAsync();
        
        _dbContext.ChangeTracker.Clear();
        
        // Soft delete entity2
        await _repository.DeleteAsync(entity2.Id);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        // Act
        var result = await _repository.GetListAsync();

        // Assert - Should only return active entity
        Assert.Single(result);
        Assert.Equal("Active", result[0].Name);
    }

    [Fact]
    public async Task GetAsync_Should_ReturnNull_ForSoftDeletedEntity()
    {
        // Arrange
        var entity = new TestEntity(Guid.NewGuid(), "Test");
        await _repository.InsertAsync(entity);
        await _dbContext.SaveChangesAsync();
        var entityId = entity.Id;
        
        _dbContext.ChangeTracker.Clear();
        
        // Soft delete
        await _repository.DeleteAsync(entityId);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        // Act
        var result = await _repository.GetAsync(entityId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetQueryableAsync_Should_FilterSoftDeletedEntities()
    {
        // Arrange
        var entity1 = new TestEntity(Guid.NewGuid(), "Active");
        var entity2 = new TestEntity(Guid.NewGuid(), "Deleted");
        
        await _repository.InsertAsync(entity1);
        await _repository.InsertAsync(entity2);
        await _dbContext.SaveChangesAsync();
        
        _dbContext.ChangeTracker.Clear();
        
        await _repository.DeleteAsync(entity2.Id);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        // Act
        var queryable = await _repository.GetQueryableAsync();
        var result = await queryable.ToListAsync();

        // Assert
        Assert.Single(result);
        Assert.Equal("Active", result[0].Name);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }
}

// Test entities
public class TestEntity : AggregateRoot<Guid>, ISoftDelete, IHasCreationTime, IHasModificationTime, IHasDeletionTime
{
    public string Name { get; set; } = string.Empty;
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    public TestEntity() { } // EF Core

    public TestEntity(Guid id, string name)
    {
        Id = id;
        Name = name;
    }
}

public class TestDbContext : DbContext
{
    public DbSet<TestEntity> TestEntities => Set<TestEntity>();

    public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<TestEntity>(b =>
        {
            b.ToTable("TestEntities");
            b.HasKey(x => x.Id);
            b.Property(x => x.Name).IsRequired().HasMaxLength(256);
            
            // Ignore domain events - they're not meant to be persisted
            b.Ignore(x => x.DomainEvents);
        });
    }
}
