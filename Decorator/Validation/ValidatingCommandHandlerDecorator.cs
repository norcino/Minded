using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Minded.Common;

namespace Minded.Decorator.Validation
{
    public class ValidatingCommandHandlerDecorator<TCommand> : CommandHandlerDecoratorBase<TCommand>, ICommandHandler<TCommand>
        where TCommand : ICommand
    {
        private readonly ICommandValidator<TCommand> _commandValidator;
        private readonly ILogger<ValidatingCommandHandlerDecorator<TCommand>> _logger;
        private const string _validationFailureTemplate = "Validation Failure: {CommandValidatorName:l} Failures: {ValidationFailures:l}";
        private const string _logTemplate = "Validation started: {CommandValidatorName:l} - ";

        public ValidatingCommandHandlerDecorator(ICommandHandler<TCommand> commandHandler, 
            ILogger<ValidatingCommandHandlerDecorator<TCommand>> logger, 
            ICommandValidator<TCommand> commandValidator)
            : base(commandHandler)
        {
            _commandValidator = commandValidator;
            _logger = logger;
        }
        
        public async Task<ICommandResponse> HandleAsync(TCommand command)
        {
            ICommandResponse retVal;

            if (IsValidatingCommand(command))
            {
                var originalLogInfo = command.ToLog();
                
                var templateArguments = new List<object> { _commandValidator.GetType().Name };
                templateArguments.AddRange(originalLogInfo.LogMessageParameters);

                _logger.LogDebug(_logTemplate + originalLogInfo.LogMessageTemplate, templateArguments.ToArray(), _commandValidator.GetType().Name);

                var valResult = await _commandValidator.ValidateAsync(command);

                _logger.LogDebug("Validation {validationSuccess} for {CommandValidatorName:l}", valResult.IsValid, _commandValidator.GetType().Name);
                
                if (valResult.IsValid)
                {
                    retVal = await InnerCommandHandler.HandleAsync(command);
                }
                else
                {
                    _logger.LogInformation(_validationFailureTemplate, _commandValidator.GetType().Name, valResult.ValidationEntries.Select(e => e.ErrorMessage).ToArray());

                    retVal = new CommandResponse
                    {
                        Successful = false,
                        ValidationEntries = valResult.ValidationEntries.ToList()
                    };
                }
            }
            else
            {
                retVal = await InnerCommandHandler.HandleAsync(command);
            }

            return retVal;
        }

        /// <summary>
        /// We use TypeDescriptor so we can retrieve dynamically added attributes from the class type.
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        private static bool IsValidatingCommand(object command)
        {
            var retVal = false;
            var validateAttrib = TypeDescriptor.GetAttributes(command)[typeof(ValidateCommandAttribute)];

            if (validateAttrib != null)
            {
                retVal = ((ValidateCommandAttribute)validateAttrib).Validate;
            }

            return retVal;
        }
    }
}