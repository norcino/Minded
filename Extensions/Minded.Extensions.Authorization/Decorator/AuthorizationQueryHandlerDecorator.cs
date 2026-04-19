using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Minded.Extensions.Authorization.Configuration;
using Minded.Extensions.Exception;
using Minded.Framework.CQRS.Abstractions;
using Minded.Framework.CQRS.Query;
using Minded.Framework.Decorator;

namespace Minded.Extensions.Authorization.Decorator
{
    /// <summary>
    /// Authorization decorator for query handlers.
    /// Evaluates RBAC attributes before delegating to the inner handler, short-circuiting
    /// with appropriate error codes (401/403) when denied.
    /// For denied queries returning IQueryResponse&lt;T&gt;, returns QueryResponse&lt;T&gt;.Error(outcomeEntry).
    /// For denied queries returning raw types, throws SecurityException (403) or UnauthorizedAccessException (401).
    /// </summary>
    /// <typeparam name="TQuery">The query type being handled.</typeparam>
    /// <typeparam name="TResult">The result type returned by the query.</typeparam>
    public class AuthorizationQueryHandlerDecorator<TQuery, TResult>
        : QueryHandlerDecoratorBase<TQuery, TResult>, IQueryHandler<TQuery, TResult>
        where TQuery : IQuery<TResult>
    {
        private readonly IAuthorizationContextAccessor _contextAccessor;
        private readonly IRequestAuthorizationEvaluator _evaluator;
        private readonly IOptions<AuthorizationOptions> _options;
        private readonly ILogger _logger;
        private static readonly bool _isEnvelopeResponse;
        private static readonly Type _innerResultType;

        static AuthorizationQueryHandlerDecorator()
        {
            var resultType = typeof(TResult);

            // Check if TResult implements IQueryResponse<> (the generic interface)
            if (resultType.IsGenericType && resultType.GetGenericTypeDefinition() == typeof(IQueryResponse<>))
            {
                _isEnvelopeResponse = true;
                _innerResultType = resultType.GetGenericArguments()[0];
            }
            else
            {
                // Also check implemented interfaces
                var queryResponseInterface = resultType.GetInterfaces()
                    .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IQueryResponse<>));

                if (queryResponseInterface != null)
                {
                    _isEnvelopeResponse = true;
                    _innerResultType = queryResponseInterface.GetGenericArguments()[0];
                }
                else
                {
                    _isEnvelopeResponse = false;
                    _innerResultType = null;
                }
            }
        }

        public AuthorizationQueryHandlerDecorator(
            IQueryHandler<TQuery, TResult> queryHandler,
            IAuthorizationContextAccessor contextAccessor,
            IRequestAuthorizationEvaluator evaluator,
            IOptions<AuthorizationOptions> options,
            ILogger<AuthorizationQueryHandlerDecorator<TQuery, TResult>> logger)
            : base(queryHandler)
        {
            _contextAccessor = contextAccessor;
            _evaluator = evaluator;
            _options = options;
            _logger = logger;
        }

        public async Task<TResult> HandleAsync(TQuery query, CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var descriptor = AuthorizationDescriptorCache.GetOrCreate(typeof(TQuery));

                // AllowUnauthenticated always passes through
                if (descriptor.AllowUnauthenticated)
                {
                    return await DecoratedQueryHandler.HandleAsync(query, cancellationToken);
                }

                // Determine if authorization checks are needed
                bool enforceAuth = _options.Value.GetEffectiveRequireAuthenticationForAllQueries();
                bool needsAuth = descriptor.IsProtected || (enforceAuth && !descriptor.AllowUnauthenticated);

                if (!needsAuth)
                {
                    return await DecoratedQueryHandler.HandleAsync(query, cancellationToken);
                }

                // Get authorization context
                var context = _contextAccessor.Current;
                bool hasPrincipal = context?.HasPrincipal ?? false;

                if (!hasPrincipal)
                {
                    stopwatch.Stop();
                    LogDenied(query, stopwatch.Elapsed, isUnauthenticated: true);
                    return DenyUnauthenticated();
                }

                // If only authentication is required (no RBAC clauses), pass through
                if (descriptor.RequireAuthenticationOnly
                    && descriptor.RoleClauses.Count == 0
                    && descriptor.PermissionClauses.Count == 0)
                {
                    stopwatch.Stop();
                    LogAllowed(query, stopwatch.Elapsed);
                    return await DecoratedQueryHandler.HandleAsync(query, cancellationToken);
                }

                // If enforce-auth brought us here but there are no RBAC clauses, just check principal
                if (!descriptor.IsProtected && enforceAuth)
                {
                    stopwatch.Stop();
                    LogAllowed(query, stopwatch.Elapsed);
                    return await DecoratedQueryHandler.HandleAsync(query, cancellationToken);
                }

                // Evaluate RBAC
                var decision = _evaluator.Evaluate(typeof(TQuery), descriptor, context);

                if (!decision.Allowed)
                {
                    stopwatch.Stop();
                    LogDenied(query, stopwatch.Elapsed, isUnauthenticated: false);
                    return DenyUnauthorized();
                }

                stopwatch.Stop();
                LogAllowed(query, stopwatch.Elapsed);
                return await DecoratedQueryHandler.HandleAsync(query, cancellationToken);
            }
            catch (SecurityException)
            {
                throw;
            }
            catch (UnauthorizedAccessException)
            {
                throw;
            }
            catch (System.Exception)
            {
                // Deny-by-default: any exception in the auth flow results in denial
                stopwatch.Stop();
                LogDenied(query, stopwatch.Elapsed, isUnauthenticated: false);
                return DenyUnauthorized();
            }
        }

        private static TResult DenyUnauthenticated()
        {
            if (_isEnvelopeResponse)
            {
                return CreateEnvelopeError(CreateUnauthenticatedOutcomeEntry());
            }

            throw new UnauthorizedAccessException("Authentication required.");
        }

        private static TResult DenyUnauthorized()
        {
            if (_isEnvelopeResponse)
            {
                return CreateEnvelopeError(CreateUnauthorizedOutcomeEntry());
            }

            throw new SecurityException("Authorization failed.");
        }

        private static TResult CreateEnvelopeError(OutcomeEntry outcomeEntry)
        {
            // Use reflection to call QueryResponse<T>.Error(outcomeEntry)
            // where T is the inner type from IQueryResponse<T>
            var queryResponseType = typeof(QueryResponse<>).MakeGenericType(_innerResultType);
            var errorMethod = queryResponseType.GetMethod("Error", BindingFlags.Public | BindingFlags.Static);
            var outcomeArray = new IOutcomeEntry[] { outcomeEntry };
            var result = errorMethod.Invoke(null, new object[] { outcomeArray });
            return (TResult)result;
        }

        private void LogAllowed(TQuery query, TimeSpan duration)
        {
            _logger.LogInformation(
                "[Tracking:{TraceId}] {RequestName} - Authorization allowed in {Duration}",
                query.TraceId, typeof(TQuery).Name, duration);
        }

        private void LogDenied(TQuery query, TimeSpan duration, bool isUnauthenticated)
        {
            if (isUnauthenticated)
            {
                _logger.LogWarning(
                    "[Tracking:{TraceId}] {RequestName} - Unauthenticated access attempt in {Duration}",
                    query.TraceId, typeof(TQuery).Name, duration);
            }
            else
            {
                _logger.LogWarning(
                    "[Tracking:{TraceId}] {RequestName} - Unauthorized access attempt in {Duration}",
                    query.TraceId, typeof(TQuery).Name, duration);
            }
        }

        private static OutcomeEntry CreateUnauthenticatedOutcomeEntry()
        {
            return new OutcomeEntry(string.Empty, "Authentication required.", null, Severity.Error, GenericErrorCodes.NotAuthenticated);
        }

        private static OutcomeEntry CreateUnauthorizedOutcomeEntry()
        {
            return new OutcomeEntry(string.Empty, "Authorization failed.", null, Severity.Error, GenericErrorCodes.NotAuthorized);
        }
    }
}
