using Swap.CLI.Infrastructure;
using Xunit;

namespace Swap.CLI.Tests.Infrastructure;

public class FieldHelperTests
{
    [Fact]
    public void ParseFields_WithoutFlags_UsesDefaults()
    {
        // Arrange
        var fieldsSpec = "Name:string Price:decimal InStock:bool";
        
        // Act
        var fields = FieldHelper.ParseFields(fieldsSpec);
        
        // Assert
        Assert.Equal(3, fields.Count);
        
        // All fields should be sortable by default
        Assert.All(fields, f => Assert.True(f.IsSortable));
        
        // All fields should be non-filterable by default
        Assert.All(fields, f => Assert.False(f.IsFilterable));
    }
    
    [Fact]
    public void ParseFields_WithSortableFlag_SetsSortable()
    {
        // Arrange
        var fieldsSpec = "Name:string:sortable Price:decimal:s";
        
        // Act
        var fields = FieldHelper.ParseFields(fieldsSpec);
        
        // Assert
        Assert.Equal(2, fields.Count);
        Assert.True(fields[0].IsSortable);
        Assert.True(fields[1].IsSortable);
    }
    
    [Fact]
    public void ParseFields_WithNoSortFlag_SetsNotSortable()
    {
        // Arrange
        var fieldsSpec = "Name:string:nosort Price:decimal:ns";
        
        // Act
        var fields = FieldHelper.ParseFields(fieldsSpec);
        
        // Assert
        Assert.Equal(2, fields.Count);
        Assert.False(fields[0].IsSortable);
        Assert.False(fields[1].IsSortable);
    }
    
    [Fact]
    public void ParseFields_WithFilterableFlag_SetsFilterable()
    {
        // Arrange
        var fieldsSpec = "InStock:bool:filterable Active:bool:f";
        
        // Act
        var fields = FieldHelper.ParseFields(fieldsSpec);
        
        // Assert
        Assert.Equal(2, fields.Count);
        Assert.True(fields[0].IsFilterable);
        Assert.True(fields[1].IsFilterable);
    }
    
    [Fact]
    public void ParseFields_WithMultipleFlags_SetsAllFlags()
    {
        // Arrange
        var fieldsSpec = "InStock:bool:sortable,filterable Active:bool:s,f";
        
        // Act
        var fields = FieldHelper.ParseFields(fieldsSpec);
        
        // Assert
        Assert.Equal(2, fields.Count);
        
        // Both should be sortable and filterable
        Assert.True(fields[0].IsSortable);
        Assert.True(fields[0].IsFilterable);
        Assert.True(fields[1].IsSortable);
        Assert.True(fields[1].IsFilterable);
    }
    
    [Fact]
    public void ParseFields_WithNoSortAndFilterable_SetsCorrectly()
    {
        // Arrange
        var fieldsSpec = "InStock:bool:nosort,filterable";
        
        // Act
        var fields = FieldHelper.ParseFields(fieldsSpec);
        
        // Assert
        var field = fields.Single();
        Assert.False(field.IsSortable);
        Assert.True(field.IsFilterable);
    }
    
    [Fact]
    public void ParseFields_WithNullableAndFlags_ParsesCorrectly()
    {
        // Arrange
        var fieldsSpec = "Description:string?:nosort InStock:bool?:filterable";
        
        // Act
        var fields = FieldHelper.ParseFields(fieldsSpec);
        
        // Assert
        Assert.Equal(2, fields.Count);
        
        // Description
        Assert.Equal("Description", fields[0].Name);
        Assert.Equal("string", fields[0].Type);
        Assert.True(fields[0].IsNullable);
        Assert.False(fields[0].IsSortable);
        Assert.False(fields[0].IsFilterable);
        
        // InStock
        Assert.Equal("InStock", fields[1].Name);
        Assert.Equal("bool", fields[1].Type);
        Assert.True(fields[1].IsNullable);
        Assert.True(fields[1].IsSortable); // Default
        Assert.True(fields[1].IsFilterable);
    }
    
