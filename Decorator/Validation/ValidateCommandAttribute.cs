using System;

namespace Minded.Decorator.Validation
{
    /// <summary>
    /// Attribute used by <see cref="ValidationCommandHandlerDecorator{TCommand}"/> to determine if a command requires validation
    /// </summary>
    public class ValidateCommandAttribute : Attribute    { }
}
