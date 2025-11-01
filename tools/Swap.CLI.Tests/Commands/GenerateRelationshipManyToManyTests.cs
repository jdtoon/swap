using Swap.CLI.Commands.Relationships;
using Swap.CLI.Commands.Relationships.Models;
using Xunit;

namespace Swap.CLI.Tests.Commands;

/// <summary>
/// Tests for many-to-many relationship generation in GenerateRelationshipCommand
/// </summary>
public class GenerateRelationshipManyToManyTests : IDisposable
{
    private readonly string _testProjectPath;
    private readonly string _testModelsPath;
    private readonly string _testDataPath;

    public GenerateRelationshipManyToManyTests()
    {
        _testProjectPath = Path.Combine(Path.GetTempPath(), $"SwapTest_{Guid.NewGuid()}");
        _testModelsPath = Path.Combine(_testProjectPath, "Models");
        _testDataPath = Path.Combine(_testProjectPath, "Data");
        
        Directory.CreateDirectory(_testModelsPath);
        Directory.CreateDirectory(_testDataPath);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testProjectPath))
        {
            Directory.Delete(_testProjectPath, true);
        }
    }

    [Fact]
    public async Task EntityModifier_AddManyToManyNavigationAsync_AddsCollectionProperty()
    {
        // Arrange
        var studentCode = @"namespace Models;

public class Student
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
";
        var studentPath = Path.Combine(_testModelsPath, "Student.cs");
        await File.WriteAllTextAsync(studentPath, studentCode);

        // Act
        var result = await EntityModifier.AddManyToManyNavigationAsync(
            studentPath, "Student", "Course", null);

        // Assert
        Assert.Contains("ICollection<Course>", result);
        Assert.Contains("Courses", result);
        Assert.Contains("new List<Course>()", result);
    }

    [Fact]
    public async Task EntityModifier_AddManyToManyNavigationAsync_SupportsCustomPropertyName()
    {
        // Arrange
        var studentCode = @"namespace Models;

public class Student
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
";
        var studentPath = Path.Combine(_testModelsPath, "Student.cs");
        await File.WriteAllTextAsync(studentPath, studentCode);

        // Act
        var result = await EntityModifier.AddManyToManyNavigationAsync(
            studentPath, "Student", "Course", "EnrolledCourses");

        // Assert
        Assert.Contains("ICollection<Course>", result);
        Assert.Contains("public ICollection<Course> EnrolledCourses { get; set; }", result);
        Assert.Contains("= new List<Course>()", result);
    }

    [Fact]
    public async Task EntityModifier_AddManyToManyNavigationAsync_DoesNotDuplicateExistingProperty()
    {
        // Arrange
        var studentCode = @"namespace Models;

public class Student
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ICollection<Course> Courses { get; set; } = new List<Course>();
}
";
        var studentPath = Path.Combine(_testModelsPath, "Student.cs");
        await File.WriteAllTextAsync(studentPath, studentCode);

        // Act
        var result = await EntityModifier.AddManyToManyNavigationAsync(
            studentPath, "Student", "Course", null);

        // Assert - should return original code unchanged
        Assert.Equal(studentCode, result);
    }

    [Fact]
    public async Task DbContextModifier_AddDbSetAsync_AddsJunctionTableDbSet()
    {
        // Arrange
        var dbContextCode = @"using Microsoft.EntityFrameworkCore;

namespace Data;

public class AppDbContext : DbContext
{
    public DbSet<Student> Students { get; set; }
    public DbSet<Course> Courses { get; set; }
}
";
        var dbContextPath = Path.Combine(_testDataPath, "AppDbContext.cs");
        await File.WriteAllTextAsync(dbContextPath, dbContextCode);

        // Act
        var result = await DbContextModifier.AddDbSetAsync(dbContextPath, "CourseStudent");

        // Assert
        Assert.Contains("public DbSet<CourseStudent> CourseStudents { get; set; }", result);
    }

    [Fact]
    public async Task DbContextModifier_AddDbSetAsync_DoesNotDuplicateExistingDbSet()
    {
        // Arrange
        var dbContextCode = @"using Microsoft.EntityFrameworkCore;

namespace Data;

public class AppDbContext : DbContext
{
    public DbSet<Student> Students { get; set; }
    public DbSet<Course> Courses { get; set; }
    public DbSet<CourseStudent> CourseStudent { get; set; }
}
";
        var dbContextPath = Path.Combine(_testDataPath, "AppDbContext.cs");
        await File.WriteAllTextAsync(dbContextPath, dbContextCode);

        // Act
        var result = await DbContextModifier.AddDbSetAsync(dbContextPath, "CourseStudent");

        // Assert - should return original code unchanged
        Assert.Equal(dbContextCode, result);
    }

    [Fact]
    public async Task DbContextModifier_ConfigureManyToManyAsync_AddsFluentAPIConfiguration()
    {
        // Arrange
        var dbContextCode = @"using Microsoft.EntityFrameworkCore;

namespace Data;

public class AppDbContext : DbContext
{
    public DbSet<Student> Students { get; set; }
    public DbSet<Course> Courses { get; set; }
    public DbSet<CourseStudent> CourseStudent { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    }
}
";
        var definition = new RelationshipDefinition
        {
            ProjectPath = _testProjectPath,
            SourceEntity = "Student",
            TargetEntity = "Course",
            Type = RelationshipType.ManyToMany,
            NavigationProperty = null,
            InverseNavigation = null
        };

        // Act
        var result = await DbContextModifier.ConfigureManyToManyAsync(
            dbContextCode, definition, "CourseStudent");

        // Assert
        Assert.Contains("modelBuilder.Entity<Student>()", result);
        Assert.Contains(".HasMany(e => e.Courses)", result);
        Assert.Contains(".WithMany(e => e.Students)", result);
        Assert.Contains(".UsingEntity<CourseStudent>", result);
        Assert.Contains("HasKey(x => new { x.StudentId, x.CourseId })", result);
    }

    [Fact]
    public async Task DbContextModifier_ConfigureManyToManyAsync_SupportsCustomNavigationProperties()
    {
        // Arrange
        var dbContextCode = @"using Microsoft.EntityFrameworkCore;

namespace Data;

public class AppDbContext : DbContext
{
    public DbSet<Student> Students { get; set; }
    public DbSet<Course> Courses { get; set; }
    public DbSet<CourseStudent> CourseStudent { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    }
}
";
        var definition = new RelationshipDefinition
        {
            ProjectPath = _testProjectPath,
            SourceEntity = "Student",
            TargetEntity = "Course",
            Type = RelationshipType.ManyToMany,
            NavigationProperty = "EnrolledCourses",
            InverseNavigation = "EnrolledStudents"
        };

        // Act
        var result = await DbContextModifier.ConfigureManyToManyAsync(
            dbContextCode, definition, "CourseStudent");

        // Assert
        Assert.Contains(".HasMany(e => e.EnrolledCourses)", result);
        Assert.Contains(".WithMany(e => e.EnrolledStudents)", result);
    }

    [Fact]
    public async Task DbContextModifier_ConfigureManyToManyAsync_CreatesOnModelCreatingIfNotExists()
    {
        // Arrange
        var dbContextCode = @"using Microsoft.EntityFrameworkCore;

namespace Data;

public class AppDbContext : DbContext
{
    public DbSet<Student> Students { get; set; }
    public DbSet<Course> Courses { get; set; }
    public DbSet<CourseStudent> CourseStudent { get; set; }
}
";
        var definition = new RelationshipDefinition
        {
            ProjectPath = _testProjectPath,
            SourceEntity = "Student",
            TargetEntity = "Course",
            Type = RelationshipType.ManyToMany
        };

        // Act
        var result = await DbContextModifier.ConfigureManyToManyAsync(
            dbContextCode, definition, "CourseStudent");

        // Assert
        Assert.Contains("protected override void OnModelCreating(ModelBuilder modelBuilder)", result);
        Assert.Contains("base.OnModelCreating(modelBuilder);", result);
        Assert.Contains("modelBuilder.Entity<Student>()", result);
    }

    [Theory]
    [InlineData("Student", "Course", "CourseStudent")]
    [InlineData("Product", "Tag", "ProductTag")]
    [InlineData("User", "Role", "RoleUser")]
    [InlineData("Order", "Product", "OrderProduct")]
    public async Task JunctionTableName_GeneratesCorrectAlphabeticalOrder(
        string entity1, string entity2, string expected)
    {
        // Arrange - use reflection to call private method
        var method = typeof(GenerateRelationshipCommand).GetMethod(
            "GenerateJunctionTableName",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        // Act
        var result = method?.Invoke(null, new object[] { entity1, entity2 }) as string;

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task JunctionEntityCode_GeneratesValidClass()
    {
        // Arrange - use reflection to call private method
        var method = typeof(GenerateRelationshipCommand).GetMethod(
            "GenerateJunctionEntityCode",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        // Act
        var result = method?.Invoke(null, new object?[] 
        { 
            "Student", 
            "Course", 
            "CourseStudent",
            null,
            "TestApp"
        }) as string;

        // Assert
        Assert.NotNull(result);
        Assert.Contains("public class CourseStudent", result);
        Assert.Contains("public int StudentId { get; set; }", result);
        Assert.Contains("public Student? Student { get; set; }", result);
        Assert.Contains("public int CourseId { get; set; }", result);
        Assert.Contains("public Course? Course { get; set; }", result);
    }

    [Fact]
    public async Task JunctionEntityCode_SupportsAdditionalProperties()
    {
        // Arrange
        var additionalProps = new Dictionary<string, string>
        {
            { "EnrolledDate", "datetime" },
            { "Grade", "decimal" }
        };

        var method = typeof(GenerateRelationshipCommand).GetMethod(
            "GenerateJunctionEntityCode",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        // Act
        var result = method?.Invoke(null, new object[] 
        { 
            "Student", 
            "Course", 
            "CourseStudent",
            additionalProps,
            "TestApp"
        }) as string;

        // Assert
        Assert.NotNull(result);
        Assert.Contains("public DateTime EnrolledDate { get; set; }", result);
        Assert.Contains("public decimal Grade { get; set; }", result);
    }

    [Theory]
    [InlineData("string", "string")]
    [InlineData("int", "int")]
    [InlineData("datetime", "DateTime")]
    [InlineData("bool", "bool")]
    [InlineData("decimal", "decimal")]
    [InlineData("guid", "Guid")]
    public async Task MapPropertyType_ConvertsCorrectly(string input, string expected)
    {
        // Arrange - use reflection to call private method
        var method = typeof(GenerateRelationshipCommand).GetMethod(
            "MapPropertyType",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        // Act
        var result = method?.Invoke(null, new object[] { input }) as string;

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void EntityModifier_Pluralize_HandlesCommonCases()
    {
        // Arrange & Act & Assert
        Assert.Equal("Courses", EntityModifier.Pluralize("Course"));
        Assert.Equal("Students", EntityModifier.Pluralize("Student"));
        Assert.Equal("Categories", EntityModifier.Pluralize("Category"));
        Assert.Equal("Taxes", EntityModifier.Pluralize("Tax"));
        // Note: Simple pluralization - "Person" -> "Persons", "Child" -> "Childs"
        // For irregular plurals, users should specify custom navigation property names
    }
}
