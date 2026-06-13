using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Minded.Extensions.Authorization.Configuration;
using Minded.Extensions.Context;
using Minded.Extensions.Exception;
using Minded.Framework.CQRS.Abstractions;
using Minded.Framework.CQRS.Command;
using Minded.Framework.CQRS.Query;
using Minded.Framework.Decorator;
using Minded.Framework.Mediator;

namespace Minded.Extensions.Authorization.Decorator
{
    /// <summary>
    /// Authorization decorator for command handlers returning <see cref="ICommandResponse"/>.
    /// Evaluates RBAC attributes before delegating to the inner handler, short-circuiting
    /// with appropriate error codes (401/403) when denied.
    /// </summary>
    /// <typeparam name="TCommand">The command type being handled.</typeparam>
    public class AuthorizationCommandHandlerDecorator<TCommand>
        : CommandHandlerDecoratorBase<TCommand>, ICommandHandler<TCommand>
        where TCommand : ICommand
    {
        private readonly IAuthorizationContextAccessor _contextAccessor;
        private readonly IRequestAuthorizationEvaluator _evaluator;
        private readonly IOptions<AuthorizationOptions> _options;
        private readonly IMindedContextAccessor _mindedContextAccessor;
        private readonly IMediator _mediator;
        private readonly ILogger _logger;

        private readonly struct AuthorizationBypass { }

        public AuthorizationCommandHandlerDecorator(
            ICommandHandler<TCommand> commandHandler,
            IAuthorizationContextAccessor contextAccessor,
            IRequestAuthorizationEvaluator evaluator,
            IOptions<AuthorizationOptions> options,
            IMindedContextAccessor mindedContextAccessor,
            IMediator mediator,
            ILogger<AuthorizationCommandHandlerDecorator<TCommand>> logger)
            : base(commandHandler)
        {
            _contextAccessor = contextAccessor;
            _evaluator = evaluator;
            _options = options;
            _mindedContextAccessor = mindedContextAccessor;
            _mediator = mediator;
            _logger = logger;
        }

        public async Task<ICommandResponse> HandleAsync(TCommand command, CancellationToken cancellationToken = default)
        {
            var denial = await EvaluateAuthorizationAsync(command, cancellationToken);
            if (denial != null)
            {
                return denial;
            }

            // Outside the deny-by-default guard: exceptions thrown by validators, the handler
            // or the data layer must propagate to the Exception decorator, not degrade to a 403.
            return await DecoratedCommmandHandler.HandleAsync(command, cancellationToken);
        }

        /// <summary>
        /// Runs the authorization flow under the deny-by-default guard.
        /// Returns null when the command is allowed to proceed, or the denial response otherwise.
        /// </summary>
        private async Task<ICommandResponse> EvaluateAuthorizationAsync(TCommand command, CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var mindedContext = _mindedContextAccessor.Current;
                if (mindedContext.TryGetScoped<AuthorizationBypass>(out _))
                {
                    return null;
                }

                var descriptor = AuthorizationDescriptorCache.GetOrCreate(typeof(TCommand));

                // AllowUnauthenticated always passes through
                if (descriptor.AllowUnauthenticated)
                {
                    return null;
                }

                // Determine if authorization checks are needed
                bool enforceAuth = _options.Value.GetEffectiveRequireAuthenticationForAllCommands();
                bool needsAuth = descriptor.IsProtected || (enforceAuth && !descriptor.AllowUnauthenticated);

                if (!needsAuth)
                {
                    return null;
                }

                // Get authorization context
                var context = _contextAccessor.Current;
                bool hasPrincipal = context?.HasPrincipal ?? false;

                if (!hasPrincipal)
                {
                    stopwatch.Stop();
                    LogDenied(command, stopwatch.Elapsed, isUnauthenticated: true);
                    return CommandResponse.Error(CreateUnauthenticatedOutcomeEntry());
                }

                // If only authentication is required (no RBAC clauses), pass through
                if (descriptor.RequireAuthenticationOnly
                    && descriptor.RoleClauses.Count == 0
                    && descriptor.PermissionClauses.Count == 0
                    && descriptor.ClaimClauses.Count == 0)
                {
                    stopwatch.Stop();
                    LogAllowed(command, stopwatch.Elapsed);
                    return null;
                }

                // If enforce-auth brought us here but there are no RBAC clauses, just check principal
                if (!descriptor.IsProtected && enforceAuth)
                {
                    stopwatch.Stop();
                    LogAllowed(command, stopwatch.Elapsed);
                    return null;
                }

                // Evaluate RBAC
                var decision = _evaluator.Evaluate(typeof(TCommand), descriptor, context);

                if (!decision.Allowed)
                {
                    stopwatch.Stop();
                    LogDenied(command, stopwatch.Elapsed, isUnauthenticated: false);
                    return CommandResponse.Error(CreateUnauthorizedOutcomeEntry());
                }

                if (!EvaluateMatchPropertyClaims(descriptor, command, context))
                {
                    stopwatch.Stop();
                    LogDenied(command, stopwatch.Elapsed, isUnauthenticated: false);
                    return CommandResponse.Error(CreateUnauthorizedOutcomeEntry());
                }

                if (!await EvaluateResourceClausesAsync(descriptor, command, context, mindedContext, cancellationToken))
                {
                    stopwatch.Stop();
                    LogDenied(command, stopwatch.Elapsed, isUnauthenticated: false);
                    return CommandResponse.Error(CreateUnauthorizedOutcomeEntry());
                }

                stopwatch.Stop();
                LogAllowed(command, stopwatch.Elapsed);
                return null;
            }
            catch (MindedContextRequiredException)
            {
                // Misconfiguration must surface; never silently degrade to a 403.
                throw;
            }
            catch (System.Exception)
            {
                // Deny-by-default: any exception in the authorization flow results in denial
                stopwatch.Stop();
                LogDenied(command, stopwatch.Elapsed, isUnauthenticated: false);
                return CommandResponse.Error(CreateUnauthorizedOutcomeEntry());
            }
        }

