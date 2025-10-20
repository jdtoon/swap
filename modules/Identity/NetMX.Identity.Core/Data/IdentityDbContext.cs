using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NetMX.Identity.Core.Users;

namespace NetMX.Identity.Core.Data;

/// <summary>
/// Database context for the NetMX Identity module.
/// Inherits from IdentityDbContext to get ASP.NET Core Identity tables and functionality.
/// </summary>
public class IdentityDbContext : IdentityDbContext<
    AppUser,           // TUser
    AppRole,           // TRole
    Guid,              // TKey
    UserClaim,         // TUserClaim
    UserRole,          // TUserRole
    IdentityUserLogin<Guid>,   // TUserLogin (using built-in)
    RoleClaim,         // TRoleClaim
    IdentityUserToken<Guid>>   // TUserToken (using built-in)
{
    public IdentityDbContext(DbContextOptions<IdentityDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Configure Identity tables with custom prefix
        builder.Entity<AppUser>(b =>
        {
            b.ToTable("Users");
            // Custom properties are already configured via conventions
            b.Property(u => u.FirstName).HasMaxLength(256);
            b.Property(u => u.LastName).HasMaxLength(256);
            b.Property(u => u.TenantId).IsRequired(false);
            b.Property(u => u.IsActive).IsRequired().HasDefaultValue(true);
        });

        builder.Entity<AppRole>(b =>
        {
            b.ToTable("Roles");
            b.Property(r => r.Description).HasMaxLength(512);
            b.Property(r => r.IsSystemRole).IsRequired().HasDefaultValue(false);
            b.Property(r => r.TenantId).IsRequired(false);
        });

        builder.Entity<UserRole>(b =>
        {
            b.ToTable("UserRoles");
            b.HasOne(ur => ur.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(ur => ur.UserId)
                .IsRequired();

            b.HasOne(ur => ur.Role)
                .WithMany()
                .HasForeignKey(ur => ur.RoleId)
                .IsRequired();
        });

        builder.Entity<UserClaim>(b =>
        {
            b.ToTable("UserClaims");
            b.HasOne(uc => uc.User)
                .WithMany(u => u.Claims)
                .HasForeignKey(uc => uc.UserId)
                .IsRequired();
        });

        builder.Entity<RoleClaim>(b =>
        {
            b.ToTable("RoleClaims");
            b.HasOne(rc => rc.Role)
                .WithMany(r => r.Claims)
                .HasForeignKey(rc => rc.RoleId)
                .IsRequired();
        });

        builder.Entity<IdentityUserLogin<Guid>>(b =>
        {
            b.ToTable("UserLogins");
        });

        builder.Entity<IdentityUserToken<Guid>>(b =>
        {
            b.ToTable("UserTokens");
        });

        // Multi-tenancy filtering can be added here if needed
        // Example: builder.Entity<AppUser>().HasQueryFilter(u => u.TenantId == _currentTenant.Id);
    }
}
