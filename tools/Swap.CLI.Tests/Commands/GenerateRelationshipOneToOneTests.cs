using Xunit;

namespace Swap.CLI.Tests.Commands;

public class GenerateRelationshipOneToOneTests : IDisposable
{
    private readonly string _testProjectPath;

    public GenerateRelationshipOneToOneTests()
    {
        _testProjectPath = Path.Combine(Path.GetTempPath(), $"SwapTest_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testProjectPath);
        Directory.CreateDirectory(Path.Combine(_testProjectPath, "Models"));
        Directory.CreateDirectory(Path.Combine(_testProjectPath, "Data"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_testProjectPath))
        {
            Directory.Delete(_testProjectPath, recursive: true);
        }
    }

    [Fact]
    public async Task GenerateOneToOne_CreatesDependentWithForeignKeyAndNavigation()
    {
        // Arrange
        var userPath = Path.Combine(_testProjectPath, "Models", "User.cs");
        var profilePath = Path.Combine(_testProjectPath, "Models", "Profile.cs");
        
        await File.WriteAllTextAsync(userPath, @"namespace Models;

public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
}
");
        
        await File.WriteAllTextAsync(profilePath, @"namespace Models;

public class Profile
{
    public int Id { get; set; }
    public string Bio { get; set; } = string.Empty;
}
");

        var definition = new Swap.CLI.Commands.Relationships.Models.RelationshipDefinition
        {
            SourceEntity = "Profile",
            TargetEntity = "User",
            Type = Swap.CLI.Commands.Relationships.Models.RelationshipType.OneToOne,
            IsRequired = true,
            ProjectPath = _testProjectPath,
            SkipNavigation = false,
            OnDelete = Swap.CLI.Commands.Relationships.Models.DeleteBehavior.Restrict
        };

        // Act - Profile is dependent (holds FK to User)
        var updatedProfile = await Swap.CLI.Commands.Relationships.EntityModifier.AddOneToOnePropertiesAsync(
            profilePath, "Profile", "User", definition, isDependent: true);
        
        // Assert
        Assert.Contains("public int UserId { get; set; }", updatedProfile);
        Assert.Contains("public User? User { get; set; }", updatedProfile);
    }

    [Fact]
    public async Task GenerateOneToOne_CreatesPrincipalWithNavigationOnly()
    {
        // Arrange
        var userPath = Path.Combine(_testProjectPath, "Models", "User.cs");
        var profilePath = Path.Combine(_testProjectPath, "Models", "Profile.cs");
        
        await File.WriteAllTextAsync(userPath, @"namespace Models;

public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
}
");
        
        await File.WriteAllTextAsync(profilePath, @"namespace Models;

public class Profile
{
    public int Id { get; set; }
    public string Bio { get; set; } = string.Empty;
}
");

        var definition = new Swap.CLI.Commands.Relationships.Models.RelationshipDefinition
        {
            SourceEntity = "Profile",
            TargetEntity = "User",
            Type = Swap.CLI.Commands.Relationships.Models.RelationshipType.OneToOne,
            IsRequired = true,
            ProjectPath = _testProjectPath,
            SkipNavigation = false,
            OnDelete = Swap.CLI.Commands.Relationships.Models.DeleteBehavior.Restrict
        };

        // Act - User is principal (no FK, just navigation)
        var updatedUser = await Swap.CLI.Commands.Relationships.EntityModifier.AddOneToOnePropertiesAsync(
            userPath, "User", "Profile", definition, isDependent: false);
        
        // Assert
        Assert.Contains("public Profile? Profile { get; set; }", updatedUser);
        Assert.DoesNotContain("ProfileId", updatedUser);
    }

    [Fact]
    public async Task GenerateOneToOne_OptionalForeignKey_CreatesNullableFK()
    {
        // Arrange
        var userPath = Path.Combine(_testProjectPath, "Models", "User.cs");
        var profilePath = Path.Combine(_testProjectPath, "Models", "Profile.cs");
        
        await File.WriteAllTextAsync(userPath, @"namespace Models;

public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
}
");
        
        await File.WriteAllTextAsync(profilePath, @"namespace Models;

public class Profile
{
    public int Id { get; set; }
    public string Bio { get; set; } = string.Empty;
}
");

        var definition = new Swap.CLI.Commands.Relationships.Models.RelationshipDefinition
        {
            SourceEntity = "Profile",
            TargetEntity = "User",
            Type = Swap.CLI.Commands.Relationships.Models.RelationshipType.OneToOne,
            IsRequired = false, // Optional
            ProjectPath = _testProjectPath,
            SkipNavigation = false,
            OnDelete = Swap.CLI.Commands.Relationships.Models.DeleteBehavior.Restrict
        };

        // Act
        var updatedProfile = await Swap.CLI.Commands.Relationships.EntityModifier.AddOneToOnePropertiesAsync(
            profilePath, "Profile", "User", definition, isDependent: true);
        
        // Assert
        Assert.Contains("public int? UserId { get; set; }", updatedProfile);
    }

    [Fact]
    public async Task GenerateOneToOne_CustomForeignKeyName()
    {
        // Arrange
        var userPath = Path.Combine(_testProjectPath, "Models", "User.cs");
        var profilePath = Path.Combine(_testProjectPath, "Models", "Profile.cs");
        
        await File.WriteAllTextAsync(userPath, @"namespace Models;

public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
}
");
        
        await File.WriteAllTextAsync(profilePath, @"namespace Models;

public class Profile
{
    public int Id { get; set; }
    public string Bio { get; set; } = string.Empty;
}
");

        var definition = new Swap.CLI.Commands.Relationships.Models.RelationshipDefinition
        {
            SourceEntity = "Profile",
            TargetEntity = "User",
            Type = Swap.CLI.Commands.Relationships.Models.RelationshipType.OneToOne,
            IsRequired = true,
            ForeignKeyName = "OwnerUserId",
            ProjectPath = _testProjectPath,
            SkipNavigation = false,
            OnDelete = Swap.CLI.Commands.Relationships.Models.DeleteBehavior.Restrict
        };

        // Act
        var updatedProfile = await Swap.CLI.Commands.Relationships.EntityModifier.AddOneToOnePropertiesAsync(
            profilePath, "Profile", "User", definition, isDependent: true);
        
        // Assert
        Assert.Contains("public int OwnerUserId { get; set; }", updatedProfile);
    }

    [Fact]
    public async Task GenerateOneToOne_CustomNavigationPropertyNames()
    {
        // Arrange
        var userPath = Path.Combine(_testProjectPath, "Models", "User.cs");
        var profilePath = Path.Combine(_testProjectPath, "Models", "Profile.cs");
        
        await File.WriteAllTextAsync(userPath, @"namespace Models;

public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
}
");
        
        await File.WriteAllTextAsync(profilePath, @"namespace Models;

public class Profile
{
    public int Id { get; set; }
    public string Bio { get; set; } = string.Empty;
}
");

        var definition = new Swap.CLI.Commands.Relationships.Models.RelationshipDefinition
        {
            SourceEntity = "Profile",
            TargetEntity = "User",
            Type = Swap.CLI.Commands.Relationships.Models.RelationshipType.OneToOne,
            IsRequired = true,
            NavigationProperty = "UserProfile", // Principal -> Dependent
            InverseNavigation = "Owner", // Dependent -> Principal
            ProjectPath = _testProjectPath,
            SkipNavigation = false,
            OnDelete = Swap.CLI.Commands.Relationships.Models.DeleteBehavior.Restrict
        };

        // Act
        var updatedUser = await Swap.CLI.Commands.Relationships.EntityModifier.AddOneToOnePropertiesAsync(
            userPath, "User", "Profile", definition, isDependent: false);
        var updatedProfile = await Swap.CLI.Commands.Relationships.EntityModifier.AddOneToOnePropertiesAsync(
            profilePath, "Profile", "User", definition, isDependent: true);
        
        // Assert
        Assert.Contains("public Profile? UserProfile { get; set; }", updatedUser);
        Assert.Contains("public User? Owner { get; set; }", updatedProfile);
    }

    [Fact]
    public async Task GenerateOneToOne_SkipNavigation_DoesNotAddNavigationProperties()
    {
        // Arrange
        var userPath = Path.Combine(_testProjectPath, "Models", "User.cs");
        var profilePath = Path.Combine(_testProjectPath, "Models", "Profile.cs");
        
        await File.WriteAllTextAsync(userPath, @"namespace Models;

public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
}
");
        
        await File.WriteAllTextAsync(profilePath, @"namespace Models;

public class Profile
{
    public int Id { get; set; }
    public string Bio { get; set; } = string.Empty;
}
");

        var definition = new Swap.CLI.Commands.Relationships.Models.RelationshipDefinition
        {
            SourceEntity = "Profile",
            TargetEntity = "User",
            Type = Swap.CLI.Commands.Relationships.Models.RelationshipType.OneToOne,
            IsRequired = true,
            ProjectPath = _testProjectPath,
            SkipNavigation = true,
            OnDelete = Swap.CLI.Commands.Relationships.Models.DeleteBehavior.Restrict
        };

        // Act
        var updatedProfile = await Swap.CLI.Commands.Relationships.EntityModifier.AddOneToOnePropertiesAsync(
            profilePath, "Profile", "User", definition, isDependent: true);
        
        // Assert
        Assert.Contains("public int UserId { get; set; }", updatedProfile); // FK still added
        Assert.DoesNotContain("public User? User { get; set; }", updatedProfile); // Nav skipped
    }

    [Fact]
    public async Task GenerateOneToOne_DbContext_AddsHasOneWithOneConfiguration()
    {
        // Arrange
        var dbContextPath = Path.Combine(_testProjectPath, "Data", "AppDbContext.cs");
        await File.WriteAllTextAsync(dbContextPath, @"using Microsoft.EntityFrameworkCore;
using Models;

namespace Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Profile> Profiles { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    }
}
");

        var definition = new Swap.CLI.Commands.Relationships.Models.RelationshipDefinition
        {
            SourceEntity = "Profile",
            TargetEntity = "User",
            Type = Swap.CLI.Commands.Relationships.Models.RelationshipType.OneToOne,
            IsRequired = true,
            ProjectPath = _testProjectPath,
            OnDelete = Swap.CLI.Commands.Relationships.Models.DeleteBehavior.Restrict
        };

        // Act
        var updatedDbContext = await Swap.CLI.Commands.Relationships.DbContextModifier.ConfigureOneToOneAsync(
            dbContextPath, definition, "User", "Profile");

        // Assert
        Assert.Contains("modelBuilder.Entity<User>()", updatedDbContext);
        Assert.Contains(".HasOne(e => e.Profile)", updatedDbContext);
        Assert.Contains(".WithOne(e => e.User)", updatedDbContext);
        Assert.Contains(".HasForeignKey<Profile>(e => e.UserId)", updatedDbContext);
        Assert.Contains(".IsRequired()", updatedDbContext);
        Assert.Contains(".OnDelete(DeleteBehavior.Restrict)", updatedDbContext);
    }

    [Fact]
    public async Task GenerateOneToOne_DbContext_AddsUniqueConstraint()
    {
        // Arrange
        var dbContextPath = Path.Combine(_testProjectPath, "Data", "AppDbContext.cs");
        await File.WriteAllTextAsync(dbContextPath, @"using Microsoft.EntityFrameworkCore;
using Models;

namespace Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Profile> Profiles { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    }
}
");

        var definition = new Swap.CLI.Commands.Relationships.Models.RelationshipDefinition
        {
            SourceEntity = "Profile",
            TargetEntity = "User",
            Type = Swap.CLI.Commands.Relationships.Models.RelationshipType.OneToOne,
            IsRequired = true,
            ProjectPath = _testProjectPath,
            OnDelete = Swap.CLI.Commands.Relationships.Models.DeleteBehavior.Restrict
        };

        // Act
        var updatedDbContext = await Swap.CLI.Commands.Relationships.DbContextModifier.ConfigureOneToOneAsync(
            dbContextPath, definition, "User", "Profile");

        // Assert
        Assert.Contains("modelBuilder.Entity<Profile>()", updatedDbContext);
        Assert.Contains(".HasIndex(e => e.UserId)", updatedDbContext);
        Assert.Contains(".IsUnique()", updatedDbContext);
    }

    [Fact]
    public async Task GenerateOneToOne_DbContext_OptionalRelationship()
    {
        // Arrange
        var dbContextPath = Path.Combine(_testProjectPath, "Data", "AppDbContext.cs");
        await File.WriteAllTextAsync(dbContextPath, @"using Microsoft.EntityFrameworkCore;
using Models;

namespace Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Profile> Profiles { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    }
}
");

        var definition = new Swap.CLI.Commands.Relationships.Models.RelationshipDefinition
        {
            SourceEntity = "Profile",
            TargetEntity = "User",
            Type = Swap.CLI.Commands.Relationships.Models.RelationshipType.OneToOne,
            IsRequired = false,
            ProjectPath = _testProjectPath,
            OnDelete = Swap.CLI.Commands.Relationships.Models.DeleteBehavior.SetNull
        };

        // Act
        var updatedDbContext = await Swap.CLI.Commands.Relationships.DbContextModifier.ConfigureOneToOneAsync(
            dbContextPath, definition, "User", "Profile");

        // Assert
        Assert.Contains(".IsRequired(false)", updatedDbContext);
        Assert.Contains(".OnDelete(DeleteBehavior.SetNull)", updatedDbContext);
    }

    [Fact]
    public async Task GenerateOneToOne_DbContext_CascadeDelete()
    {
        // Arrange
        var dbContextPath = Path.Combine(_testProjectPath, "Data", "AppDbContext.cs");
        await File.WriteAllTextAsync(dbContextPath, @"using Microsoft.EntityFrameworkCore;
using Models;

namespace Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Profile> Profiles { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    }
}
");

        var definition = new Swap.CLI.Commands.Relationships.Models.RelationshipDefinition
        {
            SourceEntity = "Profile",
            TargetEntity = "User",
            Type = Swap.CLI.Commands.Relationships.Models.RelationshipType.OneToOne,
            IsRequired = true,
            ProjectPath = _testProjectPath,
            OnDelete = Swap.CLI.Commands.Relationships.Models.DeleteBehavior.Cascade
        };

        // Act
        var updatedDbContext = await Swap.CLI.Commands.Relationships.DbContextModifier.ConfigureOneToOneAsync(
            dbContextPath, definition, "User", "Profile");

        // Assert
        Assert.Contains(".OnDelete(DeleteBehavior.Cascade)", updatedDbContext);
    }

    [Fact]
    public async Task GenerateOneToOne_DbContext_CustomForeignKeyName()
    {
        // Arrange
        var dbContextPath = Path.Combine(_testProjectPath, "Data", "AppDbContext.cs");
        await File.WriteAllTextAsync(dbContextPath, @"using Microsoft.EntityFrameworkCore;
using Models;

namespace Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Profile> Profiles { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    }
}
");

        var definition = new Swap.CLI.Commands.Relationships.Models.RelationshipDefinition
        {
            SourceEntity = "Profile",
            TargetEntity = "User",
            Type = Swap.CLI.Commands.Relationships.Models.RelationshipType.OneToOne,
            IsRequired = true,
            ForeignKeyName = "OwnerUserId",
            ProjectPath = _testProjectPath,
            OnDelete = Swap.CLI.Commands.Relationships.Models.DeleteBehavior.Restrict
        };

        // Act
        var updatedDbContext = await Swap.CLI.Commands.Relationships.DbContextModifier.ConfigureOneToOneAsync(
            dbContextPath, definition, "User", "Profile");

        // Assert
        Assert.Contains(".HasForeignKey<Profile>(e => e.OwnerUserId)", updatedDbContext);
        Assert.Contains(".HasIndex(e => e.OwnerUserId)", updatedDbContext);
    }

    [Fact]
    public async Task GenerateOneToOne_Idempotent_DoesNotDuplicateProperties()
    {
        // Arrange
        var profilePath = Path.Combine(_testProjectPath, "Models", "Profile.cs");
        await File.WriteAllTextAsync(profilePath, @"namespace Models;

public class Profile
{
    public int Id { get; set; }
    public string Bio { get; set; } = string.Empty;
}
");

        var definition = new Swap.CLI.Commands.Relationships.Models.RelationshipDefinition
        {
            SourceEntity = "Profile",
            TargetEntity = "User",
            Type = Swap.CLI.Commands.Relationships.Models.RelationshipType.OneToOne,
            IsRequired = true,
            ProjectPath = _testProjectPath,
            SkipNavigation = false,
            OnDelete = Swap.CLI.Commands.Relationships.Models.DeleteBehavior.Restrict
        };

        // Act - Apply twice
        var updated1 = await Swap.CLI.Commands.Relationships.EntityModifier.AddOneToOnePropertiesAsync(
            profilePath, "Profile", "User", definition, isDependent: true);
        await File.WriteAllTextAsync(profilePath, updated1);
        
        var updated2 = await Swap.CLI.Commands.Relationships.EntityModifier.AddOneToOnePropertiesAsync(
            profilePath, "Profile", "User", definition, isDependent: true);
        
        // Assert - Should only have one of each property
        var fkCount = System.Text.RegularExpressions.Regex.Matches(updated2, "public int UserId").Count;
        var navCount = System.Text.RegularExpressions.Regex.Matches(updated2, "public User\\? User").Count;
        
        Assert.Equal(1, fkCount);
        Assert.Equal(1, navCount);
    }

    [Fact]
    public async Task GenerateOneToOne_DbContext_CustomNavigationNames()
    {
        // Arrange
        var dbContextPath = Path.Combine(_testProjectPath, "Data", "AppDbContext.cs");
        await File.WriteAllTextAsync(dbContextPath, @"using Microsoft.EntityFrameworkCore;
using Models;

namespace Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Profile> Profiles { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    }
}
");

        var definition = new Swap.CLI.Commands.Relationships.Models.RelationshipDefinition
        {
            SourceEntity = "Profile",
            TargetEntity = "User",
            Type = Swap.CLI.Commands.Relationships.Models.RelationshipType.OneToOne,
            IsRequired = true,
            NavigationProperty = "UserProfile", // Principal -> Dependent
            InverseNavigation = "Owner", // Dependent -> Principal
            ProjectPath = _testProjectPath,
            OnDelete = Swap.CLI.Commands.Relationships.Models.DeleteBehavior.Restrict
        };

        // Act
        var updatedDbContext = await Swap.CLI.Commands.Relationships.DbContextModifier.ConfigureOneToOneAsync(
            dbContextPath, definition, "User", "Profile");

        // Assert
        Assert.Contains(".HasOne(e => e.UserProfile)", updatedDbContext);
        Assert.Contains(".WithOne(e => e.Owner)", updatedDbContext);
    }
}
