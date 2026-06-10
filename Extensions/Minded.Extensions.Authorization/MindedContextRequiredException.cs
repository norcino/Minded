using System;

namespace Minded.Extensions.Authorization
{
    /// <summary>
    /// Thrown by the authorization decorators when a request marked with
    /// <see cref="Attributes.RequireResourceAccessAttribute"/> is processed but no ambient
    /// <see cref="Minded.Extensions.Context.IMindedContext"/> is available.
    /// Resource authorization dispatches an inner authorization query through the mediator and
    /// uses an async-flow-scoped marker on the Minded context to prevent infinite recursion.
    /// Without an active context this guard cannot be installed, so the decorator fails fast
    /// instead of risking unbounded recursion.
    /// </summary>
    /// <remarks>
    /// To resolve this exception, register the Minded context decorators on your
    /// <c>MindedBuilder</c> (for example by calling <c>AddCommandContextDecorator()</c> and
    /// <c>AddQueryContextDecorator()</c>) so that an <see cref="Minded.Extensions.Context.IMindedContext"/>
    /// is published for every mediator invocation.
    /// </remarks>
    public sealed class MindedContextRequiredException : InvalidOperationException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MindedContextRequiredException"/> class.
        /// </summary>
        /// <param name="message">A human-readable description of the misconfiguration.</param>
        public MindedContextRequiredException(string message) : base(message) { }
    }
}
