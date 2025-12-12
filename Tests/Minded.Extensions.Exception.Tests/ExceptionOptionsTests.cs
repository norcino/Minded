using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Minded.Extensions.Exception.Configuration;

namespace Minded.Extensions.Exception.Tests
{
    /// <summary>
    /// Unit tests for ExceptionOptions class.
    /// Tests configuration properties and effective value resolution.
    /// </summary>
    [TestClass]
    public class ExceptionOptionsTests
    {
        /// <summary>
        /// Tests that Serialize property defaults to true.
        /// Verifies backward compatibility - serialization enabled by default.
        /// </summary>
        [TestMethod]
        public void Serialize_DefaultsToTrue()
        {
            var options = new ExceptionOptions();

            options.Serialize.Should().BeTrue();
        }

        /// <summary>
        /// Tests that Serialize property can be set to false.
        /// Verifies serialization can be disabled.
        /// </summary>
        [TestMethod]
        public void Serialize_CanBeSetToFalse()
        {
            var options = new ExceptionOptions { Serialize = false };

            options.Serialize.Should().BeFalse();
        }

        /// <summary>
        /// Tests that SerializeProvider defaults to null.
        /// Verifies no provider is set by default.
        /// </summary>
        [TestMethod]
        public void SerializeProvider_DefaultsToNull()
        {
            var options = new ExceptionOptions();

            options.SerializeProvider.Should().BeNull();
        }

        /// <summary>
        /// Tests that SerializeProvider can be set and invoked.
        /// Verifies function provider works correctly.
        /// </summary>
        [TestMethod]
        public void SerializeProvider_CanBeSetAndInvoked()
        {
            var options = new ExceptionOptions();
            bool expectedValue = false;

            options.SerializeProvider = () => expectedValue;

            options.SerializeProvider.Should().NotBeNull();
            options.SerializeProvider().Should().Be(expectedValue);
        }

        /// <summary>
        /// Tests that GetEffectiveSerialize returns Serialize value when provider is null.
        /// Verifies static value is used when no provider is set.
        /// </summary>
        [TestMethod]
        public void GetEffectiveSerialize_WhenProviderIsNull_ReturnsSerializeValue()
        {
            var options = new ExceptionOptions { Serialize = false };

            bool result = options.GetEffectiveSerialize();

            result.Should().BeFalse();
        }

        /// <summary>
        /// Tests that GetEffectiveSerialize returns provider value when provider is set.
        /// Verifies provider takes precedence over static value.
        /// </summary>
        [TestMethod]
        public void GetEffectiveSerialize_WhenProviderIsSet_ReturnsProviderValue()
        {
            var options = new ExceptionOptions
            {
                Serialize = true, // Static value is true
                SerializeProvider = () => false // But provider returns false
            };

            bool result = options.GetEffectiveSerialize();

            result.Should().BeFalse();
        }

        /// <summary>
        /// Tests that GetEffectiveSerialize returns true when both are true.
        /// Verifies provider precedence with matching values.
        /// </summary>
        [TestMethod]
        public void GetEffectiveSerialize_WhenBothAreTrue_ReturnsTrue()
        {
            var options = new ExceptionOptions
            {
                Serialize = true,
                SerializeProvider = () => true
            };

            bool result = options.GetEffectiveSerialize();

            result.Should().BeTrue();
        }

        /// <summary>
        /// Tests that GetEffectiveSerialize can dynamically change based on provider.
        /// Verifies provider is evaluated each time.
        /// </summary>
        [TestMethod]
        public void GetEffectiveSerialize_WithDynamicProvider_ReflectsCurrentValue()
        {
            bool shouldSerialize = true;
            var options = new ExceptionOptions
            {
                SerializeProvider = () => shouldSerialize
            };

            // First call - should return true
            options.GetEffectiveSerialize().Should().BeTrue();

            // Change the value
            shouldSerialize = false;

            // Second call - should return false
            options.GetEffectiveSerialize().Should().BeFalse();
        }
    }
}

