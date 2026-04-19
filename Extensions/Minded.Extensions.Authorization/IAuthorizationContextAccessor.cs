namespace Minded.Extensions.Authorization
{
    /// <summary>
    /// Provides access to the current caller's <see cref="AuthorizationContext"/>.
    /// This is the sole authentication touchpoint for the authorization decorator.
    /// Consumers implement this interface to bridge their authentication mechanism
    /// (JWT, cookies, API keys, etc.) to the authorization system.
    /// </summary>
    public interface IAuthorizationContextAccessor
    {
        /// <summary>Gets the current caller's authorization context.</summary>
        AuthorizationContext Current { get; }
    }
}
