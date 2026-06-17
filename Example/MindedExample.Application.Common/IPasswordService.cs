namespace MindedExample.Application.Common
{
    /// <summary>
    /// Abstraction for password hashing and verification.
    /// Decouples the application layer from the specific password hashing implementation.
    /// </summary>
    public interface IPasswordService
    {
        /// <summary>
        /// Hashes a plain-text password for the given user.
        /// </summary>
        /// <param name="user">The user the password belongs to.</param>
        /// <param name="password">The plain-text password to hash.</param>
        /// <returns>The hashed password string.</returns>
        string HashPassword(MindedExample.Domain.User user, string password);

        /// <summary>
        /// Verifies a plain-text password against a stored hash.
        /// </summary>
        /// <param name="user">The user the password belongs to.</param>
        /// <param name="hashedPassword">The stored hashed password.</param>
        /// <param name="providedPassword">The plain-text password to verify.</param>
        /// <returns><c>true</c> if the password is correct; otherwise <c>false</c>.</returns>
        bool VerifyPassword(MindedExample.Domain.User user, string hashedPassword, string providedPassword);
    }
}
