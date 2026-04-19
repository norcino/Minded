using System;

namespace Minded.Extensions.Authorization.Attributes
{
    /// <summary>
    /// Marks a command or query as requiring an authenticated caller (HasPrincipal must be true)
    /// without imposing any specific role or permission requirements.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class RequireAuthenticationAttribute : Attribute
    {
    }
}
