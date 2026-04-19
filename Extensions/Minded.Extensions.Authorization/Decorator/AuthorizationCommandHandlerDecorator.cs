using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Minded.Extensions.Authorization.Configuration;
using Minded.Extensions.Exception;
using Minded.Framework.CQRS.Abstractions;
using Minded.Framework.CQRS.Command;
using Minded.Framework.Decorator;

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
        private readonly ILogger _logger;

        public AuthorizationCommandHandlerDecorator(
            ICommandHandler<TCommand> commandHandler,
            IAuthorizationContextAccessor contextAccessor,
            IRequestAuthorizationEvaluator evaluator,
            IOptions<AuthorizationOptions> options,
            ILogger<AuthorizationCommandHandlerDecorator<TCommand>> logger)
            : base(commandHandler)
        {
            _contextAccessor = contextAccessor;
            _evaluator = evaluator;
            _options = options;
            _logger = logger;
        }

        public async Task<ICommandResponse> HandleAsync(TCommand command, CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var descriptor = AuthorizationDescriptorCache.GetOrCreate(typeof(TCommand));

                // AllowUnauthenticated always passes through
                if (descriptor.AllowUnauthenticated)
                {
                    return await DecoratedCommmandHandler.HandleAsync(command, cancellationToken);
                }

                // Determine if authorization checks are needed
                bool enforceAuth = _options.Value.GetEffectiveRequireAuthenticationForAllCommands();
                bool needsAuth = descriptor.IsProtected || (enforceAuth && !descriptor.AllowUnauthenticated);

                if (!needsAuth)
                {
                    return await DecoratedCommmandHandler.HandleAsync(command, cancellationToken);
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
                    && descriptor.PermissionClauses.Count == 0)
                {
                    stopwatch.Stop();
                    LogAllowed(command, stopwatch.Elapsed);
                    return await DecoratedCommmandHandler.HandleAsync(command, cancellationToken);
                }

                // If enforce-auth brought us here but there are no RBAC clauses, just check principal
                if (!descriptor.IsProtected && enforceAuth)
                {
                    stopwatch.Stop();
                    LogAllowed(command, stopwatch.Elapsed);
                    return await DecoratedCommmandHandler.HandleAsync(command, cancellationToken);
                }

                // Evaluate RBAC
                var decision = _evaluator.Evaluate(typeof(TCommand), descriptor, context);

                if (!decision.Allowed)
                {
                    stopwatch.Stop();
                    LogDenied(command, stopwatch.Elapsed, isUnauthenticated: false);
                    return CommandResponse.Error(CreateUnauthorizedOutcomeEntry());
                }

                stopwatch.Stop();
                LogAllowed(command, stopwatch.Elapsed);
                return await DecoratedCommmandHandler.HandleAsync(command, cancellationToken);
            }
            catch (System.Exception)
            {
                // Deny-by-default: any exception in the auth flow results in denial
                stopwatch.Stop();
                LogDenied(command, stopwatch.Elapsed, isUnauthenticated: false);
                return CommandResponse.Error(CreateUnauthorizedOutcomeEntry());
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
        private readonly ILogger _logger;

        public AuthorizationCommandHandlerDecorator(
            ICommandHandler<TCommand, TResult> commandHandler,
            IAuthorizationContextAccessor contextAccessor,
            IRequestAuthorizationEvaluator evaluator,
            IOptions<AuthorizationOptions> options,
            ILogger<AuthorizationCommandHandlerDecorator<TCommand, TResult>> logger)
            : base(commandHandler)
        {
            _contextAccessor = contextAccessor;
            _evaluator = evaluator;
            _options = options;
            _logger = logger;
        }

        public async Task<ICommandResponse<TResult>> HandleAsync(TCommand command, CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var descriptor = AuthorizationDescriptorCache.GetOrCreate(typeof(TCommand));

                // AllowUnauthenticated always passes through
                if (descriptor.AllowUnauthenticated)
                {
                    return await DecoratedCommmandHandler.HandleAsync(command, cancellationToken);
                }

                // Determine if authorization checks are needed
                bool enforceAuth = _options.Value.GetEffectiveRequireAuthenticationForAllCommands();
                bool needsAuth = descriptor.IsProtected || (enforceAuth && !descriptor.AllowUnauthenticated);

                if (!needsAuth)
                {
                    return await DecoratedCommmandHandler.HandleAsync(command, cancellationToken);
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
                    && descriptor.PermissionClauses.Count == 0)
                {
                    stopwatch.Stop();
                    LogAllowed(command, stopwatch.Elapsed);
                    return await DecoratedCommmandHandler.HandleAsync(command, cancellationToken);
                }

                // If enforce-auth brought us here but there are no RBAC clauses, just check principal
                if (!descriptor.IsProtected && enforceAuth)
                {
                    stopwatch.Stop();
                    LogAllowed(command, stopwatch.Elapsed);
                    return await DecoratedCommmandHandler.HandleAsync(command, cancellationToken);
                }

                // Evaluate RBAC
                var decision = _evaluator.Evaluate(typeof(TCommand), descriptor, context);

                if (!decision.Allowed)
                {
                    stopwatch.Stop();
                    LogDenied(command, stopwatch.Elapsed, isUnauthenticated: false);
                    return CommandResponse<TResult>.Error(CreateUnauthorizedOutcomeEntry());
                }

                stopwatch.Stop();
                LogAllowed(command, stopwatch.Elapsed);
                return await DecoratedCommmandHandler.HandleAsync(command, cancellationToken);
            }
            catch (System.Exception)
            {
                // Deny-by-default: any exception in the auth flow results in denial
                stopwatch.Stop();
                LogDenied(command, stopwatch.Elapsed, isUnauthenticated: false);
                return CommandResponse<TResult>.Error(CreateUnauthorizedOutcomeEntry());
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
    }
}
