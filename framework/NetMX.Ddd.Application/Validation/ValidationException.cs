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

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationException"/> class.
    /// </summary>
    /// <param name="validationResult">The validation result containing error details.</param>
    public ValidationException(ValidationResult validationResult)
        : base("One or more validation errors occurred.")
    {
        ValidationResult = validationResult;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationException"/> class with a single error.
    /// </summary>
    /// <param name="propertyName">The name of the property that failed validation.</param>
    /// <param name="errorMessage">The validation error message.</param>
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
