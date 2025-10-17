namespace NetMX.Ddd.Application.Validation;

/// <summary>
/// Exception thrown when validation fails.
/// </summary>
public class ValidationException : Exception
{
    /// <summary>
    /// Gets the validation result containing all errors.
    /// </summary>
    public ValidationResult ValidationResult { get; }

    public ValidationException(ValidationResult validationResult)
        : base("One or more validation errors occurred.")
    {
        ValidationResult = validationResult;
    }

    public ValidationException(string propertyName, string errorMessage)
        : this(ValidationResult.Failure(propertyName, errorMessage))
    {
    }

    /// <summary>
    /// Gets a dictionary of errors grouped by property name.
    /// </summary>
    public Dictionary<string, List<string>> GetErrorsDictionary()
    {
        return ValidationResult.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).ToList()
            );
    }
}
