using System.ComponentModel.DataAnnotations;

namespace NetMX.Ddd.Application.Validation;

/// <summary>
/// Base validator class that uses Data Annotations for validation.
/// Extend this class to add custom validation rules.
/// </summary>
/// <typeparam name="T">The type of object to validate.</typeparam>
public abstract class Validator<T> : IValidator<T>
{
    /// <summary>
    /// Validates the object using Data Annotations and custom rules.
    /// </summary>
    public virtual ValidationResult Validate(T instance)
    {
        var result = new ValidationResult();

        // Data Annotations validation
        var validationContext = new ValidationContext(instance!);
        var dataAnnotationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        
        if (!System.ComponentModel.DataAnnotations.Validator.TryValidateObject(
            instance!, validationContext, dataAnnotationResults, validateAllProperties: true))
        {
            foreach (var error in dataAnnotationResults)
            {
                var propertyName = error.MemberNames.FirstOrDefault() ?? string.Empty;
                result.AddError(propertyName, error.ErrorMessage ?? "Validation failed");
            }
        }

        // Custom validation rules
        ValidateCustomRules(instance, result);

        return result;
    }

    /// <summary>
    /// Asynchronously validates the object.
    /// </summary>
    public virtual Task<ValidationResult> ValidateAsync(T instance, CancellationToken cancellationToken = default)
    {
        var result = Validate(instance);
        return Task.FromResult(result);
    }

    /// <summary>
    /// Override this method to add custom validation rules.
    /// </summary>
    /// <param name="instance">The object being validated.</param>
    /// <param name="result">The validation result to add errors to.</param>
    protected virtual void ValidateCustomRules(T instance, ValidationResult result)
    {
        // Override in derived classes to add custom validation
    }
}
