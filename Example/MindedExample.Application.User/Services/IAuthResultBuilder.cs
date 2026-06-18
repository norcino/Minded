using System.Threading;
using System.Threading.Tasks;
using MindedExample.Application.User.Command;

namespace MindedExample.Application.User.Services
{
    /// <summary>
    /// Builds a fully-populated <see cref="AuthResult"/> from a domain User.
    /// Shared across multiple command and query handlers that need to return authentication responses.
    /// </summary>
    public interface IAuthResultBuilder
    {
        /// <summary>
        /// Builds an <see cref="AuthResult"/> for the given user.
        /// </summary>
        /// <param name="user">The domain user entity.</param>
        /// <param name="accessToken">The JWT access token, or <c>null</c> for /me responses.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A fully populated <see cref="AuthResult"/>.</returns>
        Task<AuthResult> BuildAsync(
            MindedExample.Domain.User user,
            string accessToken,
            CancellationToken cancellationToken = default);
    }
}
