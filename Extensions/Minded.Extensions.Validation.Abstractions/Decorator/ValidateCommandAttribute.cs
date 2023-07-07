using System;

namespace Minded.Extensions.Validation.Decorator
{
    /// <summary>
    /// Attribute used by <see cref="ValidationCommandHandlerDecorator{TCommand}"/> to determine if a command requires validation
    /// </summary>
    public class ValidateCommandAttribute : Attribute { }
}
