using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Minded.Extensions.Decorator;
using Minded.Framework.CQRS.Command;

namespace Minded.Extensions.Validation.Decorator
{
    /// <summary>
    /// Decorator responsible to determine if the Command requires validation, checking if it has the <see cref="ValidateCommandAttribute"/>.
    /// If the validation does not fail it will invoke the next <see cref="ICommandHandler{TCommand}"/> registered implementation
    /// </summary>
    /// <typeparam name="TCommand">Generic type if the <see cref="ICommand"/> implementation handled by the handler currently decorated</typeparam>
    public class ValidatingCommandHandlerDecorator<TCommand> : CommandHandlerDecoratorBase<TCommand>, ICommandHandler<TCommand>
        where TCommand : ICommand
    {
        private readonly ICommandValidator<TCommand> _commandValidator;
        private readonly ILogger<ValidatingCommandHandlerDecorator<TCommand>> _logger;
        private const string _validationFailureTemplate = "Validation Failure: {CommandValidatorName:l} Failures: {ValidationFailures:l}";
        private const string _debugOutcomeLogTemplate = "Validation {validationSuccess} for {CommandValidatorName:l}";
        private const string _logTemplate = "Validation started: {CommandValidatorName:l} - ";

        public ValidatingCommandHandlerDecorator(ICommandHandler<TCommand> commandHandler,
            ILogger<ValidatingCommandHandlerDecorator<TCommand>> logger,
            ICommandValidator<TCommand> commandValidator)
            : base(commandHandler)
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
            if (IsValidatingCommand(command))
            {
                var templateArguments = new List<object> { _commandValidator.GetType().Name };
                
                _logger.LogDebug(_logTemplate, templateArguments.ToArray(), _commandValidator.GetType().Name);

                var valResult = await _commandValidator.ValidateAsync(command);

                _logger.LogDebug(_debugOutcomeLogTemplate, valResult.IsValid, _commandValidator.GetType().Name);

                if (!valResult.IsValid)
                {
                    _logger.LogInformation(_validationFailureTemplate, _commandValidator.GetType().Name, valResult.ValidationEntries.Select(e => e.Message).ToArray());

                    return new CommandResponse
                    {
                        Successful = false,
                        OutcomeEntries = valResult.ValidationEntries.ToList()
                    };
                }
            }

            return await InnerCommandHandler.HandleAsync(command);
        }

        /// <summary>
        /// Determine if the Command requires validation
        /// </summary>
        /// <param name="command">Subject Command</param>
        /// <returns>True if the command requires validation</returns>
        private static bool IsValidatingCommand(object command)
        {
            return TypeDescriptor.GetAttributes(command)[typeof(ValidateCommandAttribute)] != null;
        }
    }
}
