using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using FluentAssertions;
using Minded.Extensions.Configuration;
using Minded.Extensions.Context.Decorator;
using Moq;

namespace Minded.Extensions.Context.Tests
{
    /// <summary>
    /// Verifies that the registration helpers expose the accessor as a singleton and queue the correct
    /// decorator registration actions on <see cref="MindedBuilder"/>.
    /// </summary>
    [TestClass]
    public class ServiceCollectionExtensionsTests
    {
        private MindedBuilder NewBuilder() => new MindedBuilder(new ServiceCollection(), new Mock<IConfiguration>().Object, a => true);

        [TestMethod]
        public void AddCommandContextDecorator_registers_accessor_as_singleton()
        {
            var builder = NewBuilder();

            builder.AddCommandContextDecorator();

            builder.ServiceCollection.Should().ContainSingle(d =>
                d.ServiceType == typeof(IMindedContextAccessor) && d.Lifetime == ServiceLifetime.Singleton);
            builder.ServiceCollection.Should().ContainSingle(d =>
                d.ServiceType == typeof(MindedContextAccessor) && d.Lifetime == ServiceLifetime.Singleton);
        }

        [TestMethod]
        public void AddCommandContextDecorator_queues_command_decorators()
        {
            var builder = NewBuilder();

            builder.AddCommandContextDecorator();

            builder.QueuedCommandDecoratorsRegistrationAction.Should().HaveCount(1);
            builder.QueuedCommandWithResultDecoratorsRegistrationAction.Should().HaveCount(1);
            builder.QueuedQueryDecoratorsRegistrationAction.Should().BeEmpty();
        }

        [TestMethod]
        public void AddQueryContextDecorator_queues_only_query_decorator()
        {
            var builder = NewBuilder();

            builder.AddQueryContextDecorator();

            builder.QueuedQueryDecoratorsRegistrationAction.Should().HaveCount(1);
            builder.QueuedCommandDecoratorsRegistrationAction.Should().BeEmpty();
            builder.QueuedCommandWithResultDecoratorsRegistrationAction.Should().BeEmpty();
        }

        [TestMethod]
        public void AddContextDecorator_queues_command_and_query_decorators()
        {
            var builder = NewBuilder();

            builder.AddContextDecorator();

            builder.QueuedCommandDecoratorsRegistrationAction.Should().HaveCount(1);
            builder.QueuedCommandWithResultDecoratorsRegistrationAction.Should().HaveCount(1);
            builder.QueuedQueryDecoratorsRegistrationAction.Should().HaveCount(1);
        }

        [TestMethod]
        public void Accessor_registration_is_idempotent_when_helpers_are_combined()
        {
            var builder = NewBuilder();

            builder.AddCommandContextDecorator();
            builder.AddQueryContextDecorator();

            builder.ServiceCollection.Count(d => d.ServiceType == typeof(MindedContextAccessor)).Should().Be(1);
            builder.ServiceCollection.Count(d => d.ServiceType == typeof(IMindedContextAccessor)).Should().Be(1);
        }
    }
}
