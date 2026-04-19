using System;

namespace Minded.Extensions.Authorization
{
    /// <summary>
    /// Defines the contract for evaluating whether an <see cref="AuthorizationContext"/>
    /// satisfies an <see cref="AuthorizationDescriptor"/>.
    /// </summary>
    public interface IRequestAuthorizationEvaluator
    {
        /// <summary>
        /// Evaluates the authorization context against the descriptor for the given request type.
        /// </summary>
        /// <param name="requestType">The type of the command or query being authorized.</param>
        /// <param name="descriptor">The compiled authorization descriptor for the request type.</param>
        /// <param name="context">The current caller's authorization context.</param>
        /// <returns>An <see cref="AuthorizationDecision"/> indicating whether the request is allowed or denied.</returns>
        AuthorizationDecision Evaluate(Type requestType, AuthorizationDescriptor descriptor, AuthorizationContext context);
    }
}
