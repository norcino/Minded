using AnonymousData;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Minded.Framework.CQRS.Abstractions;

namespace Minded.Framework.CQRS.Tests
{
    /// <summary>
    /// Unit tests for the OutcomeEntry class.
    /// Tests all constructors, properties, and methods to ensure correct behavior.
    /// </summary>
    [TestClass]
    public class OutcomeEntryTests
    {
        #region Constructor Tests

        /// <summary>
        /// Tests the two-parameter constructor (propertyName, message).
        /// Verifies that PropertyName and Message are set correctly and AttemptedValue is null.
        /// </summary>
        [TestMethod]
        public void Constructor_WithPropertyNameAndMessage_SetsPropertiesCorrectly()
        {
            var propertyName = Any.String();
            var message = Any.String();

            var outcome = new OutcomeEntry(propertyName, message);

            outcome.PropertyName.Should().Be(propertyName);
            outcome.Message.Should().Be(message);
            outcome.AttemptedValue.Should().BeNull();
            outcome.Severity.Should().Be(Severity.Error);
            outcome.ErrorCode.Should().BeNull();
            outcome.ResourceName.Should().BeNull();
            outcome.UniqueErrorCode.Should().BeNull();
        }

        /// <summary>
        /// Tests the three-parameter constructor (propertyName, message, attemptedValue).
        /// Verifies that all three parameters are set correctly.
        /// </summary>
        [TestMethod]
        public void Constructor_WithPropertyNameMessageAndAttemptedValue_SetsPropertiesCorrectly()
        {
            var propertyName = Any.String();
            var message = Any.String();
            var attemptedValue = Any.Int();

            var outcome = new OutcomeEntry(propertyName, message, attemptedValue);

            outcome.PropertyName.Should().Be(propertyName);
            outcome.Message.Should().Be(message);
            outcome.AttemptedValue.Should().Be(attemptedValue);
            outcome.Severity.Should().Be(Severity.Error);
            outcome.ErrorCode.Should().BeNull();
        }

        /// <summary>
        /// Tests the full constructor with all parameters including severity and errorCode.
        /// Verifies that all parameters are set correctly.
        /// </summary>
        [TestMethod]
        public void Constructor_WithAllParameters_SetsPropertiesCorrectly()
        {
            var propertyName = Any.String();
            var message = Any.String();
            var attemptedValue = Any.String();
            var severity = Severity.Error;
            var errorCode = Any.String();

            var outcome = new OutcomeEntry(propertyName, message, attemptedValue, severity, errorCode);

            outcome.PropertyName.Should().Be(propertyName);
            outcome.Message.Should().Be(message);
            outcome.AttemptedValue.Should().Be(attemptedValue);
            outcome.Severity.Should().Be(severity);
            outcome.ErrorCode.Should().Be(errorCode);
        }

        /// <summary>
        /// Tests the full constructor with null errorCode.
        /// Verifies that ErrorCode remains null when null is passed.
        /// </summary>
        [TestMethod]
        public void Constructor_WithNullErrorCode_SetsErrorCodeToNull()
        {
            var propertyName = Any.String();
            var message = Any.String();
            var attemptedValue = Any.String();
            var severity = Severity.Warning;

            var outcome = new OutcomeEntry(propertyName, message, attemptedValue, severity, null);

            outcome.ErrorCode.Should().BeNull();
        }

        /// <summary>
        /// Tests the constructor with different severity levels.
        /// Verifies that each severity level is set correctly.
        /// </summary>
        [TestMethod]
        [DataRow(Severity.Info)]
        [DataRow(Severity.Warning)]
        [DataRow(Severity.Error)]
        public void Constructor_WithDifferentSeverityLevels_SetsSeverityCorrectly(Severity severity)
        {
            var propertyName = Any.String();
            var message = Any.String();
            var attemptedValue = Any.String();

            var outcome = new OutcomeEntry(propertyName, message, attemptedValue, severity);

            outcome.Severity.Should().Be(severity);
        }

