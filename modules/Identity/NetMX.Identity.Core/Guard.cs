namespace NetMX.Identity.Core;

/// <summary>
/// Guard clauses for validation.
/// </summary>
public static class Guard
{
    public static string NotNullOrEmpty(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"{paramName} cannot be null or empty.", paramName);

        return value;
    }

    public static T NotNull<T>(T value, string paramName) where T : class
    {
        if (value == null)
            throw new ArgumentNullException(paramName);

        return value;
    }
}
