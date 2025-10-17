using NetMX.Identity.Core.Security;
using Xunit;

namespace NetMX.Identity.Core.Tests.Security;

public class PasswordHasherTests
{
    [Fact]
    public void HashPassword_CreatesNonEmptyHash()
    {
        // Arrange
        var hasher = new PasswordHasher();
        var password = "MySecurePassword123!";

        // Act
        var hash = hasher.HashPassword(password);

        // Assert
        Assert.NotNull(hash);
        Assert.NotEmpty(hash);
    }

    [Fact]
    public void HashPassword_CreatesDifferentHashesForSamePassword()
    {
        // Arrange
        var hasher = new PasswordHasher();
        var password = "MySecurePassword123!";

        // Act
        var hash1 = hasher.HashPassword(password);
        var hash2 = hasher.HashPassword(password);

        // Assert
        Assert.NotEqual(hash1, hash2); // Different salts
    }

    [Fact]
    public void VerifyPassword_ReturnsTrueForCorrectPassword()
    {
        // Arrange
        var hasher = new PasswordHasher();
        var password = "MySecurePassword123!";
        var hash = hasher.HashPassword(password);

        // Act
        var result = hasher.VerifyPassword(hash, password);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void VerifyPassword_ReturnsFalseForIncorrectPassword()
    {
        // Arrange
        var hasher = new PasswordHasher();
        var password = "MySecurePassword123!";
        var hash = hasher.HashPassword(password);

        // Act
        var result = hasher.VerifyPassword(hash, "WrongPassword");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void HashPassword_ThrowsForNullPassword()
    {
        // Arrange
        var hasher = new PasswordHasher();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => hasher.HashPassword(null!));
    }

    [Fact]
    public void VerifyPassword_ThrowsForNullHash()
    {
        // Arrange
        var hasher = new PasswordHasher();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => hasher.VerifyPassword(null!, "password"));
    }
}
