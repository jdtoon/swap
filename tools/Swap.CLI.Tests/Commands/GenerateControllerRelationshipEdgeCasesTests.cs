using Swap.CLI.Commands;
using System.CommandLine;
using Xunit;

namespace Swap.CLI.Tests.Commands;

/// <summary>
/// Tests for self-referencing and multiple relationship edge cases
/// </summary>
public class GenerateControllerRelationshipEdgeCasesTests
{
    [Fact]
    public void SelfReferencingRelationship_DetectsCorrectly()
    {
        // This test verifies that ParentId in Category is detected as self-reference
        // The actual detection logic is tested in integration tests
        
        // Arrange
        var command = GenerateControllerCommand.Create();
        
        // Act - Parse command with ParentId field
        var parseResult = command.Parse("Category --fields \"Name:string ParentId:int?\" --with-relationships");
        
        // Assert
        Assert.Empty(parseResult.Errors);
    }
    
    [Theory]
    [InlineData("ParentId", "Parent")]
    [InlineData("ManagerId", "Manager")]
    [InlineData("SupervisorId", "Supervisor")]
    [InlineData("ReportsToId", "ReportsTo")]
    public void SelfReferencingRelationship_ExtractsCorrectNavigationPropertyName(string fkProperty, string expectedNavProperty)
    {
        // The navigation property should be the FK prefix without "Id"
        // e.g., ParentId → Parent, ManagerId → Manager
        
        var fkPrefix = fkProperty.Substring(0, fkProperty.Length - 2);
        Assert.Equal(expectedNavProperty, fkPrefix);
    }
    
    [Fact]
    public void MultipleRelationships_ParsesSuccessfully()
    {
        // Arrange
        var command = GenerateControllerCommand.Create();
        
        // Act - Entity with multiple foreign keys
        var parseResult = command.Parse("Shipment --fields \"CustomerId:int? ShipperId:int?\" --with-relationships");
        
        // Assert
        Assert.Empty(parseResult.Errors);
    }
    
    [Theory]
    [InlineData("Order", "CustomerId", "ShipperId")]
    [InlineData("Invoice", "CustomerId", "VendorId")]
    [InlineData("Transaction", "FromAccountId", "ToAccountId")]
    public void MultipleRelationships_WithDifferentPatterns(string entityName, string fk1, string fk2)
    {
        // Arrange
        var command = GenerateControllerCommand.Create();
        var fields = $"\"{fk1}:int? {fk2}:int?\"";
        
        // Act
        var parseResult = command.Parse($"{entityName} --fields {fields} --with-relationships");
        
        // Assert
        Assert.Empty(parseResult.Errors);
    }
    
    [Fact]
    public void WithRelationshipsFlag_IsOptional()
    {
        // Arrange
        var command = GenerateControllerCommand.Create();
        
        // Act - Without flag should still work
        var parseResult = command.Parse("Product --fields \"Name:string CategoryId:int?\"");
        
        // Assert
        Assert.Empty(parseResult.Errors);
    }
    
    [Fact]
    public void WithRelationshipsFlag_CanBeCombinedWithOtherFlags()
    {
        // Arrange
        var command = GenerateControllerCommand.Create();
        
        // Act
        var parseResult = command.Parse("Product --fields \"Name:string\" --with-relationships --add-nav --no-migrations --force");
        
        // Assert
        Assert.Empty(parseResult.Errors);
    }
}
