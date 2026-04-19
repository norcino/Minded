namespace Minded.Extensions.Authorization
{
    /// <summary>
    /// Represents the immutable result of an authorization evaluation.
    /// </summary>
    public sealed class AuthorizationDecision
    {
        /// <summary>Gets a value indicating whether the request is allowed.</summary>
        public bool Allowed { get; }

        /// <summary>Gets the internal reason for the decision, used for logging and testing.</summary>
        internal AuthorizationDecisionReason Reason { get; }

        private AuthorizationDecision(bool allowed, AuthorizationDecisionReason reason)
        {
            Allowed = allowed;
            Reason = reason;
        }

        /// <summary>Creates a decision indicating the request is allowed.</summary>
        public static AuthorizationDecision Allow() => new AuthorizationDecision(true, AuthorizationDecisionReason.Allowed);

        /// <summary>Creates a decision indicating the request is denied due to unsatisfied RBAC clauses.</summary>
        public static AuthorizationDecision Deny() => new AuthorizationDecision(false, AuthorizationDecisionReason.Denied);

        /// <summary>Creates a decision indicating the request is denied because no authenticated principal is present.</summary>
        public static AuthorizationDecision NoPrincipal() => new AuthorizationDecision(false, AuthorizationDecisionReason.NoPrincipal);
    }

    /// <summary>
    /// Distinguishes the reason for an authorization decision.
    /// </summary>
    internal enum AuthorizationDecisionReason
    {
        /// <summary>The request was allowed.</summary>
        Allowed,

        /// <summary>The request was denied because RBAC clauses were not satisfied.</summary>
        Denied,

        /// <summary>The request was denied because no authenticated principal was present.</summary>
        NoPrincipal
    }
}
