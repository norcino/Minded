using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Minded.Extensions.Authorization.Configuration;
using Minded.Extensions.Context;
using Minded.Extensions.Exception;
using Minded.Framework.CQRS.Abstractions;
using Minded.Framework.CQRS.Query;
using Minded.Framework.Decorator;
using Minded.Framework.Mediator;

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
        private readonly IMindedContextAccessor _mindedContextAccessor;
        private readonly IMediator _mediator;
        private readonly ILogger _logger;
        private static readonly bool _isEnvelopeResponse;
        private static readonly Type _innerResultType;

        private readonly struct AuthorizationBypass { }

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
            IMindedContextAccessor mindedContextAccessor,
            IMediator mediator,
            ILogger<AuthorizationQueryHandlerDecorator<TQuery, TResult>> logger)
            : base(queryHandler)
        {
            _contextAccessor = contextAccessor;
            _evaluator = evaluator;
            _options = options;
            _mindedContextAccessor = mindedContextAccessor;
            _mediator = mediator;
            _logger = logger;
        }

        public async Task<TResult> HandleAsync(TQuery query, CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var mindedContext = _mindedContextAccessor.Current;
                if (mindedContext.TryGetScoped<AuthorizationBypass>(out _))
                {
                    return await DecoratedQueryHandler.HandleAsync(query, cancellationToken);
                }

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
                    && descriptor.PermissionClauses.Count == 0
                    && descriptor.ClaimClauses.Count == 0)
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

                if (!EvaluateMatchPropertyClaims(descriptor, query, context))
                {
                    stopwatch.Stop();
                    LogDenied(query, stopwatch.Elapsed, isUnauthenticated: false);
                    return DenyUnauthorized();
                }

                if (!await EvaluateResourceClausesAsync(descriptor, query, context, mindedContext, cancellationToken))
                {
                    stopwatch.Stop();
                    LogDenied(query, stopwatch.Elapsed, isUnauthenticated: false);
                    return DenyUnauthorized();
                }

                stopwatch.Stop();
                LogAllowed(query, stopwatch.Elapsed);
                return await DecoratedQueryHandler.HandleAsync(query, cancellationToken);
            }
            catch (MindedContextRequiredException)
            {
                // Misconfiguration must surface; never silently degrade to a denial.
                throw;
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

        private static void EnsureMindedContextAvailable(IMindedContext mindedContext)
        {
            if (mindedContext is NullMindedContext)
            {
                throw new MindedContextRequiredException(
                    $"Request '{typeof(TQuery).Name}' uses [RequireResourceAccess] which requires an active " +
                    "Minded context to install the recursion guard for the inner authorization query. " +
                    "Register the Minded context decorators on your MindedBuilder (for example by calling " +
                    "AddCommandContextDecorator() and AddQueryContextDecorator()) so that an IMindedContext " +
                    "is published for every mediator invocation.");
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

        private bool EvaluateMatchPropertyClaims(AuthorizationDescriptor descriptor, TQuery query, AuthorizationContext context)
        {
            foreach (var claimClause in descriptor.ClaimClauses)
            {
                if (string.IsNullOrWhiteSpace(claimClause.MatchProperty))
                {
                    continue;
                }

                if (IsOrClauseSatisfied(claimClause.OrAnyRole, claimClause.OrAnyPermission, claimClause.OrAnyClaim, context))
                {
                    continue;
                }

                if (!context.Claims.TryGetValue(claimClause.ClaimType, out var claimValue))
                {
                    return false;
                }

                var property = typeof(TQuery).GetProperty(claimClause.MatchProperty);
                var propertyValue = property?.GetValue(query)?.ToString();
                if (!string.Equals((claimValue ?? string.Empty).Trim(), (propertyValue ?? string.Empty).Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            return true;
        }

        private async Task<bool> EvaluateResourceClausesAsync(
            AuthorizationDescriptor descriptor,
            TQuery query,
            AuthorizationContext context,
            IMindedContext mindedContext,
            CancellationToken cancellationToken)
        {
            foreach (var resourceClause in descriptor.ResourceClauses)
            {
                if (IsOrClauseSatisfied(resourceClause.OrAnyRole, resourceClause.OrAnyPermission, resourceClause.OrAnyClaim, context))
                {
                    continue;
                }

                if (!context.Claims.TryGetValue(resourceClause.ResourceIdClaim, out var claimValue))
                {
                    return false;
                }

                var resourceProperty = typeof(TQuery).GetProperty(resourceClause.ResourceIdProperty);
                var resourceId = resourceProperty?.GetValue(query);

                var authQuery = Activator.CreateInstance(resourceClause.QueryType, resourceId, claimValue);
                if (authQuery == null)
                {
                    return false;
                }

                EnsureMindedContextAvailable(mindedContext);

                using (mindedContext.BeginScope(new AuthorizationBypass()))
                {
                    bool authorized = await DispatchAuthorizationQueryAsync(resourceClause.QueryType, authQuery, cancellationToken);
                    if (!authorized)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private async Task<bool> DispatchAuthorizationQueryAsync(Type queryType, object queryInstance, CancellationToken cancellationToken)
        {
            var queryInterface = queryType.GetInterfaces().First(i =>
                i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IQuery<>));
            var resultType = queryInterface.GetGenericArguments()[0];

            var method = typeof(IMediator)
                .GetMethod(nameof(IMediator.ProcessQueryAsync))
                .MakeGenericMethod(resultType);

            var taskObject = (Task)method.Invoke(_mediator, new[] { queryInstance, (object)cancellationToken });
            await taskObject.ConfigureAwait(false);

            var resultObject = taskObject.GetType().GetProperty("Result", BindingFlags.Public | BindingFlags.Instance)?.GetValue(taskObject);
            if (resultType == typeof(bool))
            {
                return resultObject is bool granted && granted;
            }

            if (resultObject == null)
            {
                return false;
            }

            var successful = (bool)(resultObject.GetType().GetProperty(nameof(IQueryResponse<bool>.Successful))?.GetValue(resultObject) ?? false);
            var grantedValue = (bool)(resultObject.GetType().GetProperty(nameof(IQueryResponse<bool>.Result))?.GetValue(resultObject) ?? false);
            return successful && grantedValue;
        }

        private static bool IsOrClauseSatisfied(
            IReadOnlyList<string> orAnyRole,
            IReadOnlyList<string> orAnyPermission,
            IReadOnlyList<string> orAnyClaim,
            AuthorizationContext context)
        {
            if (MatchesAny(orAnyRole, context.Roles) || MatchesAny(orAnyPermission, context.Permissions))
            {
                return true;
            }

            if (orAnyClaim != null)
            {
                for (int i = 0; i < orAnyClaim.Count; i++)
                {
                    var key = orAnyClaim[i]?.Trim();
                    if (!string.IsNullOrEmpty(key) && context.Claims.ContainsKey(key))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool MatchesAny(IReadOnlyList<string> requiredItems, IReadOnlyCollection<string> contextItems)
        {
            if (requiredItems == null || requiredItems.Count == 0)
            {
                return false;
            }

            var normalized = new HashSet<string>(contextItems.Select(item => item.Trim()), StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < requiredItems.Count; i++)
            {
                var item = requiredItems[i]?.Trim();
                if (!string.IsNullOrEmpty(item) && normalized.Contains(item))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
