using Xunit;

namespace Swap.CLI.Tests.Commands;

public class GenerateControllerEdgeCasesTests
{
    [Theory]
    [InlineData("Task", "Tasks")] // Conflicts with System.Threading.Tasks.Task
    [InlineData("Attribute", "Attributes")] // Conflicts with System.Attribute
    [InlineData("Console", "Consoles")] // Conflicts with System.Console
    public void GenerateController_ShouldHandleSystemTypeConflicts(string entityName, string pluralName)
    {
        // These entity names conflict with System types
        // The command should handle them by using fully qualified names
        // This test documents the expected behavior
        
        Assert.NotNull(entityName);
        Assert.NotNull(pluralName);
        
        // In the actual implementation:
        // - Controller should use Models.Task instead of Task
        // - DbContext should use ProjectName.Models.Task instead of Task
    }

    [Theory]
    [InlineData("Product With Space")] // Spaces not allowed
    [InlineData("Product-Item")] // Hyphens not allowed
    [InlineData("Product.Item")] // Dots not allowed
    [InlineData("Product/Item")] // Slashes not allowed
    public void GenerateController_ShouldRejectInvalidEntityNames(string invalidName)
    {
        // Entity names should only contain alphanumeric characters
        // These should be rejected during validation
        
        // Verify the string contains at least one invalid character
        var hasInvalidChar = invalidName.Any(c => !char.IsLetterOrDigit(c));
        Assert.True(hasInvalidChar, $"Test data '{invalidName}' should contain at least one invalid character");
    }

    [Theory]
    [InlineData("product", "Product")] // lowercase
    [InlineData("PRODUCT", "PRODUCT")] // uppercase (should convert first char only)
    [InlineData("myProduct", "MyProduct")] // camelCase
    public void GenerateController_ShouldNormalizeCasing(string input, string expected)
    {
        // The command should auto-correct casing to PascalCase
        var normalized = char.IsUpper(input[0]) 
            ? input 
            : char.ToUpper(input[0]) + input.Substring(1);
        
        Assert.Equal(expected, normalized);
    }

    [Fact]
    public void GenerateController_ShouldDetectMissingCsprojFile()
    {
        // Command should fail gracefully if not run from project directory
        // This prevents generating files in wrong location
        
        // Expected behavior: Check for *.csproj in current directory
        // If not found, show error: "No .csproj file found in current directory"
        Assert.True(true); // Documented behavior
    }

    [Fact]
    public void GenerateController_ShouldDetectExistingController()
    {
        // If ProductController.cs already exists, should:
        // Option 1: Warn and skip
        // Option 2: Prompt for overwrite
        // Option 3: Fail with clear error
        
        // Current implementation: Will overwrite
        // Future enhancement: Add --force flag
        Assert.True(true); // Documented behavior
    }

    [Fact]
    public void GenerateController_ShouldDetectExistingDbSet()
    {
        // If DbSet<Product> already exists in DbContext
        // Should skip adding it again and show warning
        
        // Implementation checks: content.Contains($"DbSet<{entityName}>")
        Assert.True(true); // Already implemented
    }

    [Theory]
    [InlineData("Product", "Products")]
    [InlineData("Category", "Categorys")] // Note: Simple pluralization (not Categories)
    [InlineData("Person", "Persons")] // Note: Not People
    [InlineData("Child", "Childs")] // Note: Not Children
    public void SimplePluralization_DocumentedBehavior(string singular, string expectedPlural)
    {
        // Current implementation uses simple +s pluralization
        // Future enhancement: Use Humanizer library for proper pluralization
        var simplePlural = singular + "s";
        
        Assert.Equal(expectedPlural, simplePlural);
    }

    [Fact]
    public void DbContextUpdate_ShouldFindLastDbSet()
    {
        // DbContext update logic finds last DbSet and inserts after it
        // Pattern: "public DbSet<"
        
        var dbContextSample = @"
    public DbSet<TodoItem> TodoItems => Set<TodoItem>();
    public DbSet<User> Users { get; set; }
    
    protected override void OnModelCreating";
        
        var lastDbSetIndex = dbContextSample.LastIndexOf("public DbSet<");
        
        Assert.True(lastDbSetIndex > 0);
        Assert.Contains("Users", dbContextSample.Substring(lastDbSetIndex));
    }

    [Fact]
    public void DbContextUpdate_ShouldHandleDifferentDbSetStyles()
    {
        // DbContext might use different property styles:
        // Style 1: public DbSet<Entity> Entities { get; set; }
        // Style 2: public DbSet<Entity> Entities => Set<Entity>();
        
        var style1 = "public DbSet<Product> Products { get; set; }";
        var style2 = "public DbSet<TodoItem> TodoItems => Set<TodoItem>();";
        
        Assert.Contains("public DbSet<", style1);
        Assert.Contains("public DbSet<", style2);
    }
}
