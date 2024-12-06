using System;
using System.Linq;
using System.Reflection;
using Minded.Extensions.Configuration;
using Minded.Framework.CQRS.Command;
using Minded.Framework.CQRS.Query;

namespace Minded.Extensions.Validation.Decorator
{
    public static class ServiceCollectionExtensions
    {
        private static Func<Type, Type> CommandValidatorTypeGenerator = (t) =>
        {
            var interfaceType = t.GetInterfaces().FirstOrDefault(i => i.GetTypeInfo().IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommandHandler<>));
            return typeof(ICommandValidator<>).MakeGenericType(interfaceType.GetGenericArguments());
        };

        private static Func<Type, Type> CommandWithResultValidatorTypeGenerator = (t) =>
        {
            var interfaceType = t.GetInterfaces().FirstOrDefault(i => i.GetTypeInfo().IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommandHandler<,>));
            return typeof(ICommandValidator<>).MakeGenericType(interfaceType.GetGenericArguments().First());
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

            builder.QueueCommandWithResultDecoratorRegistrationAction((b, i) => {
                b.DecorateHandlerDescriptors(i, typeof(ValidatingCommandHandlerDecorator<,>), typeof(ValidateCommandAttribute), CommandWithResultValidatorTypeGenerator);
            });

            return builder;
        }

        private static Func<Type, Type> QueryWithResultValidatorTypeGenerator = (t) =>
        {
            var interfaceType = t.GetInterfaces().FirstOrDefault(i => i.GetTypeInfo().IsGenericType && i.GetGenericTypeDefinition() == typeof(IQueryHandler<,>));
            return typeof(IQueryValidator<,>).MakeGenericType(interfaceType.GetGenericArguments());
        };

        /// <summary>
        /// Register the Validator classes used to validate queries and entities
        /// </summary>
        public static MindedBuilder AddQueryValidationDecorator(this MindedBuilder builder)
        {
            builder.RegisterAllTypesInServiceAssembliesImplementingInterface(typeof(IQueryValidator<,>));
            builder.RegisterAllTypesInServiceAssembliesImplementingInterface(typeof(IValidator<>));

            builder.QueueQueryDecoratorRegistrationAction((b, i) => {
                b.DecorateHandlerDescriptors(i, typeof(ValidatingQueryHandlerDecorator<,>), typeof(ValidateQueryAttribute), QueryWithResultValidatorTypeGenerator);
            });

            return builder;
        }
    }
}
