using System;
using System.Linq;
using System.Reflection;
using Minded.Extensions.Configuration;
using Minded.Framework.CQRS.Command;

namespace Minded.Extensions.Validation.Decorator
{
    public static class ServiceCollectionExtensions
    {
        private static Func<Type, Type> CommandValidatorTypeGenerator = (t) =>
        {
            var interfaceType = t.GetInterfaces().FirstOrDefault(i => i.GetTypeInfo().IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommandHandler<>));
            return typeof(ICommandValidator<>).MakeGenericType(interfaceType.GetGenericArguments());
        };

        /// <summary>
        /// Register the Validator classes used to validate commands and entities
        /// </summary>
        public static MindedBuilder AddCommandValidationDecorator(this MindedBuilder builder)
        {
            builder.RegisterAllTypesInServiceAssembliesImplementingInterface(typeof(ICommandValidator<>));
            builder.RegisterAllTypesInServiceAssembliesImplementingInterface(typeof(IValidator<>));

            builder.QueueCommandDecoratorRegistrationAction((b, i) => {
                b.DecorateHandlerDescriptors(i, typeof(ValidatingCommandHandlerDecorator<>), typeof(ValidateCommandAttribute), CommandValidatorTypeGenerator);
            });
            return builder;
        }
    }
}