    [Fact]
    public void ParseFields_WithInvalidFlag_ThrowsException()
    {
        // Arrange
        var fieldsSpec = "Name:string:invalidflag";
        
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => FieldHelper.ParseFields(fieldsSpec));
        Assert.Contains("Unknown flag 'invalidflag'", ex.Message);
    }
    
    [Fact]
    public void GenerateSortCases_WithAllSortable_GeneratesAllCases()
    {
        // Arrange
        var fields = new List<FieldDefinition>
        {
            new() { Name = "Name", Type = "string", IsSortable = true, IsNullable = false, IsRequired = true, IsFilterable = false },
            new() { Name = "Price", Type = "decimal", IsSortable = true, IsNullable = false, IsRequired = true, IsFilterable = false }
        };
        
        // Act
        var result = FieldHelper.GenerateSortCases(fields);
        
        // Assert
        Assert.Contains("\"name\"", result);
        Assert.Contains("\"price\"", result);
        Assert.Contains("OrderBy", result);
        Assert.Contains("OrderByDescending", result);
    }
    
    [Fact]
    public void GenerateSortCases_WithNonSortableFields_ExcludesThem()
    {
        // Arrange
        var fields = new List<FieldDefinition>
        {
            new() { Name = "Name", Type = "string", IsSortable = true, IsNullable = false, IsRequired = true, IsFilterable = false },
            new() { Name = "Description", Type = "string", IsSortable = false, IsNullable = false, IsRequired = true, IsFilterable = false },
            new() { Name = "Price", Type = "decimal", IsSortable = true, IsNullable = false, IsRequired = true, IsFilterable = false }
        };
        
        // Act
        var result = FieldHelper.GenerateSortCases(fields);
        
        // Assert
        Assert.Contains("\"name\"", result);
        Assert.Contains("\"price\"", result);
        Assert.DoesNotContain("\"description\"", result);
    }
    
    [Fact]
    public void GenerateSortCases_WithNoSortableFields_ReturnsComment()
    {
        // Arrange
        var fields = new List<FieldDefinition>
        {
            new() { Name = "Name", Type = "string", IsSortable = false, IsNullable = false, IsRequired = true, IsFilterable = false }
        };
        
        // Act
        var result = FieldHelper.GenerateSortCases(fields);
        
        // Assert
        Assert.Equal("// No sortable fields", result);
    }
    
    [Fact]
    public void GenerateFilterParameters_WithFilterableFields_GeneratesParameters()
    {
        // Arrange
        var fields = new List<FieldDefinition>
        {
            new() { Name = "InStock", Type = "bool", IsSortable = true, IsNullable = false, IsRequired = true, IsFilterable = true },
            new() { Name = "Active", Type = "bool", IsSortable = true, IsNullable = false, IsRequired = true, IsFilterable = true }
        };
        
        // Act
        var result = FieldHelper.GenerateFilterParameters(fields);
        
        // Assert
        Assert.Contains("bool? inStock = null", result);
        Assert.Contains("bool? active = null", result);
    }
    
    [Fact]
    public void GenerateFilterParameters_WithNonFilterableFields_ReturnsEmpty()
    {
        // Arrange
        var fields = new List<FieldDefinition>
        {
            new() { Name = "InStock", Type = "bool", IsSortable = true, IsNullable = false, IsRequired = true, IsFilterable = false }
        };
        
        // Act
        var result = FieldHelper.GenerateFilterParameters(fields);
        
        // Assert
        Assert.Empty(result);
    }
    
    [Fact]
    public void GenerateFilterControls_OnlyIncludesFilterableFields()
    {
        // Arrange
        var fields = new List<FieldDefinition>
        {
            new() { Name = "InStock", Type = "bool", IsSortable = true, IsNullable = false, IsRequired = true, IsFilterable = true },
            new() { Name = "Active", Type = "bool", IsSortable = true, IsNullable = false, IsRequired = true, IsFilterable = false }
        };
        
        // Act
        var result = FieldHelper.GenerateFilterControls(fields, "product");
        
        // Assert
        Assert.Contains("InStock", result);
        Assert.DoesNotContain("Active", result);
    }
}
