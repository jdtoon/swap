using NetMX.Ddd.Application.Validation;
using System.ComponentModel.DataAnnotations;

namespace NetMX.Ddd.Application.Tests.Validation;

public class ValidationTests
{
    [Fact]
    public void Validate_WithNoErrors_ReturnsValidResult()
    {
        // Arrange
        var validator = new TestValidator();
        var obj = new TestObject { Name = "Valid", Age = 25 };

        // Act
        var result = validator.Validate(obj);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_WithDataAnnotationErrors_ReturnsInvalidResult()
    {
        // Arrange
        var validator = new TestValidator();
        var obj = new TestObject { Name = "", Age = 25 }; // Name is required

        // Act
        var result = validator.Validate(obj);

        // Assert
        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.Equal("Name", result.Errors[0].PropertyName);
        Assert.Contains("required", result.Errors[0].ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_WithCustomRuleErrors_ReturnsInvalidResult()
    {
        // Arrange
        var validator = new TestValidator();
        var obj = new TestObject { Name = "Test", Age = 150 }; // Age > 100

        // Act
        var result = validator.Validate(obj);

        // Assert
        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.Equal("Age", result.Errors[0].PropertyName);
        Assert.Equal("Age must be between 0 and 100", result.Errors[0].ErrorMessage);
    }

    [Fact]
    public void Validate_WithMultipleErrors_ReturnsAllErrors()
    {
        // Arrange
        var validator = new TestValidator();
        var obj = new TestObject { Name = "", Age = 150 }; // Both invalid

        // Act
        var result = validator.Validate(obj);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(2, result.Errors.Count);
    }

    [Fact]
    public async Task ValidateAsync_WorksCorrectly()
    {
        // Arrange
        var validator = new TestValidator();
        var obj = new TestObject { Name = "Valid", Age = 25 };

        // Act
        var result = await validator.ValidateAsync(obj);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidationException_ContainsValidationResult()
    {
        // Arrange
        var validationResult = NetMX.Ddd.Application.Validation.ValidationResult.Failure("Name", "Name is required");

        // Act
        var exception = new NetMX.Ddd.Application.Validation.ValidationException(validationResult);

        // Assert
        Assert.Equal(validationResult, exception.ValidationResult);
        Assert.Single(exception.ValidationResult.Errors);
    }

    [Fact]
    public void ValidationException_GetErrorsDictionary_GroupsByProperty()
    {
        // Arrange
        var validationResult = new NetMX.Ddd.Application.Validation.ValidationResult();
        validationResult.AddError("Name", "Name is required");
        validationResult.AddError("Name", "Name must be at least 3 characters");
        validationResult.AddError("Age", "Age must be positive");
        
        var exception = new NetMX.Ddd.Application.Validation.ValidationException(validationResult);

        // Act
        var errors = exception.GetErrorsDictionary();

        // Assert
        Assert.Equal(2, errors.Count);
        Assert.Equal(2, errors["Name"].Count);
        Assert.Single(errors["Age"]);
    }

    // Test helpers
    private class TestObject
    {
        [Required]
        public string Name { get; set; } = string.Empty;
        
        public int Age { get; set; }
    }

    private class TestValidator : Validator<TestObject>
    {
        protected override void ValidateCustomRules(TestObject instance, NetMX.Ddd.Application.Validation.ValidationResult result)
        {
            if (instance.Age < 0 || instance.Age > 100)
            {
                result.AddError(nameof(instance.Age), "Age must be between 0 and 100");
            }
        }
    }
}
