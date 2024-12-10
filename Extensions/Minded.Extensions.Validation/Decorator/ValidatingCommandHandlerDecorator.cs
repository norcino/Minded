using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Minded.Framework.CQRS.Abstractions;
using Minded.Framework.CQRS.Command;
using Minded.Framework.Decorator;

namespace Minded.Extensions.Validation.Decorator
{
    internal static class Shared
    {
        internal const string ValidationFailureTemplate = "Validation Failure: {CommandValidatorName:l} Failures: {ValidationFailures:l}";
        internal const string DebugOutcomeLogTemplate = "Validation {validationSuccess} for {CommandValidatorName:l}";
        internal const string LogTemplate = "Validation started: {CommandValidatorName:l} - ";

        /// <summary>
        /// Determine if the Command requires validation
        /// </summary>
        /// <param name="command">Subject Command</param>
        /// <returns>True if the command requires validation</returns>
        internal static bool IsValidatingCommand(object command)
        {
            return TypeDescriptor.GetAttributes(command)[typeof(ValidateCommandAttribute)] != null;
        }
    }

    /// <summary>
    /// Decorator responsible to determine if the Command requires validation, checking if it has the <see cref="ValidateCommandAttribute"/>.
    /// If the validation does not fail it will invoke the next <see cref="ICommandHandler{TCommand}"/> registered implementation
    /// </summary>
    /// <typeparam name="TCommand">Generic type if the <see cref="ICommand"/> implementation handled by the handler currently decorated</typeparam>
    public class ValidatingCommandHandlerDecorator<TCommand> : CommandHandlerDecoratorBase<TCommand>, ICommandHandler<TCommand> where TCommand : ICommand
    {
        private readonly ICommandValidator<TCommand> _commandValidator;
        private readonly ILogger _logger;
        
        public ValidatingCommandHandlerDecorator(ICommandHandler<TCommand> commandHandler, ILogger<ValidatingCommandHandlerDecorator<TCommand>> logger,
            ICommandValidator<TCommand> commandValidator) : base(commandHandler)
        {
            _commandValidator = commandValidator;
            _logger = logger;
        }

        /// <summary>
        /// Execute the command asynchronously returning an instance of ICommandResponse
        /// </summary>
        /// <param name="command">Subject Command</param>
        /// <returns>An instance of <see cref="ICommandResponse"/> representing the output of the command</returns>
        public async Task<ICommandResponse> HandleAsync(TCommand command)
        {
            if (!Shared.IsValidatingCommand(command))
            {
                return await InnerCommandHandler.HandleAsync(command);
            }

            _logger.LogDebug(Shared.LogTemplate, _commandValidator.GetType().Name);

            var valResult = await _commandValidator.ValidateAsync(command);

            _logger.LogDebug(Shared.DebugOutcomeLogTemplate, valResult.IsValid, _commandValidator.GetType().Name);

            if (!valResult.IsValid)
            {
                _logger.LogInformation(Shared.ValidationFailureTemplate, _commandValidator.GetType().Name, valResult.OutcomeEntries.Select(e => e.Message).ToArray());

                return new CommandResponse
                {
                    OutcomeEntries = valResult.OutcomeEntries.ToList(),
                    Successful = valResult.IsValid
                };
            }

            var result = await InnerCommandHandler.HandleAsync(command);

            if (result.OutcomeEntries == null)
            {
                result.OutcomeEntries = new List<IOutcomeEntry>();
            }

            result.OutcomeEntries.AddRange(valResult.OutcomeEntries);
            return result;
        }
    }

    public class ValidatingCommandHandlerDecorator<TCommand, TResult> : CommandHandlerDecoratorBase<TCommand, TResult>, ICommandHandler<TCommand, TResult> where TCommand : ICommand<TResult>
    {
        private readonly ICommandValidator<TCommand> _commandValidator;
        private readonly ILogger<ValidatingCommandHandlerDecorator<TCommand, TResult>> _logger;

        public ValidatingCommandHandlerDecorator(ICommandHandler<TCommand, TResult> commandHandler,
            ILogger<ValidatingCommandHandlerDecorator<TCommand, TResult>> logger, ICommandValidator<TCommand> commandValidator) : base(commandHandler)
        {
            _commandValidator = commandValidator;
            _logger = logger;
        }

        /// <summary>
        /// Execute the command asynchronously returning an instance of ICommandResponse
        /// </summary>
        /// <param name="command">Subject Command</param>
        /// <returns>An instance of <see cref="ICommandResponse{TResult}"/> representing the output of the command</returns>
        public async Task<ICommandResponse<TResult>> HandleAsync(TCommand command)
        {
            if (!Shared.IsValidatingCommand(command))
            {
                return await InnerCommandHandler.HandleAsync(command);
            }

            _logger.LogDebug(Shared.LogTemplate, _commandValidator.GetType().Name);

            var valResult = await _commandValidator.ValidateAsync(command);

            _logger.LogDebug(Shared.DebugOutcomeLogTemplate, valResult.IsValid, _commandValidator.GetType().Name);

            if (valResult.IsValid)
            {
                return await InnerCommandHandler.HandleAsync(command);
            }

            _logger.LogInformation(Shared.ValidationFailureTemplate, _commandValidator.GetType().Name, valResult.OutcomeEntries.Select(e => e.Message).ToArray());

            return new CommandResponse<TResult>
            {
                Successful = false,
                OutcomeEntries = valResult.OutcomeEntries.ToList()
            };
        }
    }
}
