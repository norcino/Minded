using System;

namespace Minded.Extensions.Authorization.Attributes
{
    /// <summary>
    /// Explicitly marks a command or query as not requiring authentication,
    /// opting it out of the enforce-authentication policy when that policy is enabled.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class AllowUnauthenticatedAttribute : Attribute
    {
    }
}
