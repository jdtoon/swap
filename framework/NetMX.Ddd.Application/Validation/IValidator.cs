namespace NetMX.Ddd.Application.Validation;

/// <summary>
/// Interface for object validators.
/// </summary>
/// <typeparam name="T">The type of object to validate.</typeparam>
public interface IValidator<in T>
{
    /// <summary>
    /// Validates the specified object.
    /// </summary>
    /// <param name="instance">The object to validate.</param>
    /// <returns>A validation result containing any errors.</returns>
    ValidationResult Validate(T instance);

    /// <summary>
    /// Asynchronously validates the specified object.
    /// </summary>
    /// <param name="instance">The object to validate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A validation result containing any errors.</returns>
    Task<ValidationResult> ValidateAsync(T instance, CancellationToken cancellationToken = default);
}
