using SwapModularMonolith.Data.Configurations;
using SwapModularMonolith.Data.Seeding;
using Microsoft.EntityFrameworkCore;
//#if (IncludeSampleModule)
using SwapModularMonolith.Modules.Notes.Entities;
//#endif

namespace SwapModularMonolith.Data;

public class AppDbContext : DbContext
{
    private readonly IHttpContextAccessor? _httpContextAccessor;

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public AppDbContext(DbContextOptions<AppDbContext> options, IHttpContextAccessor httpContextAccessor)
        : base(options)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    #region DbSets

//#if (IncludeSampleModule)
    public DbSet<Note> Notes { get; set; }
//#endif

    #endregion

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditFields();
        return await base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyAppConfigurations();
        MasterDataSeeder.Seed(modelBuilder);
    }

    private void ApplyAuditFields()
    {
        var userId = _httpContextAccessor?.HttpContext?.User?
            .FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<IAuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    entry.Entity.CreatedByUserId = userId;
                    break;

                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    entry.Entity.ModifiedByUserId = userId;
                    break;
            }
        }
    }
}
