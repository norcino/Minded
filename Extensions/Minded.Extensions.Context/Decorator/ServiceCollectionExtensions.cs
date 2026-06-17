using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Minded.Extensions.Configuration;

namespace Minded.Extensions.Context.Decorator
{
    /// <summary>
    /// Registration helpers that plug the context decorators into the Minded command and query pipelines
    /// and expose the ambient <see cref="IMindedContextAccessor"/> as a singleton.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers the command context decorator and the ambient accessor. Should be placed as the
        /// outermost decorator in the command pipeline so that every other decorator and the handler
        /// observe a populated context.
        /// </summary>
        /// <param name="builder">MindedBuilder instance.</param>
        /// <returns>MindedBuilder for fluent chaining.</returns>
        public static MindedBuilder AddCommandContextDecorator(this MindedBuilder builder)
        {
            RegisterAccessor(builder);

            builder.QueueCommandDecoratorRegistrationAction((b, i) => b.DecorateHandlerDescriptors(i, typeof(ContextCommandHandlerDecorator<>)));
            builder.QueueCommandWithResultDecoratorRegistrationAction((b, i) => b.DecorateHandlerDescriptors(i, typeof(ContextCommandHandlerDecorator<,>)));

            return builder;
        }

        /// <summary>
        /// Registers the query context decorator and the ambient accessor. Should be placed as the
        /// outermost decorator in the query pipeline so that every other decorator and the handler
        /// observe a populated context.
        /// </summary>
        /// <param name="builder">MindedBuilder instance.</param>
        /// <returns>MindedBuilder for fluent chaining.</returns>
        public static MindedBuilder AddQueryContextDecorator(this MindedBuilder builder)
        {
            RegisterAccessor(builder);

            builder.QueueQueryDecoratorRegistrationAction((b, i) => b.DecorateHandlerDescriptors(i, typeof(ContextQueryHandlerDecorator<,>)));

            return builder;
        }

        /// <summary>
        /// Convenience helper that registers both the command and the query context decorators and the
        /// ambient accessor in a single call.
        /// </summary>
        /// <param name="builder">MindedBuilder instance.</param>
        /// <returns>MindedBuilder for fluent chaining.</returns>
        public static MindedBuilder AddContextDecorator(this MindedBuilder builder)
        {
            builder.AddCommandContextDecorator();
            builder.AddQueryContextDecorator();
            return builder;
        }

        private static void RegisterAccessor(MindedBuilder builder)
        {
            builder.ServiceCollection.TryAddSingleton<MindedContextAccessor>();
            builder.ServiceCollection.TryAddSingleton<IMindedContextAccessor>(sp => sp.GetRequiredService<MindedContextAccessor>());
        }
    }
}
