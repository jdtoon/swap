namespace carestream.core.interfaces.services
{
    public interface IPasswordHasherService
    {
        /// <summary>
        /// Hashes a password using a randomly generated salt.
        /// </summary>
        /// <param name="password">The password to hash.</param>
        /// <param name="salt">Output: The generated salt (Base64 encoded).</param>
        /// <returns>The hashed password (Base64 encoded).</returns>
        string HashPassword(string password, out string salt);

        /// <summary>
        /// Verifies a password against a stored hash and salt.
        /// </summary>
        /// <param name="password">The password to verify.</param>
        /// <param name="storedHash">The stored hashed password (Base64 encoded).</param>
        /// <param name="salt">The salt used when the password was hashed (Base64 encoded).</param>
        /// <returns>True if the password matches, false otherwise.</returns>
        bool VerifyPassword(string password, string storedHash, string salt);
    }
}