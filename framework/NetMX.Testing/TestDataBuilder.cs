using Bogus;

namespace NetMX.Testing;

/// <summary>
/// Provides test data generation using Bogus library.
/// Simplifies creation of realistic test data.
/// </summary>
public static class TestDataBuilder
{
    private static readonly Randomizer Random = new();

    /// <summary>
    /// Creates a Faker instance for generating test data.
    /// </summary>
    /// <typeparam name="T">Type to generate</typeparam>
    /// <returns>Faker instance</returns>
    public static Faker<T> Create<T>() where T : class
    {
        return new Faker<T>();
    }

    /// <summary>
    /// Generates a random string suitable for entity names.
    /// </summary>
    /// <param name="prefix">Optional prefix</param>
    /// <returns>Random string</returns>
    public static string RandomEntityName(string? prefix = null)
    {
        var name = new Faker().Commerce.ProductName();
        return prefix != null ? $"{prefix}{name}" : name;
    }

    /// <summary>
    /// Generates a random email address.
    /// </summary>
    /// <returns>Email address</returns>
    public static string RandomEmail()
    {
        return new Faker().Internet.Email();
    }

    /// <summary>
    /// Generates a random phone number.
    /// </summary>
    /// <returns>Phone number</returns>
    public static string RandomPhone()
    {
        return new Faker().Phone.PhoneNumber();
    }

    /// <summary>
    /// Generates a random company name.
    /// </summary>
    /// <returns>Company name</returns>
    public static string RandomCompanyName()
    {
        return new Faker().Company.CompanyName();
    }

    /// <summary>
    /// Generates a random person name.
    /// </summary>
    /// <returns>Person name</returns>
    public static string RandomPersonName()
    {
        return new Faker().Name.FullName();
    }

    /// <summary>
    /// Generates a random address.
    /// </summary>
    /// <returns>Full address</returns>
    public static string RandomAddress()
    {
        return new Faker().Address.FullAddress();
    }

    /// <summary>
    /// Generates a random number between min and max.
    /// </summary>
    public static int RandomNumber(int min = 1, int max = 100)
    {
        return Random.Number(min, max);
    }

    /// <summary>
    /// Generates a random decimal between min and max.
    /// </summary>
    public static decimal RandomDecimal(decimal min = 1, decimal max = 1000)
    {
        return Random.Decimal(min, max);
    }

    /// <summary>
    /// Generates a random boolean.
    /// </summary>
    public static bool RandomBool()
    {
        return Random.Bool();
    }

    /// <summary>
    /// Generates a random date between start and end.
    /// </summary>
    public static DateTime RandomDate(DateTime? start = null, DateTime? end = null)
    {
        start ??= DateTime.UtcNow.AddYears(-1);
        end ??= DateTime.UtcNow;
        
        var range = (end.Value - start.Value).TotalDays;
        var randomDays = Random.Double() * range;
        return start.Value.AddDays(randomDays);
    }
}
