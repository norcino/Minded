using AnonymousData;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Minded.Extensions.Logging.Configuration;

namespace Minded.Extensions.Logging.Tests
{
    /// <summary>
    /// Unit tests for LoggingOptions configuration class.
    /// Tests property initialization and mutation.
    /// </summary>
    [TestClass]
    public class LoggingOptionsTests
    {
        /// <summary>
        /// Tests that Enabled property can be set and retrieved.
        /// Verifies property getter and setter work correctly.
        /// </summary>
        [TestMethod]
        public void Enabled_CanBeSetAndRetrieved()
        {
            var options = new LoggingOptions();

            options.Enabled = true;

            options.Enabled.Should().BeTrue();
        }

        /// <summary>
        /// Tests that Enabled property defaults to false.
        /// Verifies default value for boolean property.
        /// </summary>
        [TestMethod]
        public void Enabled_DefaultsToFalse()
        {
            var options = new LoggingOptions();

            options.Enabled.Should().BeFalse();
        }

        /// <summary>
        /// Tests that LogMessageTemplateData property can be set and retrieved.
        /// Verifies property getter and setter work correctly.
        /// </summary>
        [TestMethod]
        public void LogMessageTemplateData_CanBeSetAndRetrieved()
        {
            var options = new LoggingOptions();

            options.LogMessageTemplateData = true;

            options.LogMessageTemplateData.Should().BeTrue();
        }

        /// <summary>
        /// Tests that LogMessageTemplateData property defaults to false.
        /// Verifies default value for boolean property.
        /// </summary>
        [TestMethod]
        public void LogMessageTemplateData_DefaultsToFalse()
        {
            var options = new LoggingOptions();

            options.LogMessageTemplateData.Should().BeFalse();
        }

        /// <summary>
        /// Tests that both properties can be set independently.
        /// Verifies properties are independent.
        /// </summary>
        [TestMethod]
        public void Properties_CanBeSetIndependently()
        {
            var options = new LoggingOptions
            {
                Enabled = true,
                LogMessageTemplateData = false
            };

            options.Enabled.Should().BeTrue();
            options.LogMessageTemplateData.Should().BeFalse();
        }
    }
}

