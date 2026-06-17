namespace MindedExample.Application.Common
{
    /// <summary>
    /// Abstraction for generating authentication tokens for users.
    /// Decouples the application layer from the JWT implementation details.
    /// </summary>
    public interface IJwtTokenService
    {
        /// <summary>
        /// Creates an access token for the specified user.
        /// </summary>
        /// <param name="user">The user to create the token for.</param>
        /// <returns>A signed access token string.</returns>
        string CreateAccessToken(MindedExample.Domain.User user);
    }
}