        private static void EnsureMindedContextAvailable(IMindedContext mindedContext)
        {
            if (mindedContext is NullMindedContext)
            {
                throw new MindedContextRequiredException(
                    $"Request '{typeof(TCommand).Name}' uses [RequireResourceAccess] which requires an active " +
                    "Minded context to install the recursion guard for the inner authorization query. " +
                    "Register the Minded context decorators on your MindedBuilder (for example by calling " +
                    "AddCommandContextDecorator() and AddQueryContextDecorator()) so that an IMindedContext " +
                    "is published for every mediator invocation.");
            }
        }

        private void LogAllowed(TCommand command, TimeSpan duration)
        {
            _logger.LogInformation(
                "[Tracking:{TraceId}] {RequestName} - Authorization allowed in {Duration}",
                command.TraceId, typeof(TCommand).Name, duration);
        }

        private void LogDenied(TCommand command, TimeSpan duration, bool isUnauthenticated)
        {
            if (isUnauthenticated)
            {
                _logger.LogWarning(
                    "[Tracking:{TraceId}] {RequestName} - Unauthenticated access attempt in {Duration}",
                    command.TraceId, typeof(TCommand).Name, duration);
            }
            else
            {
                _logger.LogWarning(
                    "[Tracking:{TraceId}] {RequestName} - Unauthorized access attempt in {Duration}",
                    command.TraceId, typeof(TCommand).Name, duration);
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

        private bool EvaluateMatchPropertyClaims(AuthorizationDescriptor descriptor, TCommand command, AuthorizationContext context)
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

                var property = typeof(TCommand).GetProperty(claimClause.MatchProperty);
                var propertyValue = property?.GetValue(command)?.ToString();
                if (!string.Equals((claimValue ?? string.Empty).Trim(), (propertyValue ?? string.Empty).Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            return true;
        }

        private async Task<bool> EvaluateResourceClausesAsync(
            AuthorizationDescriptor descriptor,
            TCommand command,
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

                var resourceProperty = typeof(TCommand).GetProperty(resourceClause.ResourceIdProperty);
                var resourceId = resourceProperty?.GetValue(command);

                var query = Activator.CreateInstance(resourceClause.QueryType, resourceId, claimValue);
                if (query == null)
                {
                    return false;
                }

                EnsureMindedContextAvailable(mindedContext);

                using (mindedContext.BeginScope(new AuthorizationBypass()))
                {
                    bool authorized = await DispatchAuthorizationQueryAsync(resourceClause.QueryType, query, cancellationToken);
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

    /// <summary>
    /// Authorization decorator for command handlers returning <see cref="ICommandResponse{TResult}"/>.
    /// Evaluates RBAC attributes before delegating to the inner handler, short-circuiting
    /// with appropriate error codes (401/403) when denied.
    /// </summary>
    /// <typeparam name="TCommand">The command type being handled.</typeparam>
    /// <typeparam name="TResult">The result type returned by the command.</typeparam>
    public class AuthorizationCommandHandlerDecorator<TCommand, TResult>
        : CommandHandlerDecoratorBase<TCommand, TResult>, ICommandHandler<TCommand, TResult>
        where TCommand : ICommand<TResult>
    {
        private readonly IAuthorizationContextAccessor _contextAccessor;
        private readonly IRequestAuthorizationEvaluator _evaluator;
        private readonly IOptions<AuthorizationOptions> _options;
        private readonly IMindedContextAccessor _mindedContextAccessor;
        private readonly IMediator _mediator;
        private readonly ILogger _logger;

        private readonly struct AuthorizationBypass { }

        public AuthorizationCommandHandlerDecorator(
            ICommandHandler<TCommand, TResult> commandHandler,
            IAuthorizationContextAccessor contextAccessor,
            IRequestAuthorizationEvaluator evaluator,
            IOptions<AuthorizationOptions> options,
            IMindedContextAccessor mindedContextAccessor,
            IMediator mediator,
            ILogger<AuthorizationCommandHandlerDecorator<TCommand, TResult>> logger)
            : base(commandHandler)
        {
            _contextAccessor = contextAccessor;
            _evaluator = evaluator;
            _options = options;
            _mindedContextAccessor = mindedContextAccessor;
            _mediator = mediator;
            _logger = logger;
        }

        public async Task<ICommandResponse<TResult>> HandleAsync(TCommand command, CancellationToken cancellationToken = default)
        {
            var denial = await EvaluateAuthorizationAsync(command, cancellationToken);
            if (denial != null)
            {
                return denial;
            }

            // Outside the deny-by-default guard: exceptions thrown by validators, the handler
            // or the data layer must propagate to the Exception decorator, not degrade to a 403.
            return await DecoratedCommmandHandler.HandleAsync(command, cancellationToken);
        }

        /// <summary>
        /// Runs the authorization flow under the deny-by-default guard.
        /// Returns null when the command is allowed to proceed, or the denial response otherwise.
        /// </summary>
        private async Task<ICommandResponse<TResult>> EvaluateAuthorizationAsync(TCommand command, CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var mindedContext = _mindedContextAccessor.Current;
                if (mindedContext.TryGetScoped<AuthorizationBypass>(out _))
                {
                    return null;
                }

                var descriptor = AuthorizationDescriptorCache.GetOrCreate(typeof(TCommand));

                // AllowUnauthenticated always passes through
                if (descriptor.AllowUnauthenticated)
                {
                    return null;
                }

                // Determine if authorization checks are needed
                bool enforceAuth = _options.Value.GetEffectiveRequireAuthenticationForAllCommands();
                bool needsAuth = descriptor.IsProtected || (enforceAuth && !descriptor.AllowUnauthenticated);

                if (!needsAuth)
                {
                    return null;
                }

                // Get authorization context
                var context = _contextAccessor.Current;
                bool hasPrincipal = context?.HasPrincipal ?? false;

                if (!hasPrincipal)
                {
                    stopwatch.Stop();
                    LogDenied(command, stopwatch.Elapsed, isUnauthenticated: true);
                    return CommandResponse<TResult>.Error(CreateUnauthenticatedOutcomeEntry());
                }

                // If only authentication is required (no RBAC clauses), pass through
                if (descriptor.RequireAuthenticationOnly
                    && descriptor.RoleClauses.Count == 0
                    && descriptor.PermissionClauses.Count == 0
                    && descriptor.ClaimClauses.Count == 0)
                {
                    stopwatch.Stop();
                    LogAllowed(command, stopwatch.Elapsed);
                    return null;
                }

                // If enforce-auth brought us here but there are no RBAC clauses, just check principal
                if (!descriptor.IsProtected && enforceAuth)
                {
                    stopwatch.Stop();
                    LogAllowed(command, stopwatch.Elapsed);
                    return null;
                }

                // Evaluate RBAC
                var decision = _evaluator.Evaluate(typeof(TCommand), descriptor, context);

                if (!decision.Allowed)
                {
                    stopwatch.Stop();
                    LogDenied(command, stopwatch.Elapsed, isUnauthenticated: false);
                    return CommandResponse<TResult>.Error(CreateUnauthorizedOutcomeEntry());
                }

                if (!EvaluateMatchPropertyClaims(descriptor, command, context))
                {
                    stopwatch.Stop();
                    LogDenied(command, stopwatch.Elapsed, isUnauthenticated: false);
                    return CommandResponse<TResult>.Error(CreateUnauthorizedOutcomeEntry());
                }

                if (!await EvaluateResourceClausesAsync(descriptor, command, context, mindedContext, cancellationToken))
                {
                    stopwatch.Stop();
                    LogDenied(command, stopwatch.Elapsed, isUnauthenticated: false);
                    return CommandResponse<TResult>.Error(CreateUnauthorizedOutcomeEntry());
                }

                stopwatch.Stop();
                LogAllowed(command, stopwatch.Elapsed);
                return null;
            }
            catch (MindedContextRequiredException)
            {
                // Misconfiguration must surface; never silently degrade to a 403.
                throw;
            }
            catch (System.Exception)
            {
                // Deny-by-default: any exception in the authorization flow results in denial
                stopwatch.Stop();
                LogDenied(command, stopwatch.Elapsed, isUnauthenticated: false);
                return CommandResponse<TResult>.Error(CreateUnauthorizedOutcomeEntry());
            }
        }

        private static void EnsureMindedContextAvailable(IMindedContext mindedContext)
        {
            if (mindedContext is NullMindedContext)
            {
                throw new MindedContextRequiredException(
                    $"Request '{typeof(TCommand).Name}' uses [RequireResourceAccess] which requires an active " +
                    "Minded context to install the recursion guard for the inner authorization query. " +
                    "Register the Minded context decorators on your MindedBuilder (for example by calling " +
                    "AddCommandContextDecorator() and AddQueryContextDecorator()) so that an IMindedContext " +
                    "is published for every mediator invocation.");
            }
        }

        private void LogAllowed(TCommand command, TimeSpan duration)
        {
            _logger.LogInformation(
                "[Tracking:{TraceId}] {RequestName} - Authorization allowed in {Duration}",
                command.TraceId, typeof(TCommand).Name, duration);
        }

        private void LogDenied(TCommand command, TimeSpan duration, bool isUnauthenticated)
        {
            if (isUnauthenticated)
            {
                _logger.LogWarning(
                    "[Tracking:{TraceId}] {RequestName} - Unauthenticated access attempt in {Duration}",
                    command.TraceId, typeof(TCommand).Name, duration);
            }
            else
            {
                _logger.LogWarning(
                    "[Tracking:{TraceId}] {RequestName} - Unauthorized access attempt in {Duration}",
                    command.TraceId, typeof(TCommand).Name, duration);
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

        private bool EvaluateMatchPropertyClaims(AuthorizationDescriptor descriptor, TCommand command, AuthorizationContext context)
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

                var property = typeof(TCommand).GetProperty(claimClause.MatchProperty);
                var propertyValue = property?.GetValue(command)?.ToString();
                if (!string.Equals((claimValue ?? string.Empty).Trim(), (propertyValue ?? string.Empty).Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            return true;
        }

        private async Task<bool> EvaluateResourceClausesAsync(
            AuthorizationDescriptor descriptor,
            TCommand command,
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

                var resourceProperty = typeof(TCommand).GetProperty(resourceClause.ResourceIdProperty);
                var resourceId = resourceProperty?.GetValue(command);

                var query = Activator.CreateInstance(resourceClause.QueryType, resourceId, claimValue);
                if (query == null)
                {
                    return false;
                }

                EnsureMindedContextAvailable(mindedContext);

                using (mindedContext.BeginScope(new AuthorizationBypass()))
                {
                    bool authorized = await DispatchAuthorizationQueryAsync(resourceClause.QueryType, query, cancellationToken);
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
