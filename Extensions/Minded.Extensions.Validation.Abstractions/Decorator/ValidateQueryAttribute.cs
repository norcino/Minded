using System;

namespace Minded.Extensions.Validation.Decorator
{
    /// <summary>
    /// Attribute used by <see cref="ValidationQueryHandlerDecorator{TQuery}"/> to determine if a query requires validation
    /// </summary>
    public class ValidateQueryAttribute : Attribute { }
}