        #endregion

        #region Property Tests

        /// <summary>
        /// Tests that Severity property can be set after construction.
        /// Verifies that the setter works correctly.
        /// </summary>
        [TestMethod]
        public void Severity_CanBeSetAfterConstruction()
        {
            var outcome = new OutcomeEntry(Any.String(), Any.String());

            outcome.Severity = Severity.Error;

            outcome.Severity.Should().Be(Severity.Error);
        }

        /// <summary>
        /// Tests that ErrorCode property can be set after construction.
        /// Verifies that the setter works correctly.
        /// </summary>
        [TestMethod]
        public void ErrorCode_CanBeSetAfterConstruction()
        {
            var outcome = new OutcomeEntry(Any.String(), Any.String());
            var errorCode = Any.String();

            outcome.ErrorCode = errorCode;

            outcome.ErrorCode.Should().Be(errorCode);
        }

        /// <summary>
        /// Tests that ResourceName property can be set after construction.
        /// Verifies that the setter works correctly.
        /// </summary>
        [TestMethod]
        public void ResourceName_CanBeSetAfterConstruction()
        {
            var outcome = new OutcomeEntry(Any.String(), Any.String());
            var resourceName = Any.String();

            outcome.ResourceName = resourceName;

            outcome.ResourceName.Should().Be(resourceName);
        }

        /// <summary>
        /// Tests that UniqueErrorCode property can be set after construction.
        /// Verifies that the setter works correctly.
        /// </summary>
        [TestMethod]
        public void UniqueErrorCode_CanBeSetAfterConstruction()
        {
            var outcome = new OutcomeEntry(Any.String(), Any.String());
            var uniqueErrorCode = Any.String();

            outcome.UniqueErrorCode = uniqueErrorCode;

            outcome.UniqueErrorCode.Should().Be(uniqueErrorCode);
        }

        #endregion

        #region ToString Tests

        /// <summary>
        /// Tests that ToString returns the Message property.
        /// Verifies that the ToString method works correctly.
        /// </summary>
        [TestMethod]
        public void ToString_ReturnsMessage()
        {
            var message = Any.String();
            var outcome = new OutcomeEntry(Any.String(), message);

            var result = outcome.ToString();

            result.Should().Be(message);
        }

        /// <summary>
        /// Tests ToString with empty message.
        /// Verifies that ToString returns empty string when message is empty.
        /// </summary>
        [TestMethod]
        public void ToString_WithEmptyMessage_ReturnsEmptyString()
        {
            var outcome = new OutcomeEntry(Any.String(), "");

            var result = outcome.ToString();

            result.Should().BeEmpty();
        }

        #endregion

        #region Edge Case Tests

        /// <summary>
        /// Tests constructor with null property name.
        /// Verifies that null values are handled correctly.
        /// </summary>
        [TestMethod]
        public void Constructor_WithNullPropertyName_SetsPropertyNameToNull()
        {
            var outcome = new OutcomeEntry(null, Any.String());

            outcome.PropertyName.Should().BeNull();
        }

        /// <summary>
        /// Tests constructor with null message.
        /// Verifies that null values are handled correctly.
        /// </summary>
        [TestMethod]
        public void Constructor_WithNullMessage_SetsMessageToNull()
        {
            var outcome = new OutcomeEntry(Any.String(), null);

            outcome.Message.Should().BeNull();
        }

        /// <summary>
        /// Tests constructor with complex object as attemptedValue.
        /// Verifies that complex objects can be stored as attempted values.
        /// </summary>
        [TestMethod]
        public void Constructor_WithComplexObjectAsAttemptedValue_StoresObjectCorrectly()
        {
            var complexObject = new { Name = Any.String(), Value = Any.Int() };

            var outcome = new OutcomeEntry(Any.String(), Any.String(), complexObject);

            outcome.AttemptedValue.Should().Be(complexObject);
        }

        #endregion
    }
}


