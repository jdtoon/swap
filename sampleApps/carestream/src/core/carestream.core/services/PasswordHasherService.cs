using System;
using System.Security.Cryptography;
using carestream.core.interfaces.services;
using Microsoft.Extensions.Logging; // For logging if needed

namespace carestream.core.services
{
    public class PasswordHasherService : IPasswordHasherService
    {
        private const int SaltSize = 16; // 128 bit
        private const int KeySize = 32;  // 256 bit
        private const int Iterations = 100000; // Number of iterations for PBKDF2
        private static readonly HashAlgorithmName _hashAlgorithmName = HashAlgorithmName.SHA256;
        private readonly ILogger<PasswordHasherService> _logger;

        public PasswordHasherService(ILogger<PasswordHasherService> logger)
        {
            _logger = logger;
        }

        public string HashPassword(string password, out string salt)
        {
            if (string.IsNullOrEmpty(password))
                throw new ArgumentNullException(nameof(password));

            byte[] saltBytes = RandomNumberGenerator.GetBytes(SaltSize);
            byte[] hashBytes = Rfc2898DeriveBytes.Pbkdf2(
                password,
                saltBytes,
                Iterations,
                _hashAlgorithmName,
                KeySize);

            salt = Convert.ToBase64String(saltBytes);
            return Convert.ToBase64String(hashBytes);
        }

        public bool VerifyPassword(string password, string storedHash, string salt)
        {
            if (string.IsNullOrEmpty(password))
                throw new ArgumentNullException(nameof(password));
            if (string.IsNullOrEmpty(storedHash))
                throw new ArgumentNullException(nameof(storedHash));
            if (string.IsNullOrEmpty(salt))
                throw new ArgumentNullException(nameof(salt));

            try
            {
                byte[] saltBytes = Convert.FromBase64String(salt);
                byte[] storedHashBytes = Convert.FromBase64String(storedHash);

                byte[] hashToVerifyBytes = Rfc2898DeriveBytes.Pbkdf2(
                    password,
                    saltBytes,
                    Iterations,
                    _hashAlgorithmName,
                    KeySize);

                return CryptographicOperations.FixedTimeEquals(hashToVerifyBytes, storedHashBytes);
            }
            catch (FormatException ex)
            {
                _logger.LogError(ex, "Error during password verification due to Base64 format issue.");
                return false; // Salt or hash was not a valid Base64 string
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during password verification.");
                return false;
            }
        }
    }
}