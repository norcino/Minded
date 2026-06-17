using Microsoft.AspNetCore.Identity;
using MindedExample.Application.Common;
using MindedExample.Domain;

namespace MindedExample.Infrastructure.Persistence.Security
{
    /// <summary>
    /// Infrastructure implementation of <see cref="IPasswordService"/> that delegates
    /// password hashing and verification to ASP.NET Core's <see cref="IPasswordHasher{TUser}"/>.
    /// </summary>
    public class PasswordService : IPasswordService
    {
        private readonly IPasswordHasher<User> _hasher;

        /// <summary>Initializes a new <see cref="PasswordService"/>.</summary>
        public PasswordService(IPasswordHasher<User> hasher)
        {
            _hasher = hasher;
        }

        /// <inheritdoc />
        public string HashPassword(User user, string password)
            => _hasher.HashPassword(user, password);

        /// <inheritdoc />
        public bool VerifyPassword(User user, string hashedPassword, string providedPassword)
            => _hasher.VerifyHashedPassword(user, hashedPassword, providedPassword)
               != PasswordVerificationResult.Failed;
    }
}
