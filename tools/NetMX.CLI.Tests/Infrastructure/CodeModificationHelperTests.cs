using NetMX.CLI.Infrastructure;
using Xunit;
using FluentAssertions;

namespace NetMX.CLI.Tests.Infrastructure;

public class CodeModificationHelperTests
{
    private const string SampleDbContextCode = @"
using Microsoft.EntityFrameworkCore;

namespace MyApp.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users => Set<User>();
    }
}";

    private const string MinimalDbContextCode = @"
using Microsoft.EntityFrameworkCore;

namespace MyApp.Data
{
    public class AppDbContext : DbContext
    {
    }
}";

    [Fact]
    public void AddDbSetProperty_ShouldAddPropertyToDbContext()
    {
        // Act
        var result = CodeModificationHelper.AddDbSetProperty(MinimalDbContextCode, "Product");

        // Assert
        result.Should().Contain("public DbSet<Product> Products => Set<Product>();");
    }

    [Fact]
    public void AddDbSetProperty_ShouldPluralizePropertyName()
    {
        // Arrange & Act
        var resultProduct = CodeModificationHelper.AddDbSetProperty(MinimalDbContextCode, "Product");
        var resultCategory = CodeModificationHelper.AddDbSetProperty(MinimalDbContextCode, "Category");
        var resultAddress = CodeModificationHelper.AddDbSetProperty(MinimalDbContextCode, "Address");

        // Assert
        resultProduct.Should().Contain("Products");
        resultCategory.Should().Contain("Categories"); // y -> ies
        resultAddress.Should().Contain("Addresses"); // ss -> sses
    }

    [Fact]
    public void AddDbSetProperty_ShouldThrowWhenDbContextNotFound()
    {
        // Arrange
        var invalidCode = @"
namespace MyApp
{
    public class MyClass
    {
    }
}";

        // Act & Assert
        var act = () => CodeModificationHelper.AddDbSetProperty(invalidCode, "Product");
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*No DbContext class found*");
    }

    [Fact]
    public void AddDbSetProperty_ShouldThrowWhenPropertyAlreadyExists()
    {
        // Act & Assert
        var act = () => CodeModificationHelper.AddDbSetProperty(SampleDbContextCode, "User");
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public void AddDbSetProperty_ShouldAddUsingDirective()
    {
        // Act
        var result = CodeModificationHelper.AddDbSetProperty(
            MinimalDbContextCode, 
            "Product", 
            "MyApp.Core.Entities");

        // Assert
        result.Should().Contain("using MyApp.Core.Entities;");
    }

    [Fact]
    public void AddDbSetProperty_ShouldNotDuplicateUsingDirective()
    {
        // Arrange
        var codeWithUsing = @"
using Microsoft.EntityFrameworkCore;
using MyApp.Core.Entities;

namespace MyApp.Data
{
    public class AppDbContext : DbContext
    {
    }
}";

        // Act
        var result = CodeModificationHelper.AddDbSetProperty(
            codeWithUsing, 
            "Product", 
            "MyApp.Core.Entities");

        // Assert
        var usingCount = result.Split("using MyApp.Core.Entities;").Length - 1;
        usingCount.Should().Be(1);
    }

    [Fact]
    public void FindDbContextFile_ShouldFindInDataFolder()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var dataDir = Path.Combine(tempDir, "Data");
        Directory.CreateDirectory(dataDir);
        var dbContextFile = Path.Combine(dataDir, "AppDbContext.cs");
        File.WriteAllText(dbContextFile, MinimalDbContextCode);

        try
        {
            // Act
            var result = CodeModificationHelper.FindDbContextFile(tempDir);

            // Assert
            result.Should().NotBeNull();
            result.Should().EndWith("AppDbContext.cs");
        }
        finally
        {
            // Cleanup
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void FindDbContextFile_ShouldReturnNullWhenNotFound()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            // Act
            var result = CodeModificationHelper.FindDbContextFile(tempDir);

            // Assert
            result.Should().BeNull();
        }
        finally
        {
            // Cleanup
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void IsValidCSharpCode_ShouldReturnTrueForValidCode()
    {
        // Act
        var result = CodeModificationHelper.IsValidCSharpCode(MinimalDbContextCode);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsValidCSharpCode_ShouldReturnFalseForInvalidCode()
    {
        // Arrange
        var invalidCode = "this is not valid C# code { } } {";

        // Act
        var result = CodeModificationHelper.IsValidCSharpCode(invalidCode);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ExtractNamespace_ShouldExtractFileScopedNamespace()
    {
        // Arrange
        var codeWithFileScopedNamespace = @"
using Microsoft.EntityFrameworkCore;

namespace MyApp.Data;

public class AppDbContext : DbContext
{
}";

        // Act
        var result = CodeModificationHelper.ExtractNamespace(codeWithFileScopedNamespace);

        // Assert
        result.Should().Be("MyApp.Data");
    }

    [Fact]
    public void ExtractNamespace_ShouldExtractBlockScopedNamespace()
    {
        // Act
        var result = CodeModificationHelper.ExtractNamespace(MinimalDbContextCode);

        // Assert
        result.Should().Be("MyApp.Data");
    }

    [Fact]
    public void ExtractClassNames_ShouldExtractAllClassNames()
    {
        // Arrange
        var codeWithMultipleClasses = @"
namespace MyApp
{
    public class Class1 { }
    internal class Class2 { }
    public class Class3 { }
}";

        // Act
        var result = CodeModificationHelper.ExtractClassNames(codeWithMultipleClasses);

        // Assert
        result.Should().HaveCount(3);
        result.Should().Contain("Class1");
        result.Should().Contain("Class2");
        result.Should().Contain("Class3");
    }

    [Fact]
    public void AddDbSetProperty_ShouldPreserveExistingCode()
    {
        // Act
        var result = CodeModificationHelper.AddDbSetProperty(SampleDbContextCode, "Product");

        // Assert
        result.Should().Contain("public DbSet<User> Users => Set<User>();");
        result.Should().Contain("public DbSet<Product> Products => Set<Product>();");
        result.Should().Contain("public AppDbContext(DbContextOptions<AppDbContext> options)");
    }

    [Fact]
    public void AddDbSetProperty_ShouldFormatCodeProperly()
    {
        // Act
        var result = CodeModificationHelper.AddDbSetProperty(MinimalDbContextCode, "Product");

        // Assert
        // Should have the DbSet property
        result.Should().Contain("public DbSet<Product> Products => Set<Product>();");
        // Should have proper structure (namespace and class)
        result.Should().Contain("namespace MyApp.Data");
        result.Should().Contain("public class AppDbContext : DbContext");
    }

    [Theory]
    [InlineData("Product", "Products")]
    [InlineData("Category", "Categories")]
    [InlineData("Box", "Boxes")]
    [InlineData("Batch", "Batches")]
    [InlineData("Brush", "Brushes")]
    [InlineData("Class", "Classes")]
    [InlineData("Person", "Persons")] // Simple rule: not Persons -> People
    public void AddDbSetProperty_ShouldHandleVariousPluralizations(string singular, string expectedPlural)
    {
        // Act
        var result = CodeModificationHelper.AddDbSetProperty(MinimalDbContextCode, singular);

        // Assert
        result.Should().Contain($"public DbSet<{singular}> {expectedPlural} =>");
    }
}
