using AnonymousData;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Minded.Extensions.Logging.Configuration;
using Minded.Framework.CQRS.Abstractions;

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
        /// Tests that LogOutcomeEntries property can be set and retrieved.
        /// Verifies property getter and setter work correctly.
        /// </summary>
        [TestMethod]
        public void LogOutcomeEntries_CanBeSetAndRetrieved()
        {
            var options = new LoggingOptions();

            options.LogOutcomeEntries = true;

            options.LogOutcomeEntries.Should().BeTrue();
        }

        /// <summary>
        /// Tests that LogOutcomeEntries property defaults to false.
        /// Verifies default value for boolean property.
        /// </summary>
        [TestMethod]
        public void LogOutcomeEntries_DefaultsToFalse()
        {
            var options = new LoggingOptions();

            options.LogOutcomeEntries.Should().BeFalse();
        }

        /// <summary>
        /// Tests that MinimumOutcomeSeverityLevel property can be set and retrieved.
        /// Verifies property getter and setter work correctly.
        /// </summary>
        [TestMethod]
        public void MinimumOutcomeSeverityLevel_CanBeSetAndRetrieved()
        {
            var options = new LoggingOptions();

            options.MinimumOutcomeSeverityLevel = Severity.Error;

            options.MinimumOutcomeSeverityLevel.Should().Be(Severity.Error);
        }

        /// <summary>
        /// Tests that MinimumOutcomeSeverityLevel property defaults to Severity.Info.
        /// Verifies default value allows logging all outcome entries.
        /// </summary>
        [TestMethod]
        public void MinimumOutcomeSeverityLevel_DefaultsToInfo()
        {
            var options = new LoggingOptions();

            options.MinimumOutcomeSeverityLevel.Should().Be(Severity.Info);
        }

        /// <summary>
        /// Tests that MinimumOutcomeSeverityLevelProvider can be set and invoked.
        /// Verifies function provider works correctly.
        /// </summary>
        [TestMethod]
        public void MinimumOutcomeSeverityLevelProvider_CanBeSetAndInvoked()
        {
            var options = new LoggingOptions();
            var expectedSeverity = Severity.Warning;

            options.MinimumOutcomeSeverityLevelProvider = () => expectedSeverity;

            options.MinimumOutcomeSeverityLevelProvider.Should().NotBeNull();
            options.MinimumOutcomeSeverityLevelProvider().Should().Be(expectedSeverity);
        }

        /// <summary>
        /// Tests that MinimumOutcomeSeverityLevelProvider defaults to null.
        /// Verifies default value.
        /// </summary>
        [TestMethod]
        public void MinimumOutcomeSeverityLevelProvider_DefaultsToNull()
        {
            var options = new LoggingOptions();

            options.MinimumOutcomeSeverityLevelProvider.Should().BeNull();
        }

        /// <summary>
        /// Tests that GetEffectiveMinimumSeverityLevel returns provider value when set.
        /// Verifies provider takes precedence over static property.
        /// </summary>
        [TestMethod]
        public void GetEffectiveMinimumSeverityLevel_ReturnsProviderValue_WhenProviderIsSet()
        {
            var options = new LoggingOptions
            {
                MinimumOutcomeSeverityLevel = Severity.Info,
                MinimumOutcomeSeverityLevelProvider = () => Severity.Error
            };

            var result = options.GetEffectiveMinimumSeverityLevel();

            result.Should().Be(Severity.Error);
        }

        /// <summary>
        /// Tests that GetEffectiveMinimumSeverityLevel returns static value when provider is null.
        /// Verifies fallback to static property.
        /// </summary>
        [TestMethod]
        public void GetEffectiveMinimumSeverityLevel_ReturnsStaticValue_WhenProviderIsNull()
        {
            var options = new LoggingOptions
            {
                MinimumOutcomeSeverityLevel = Severity.Warning,
                MinimumOutcomeSeverityLevelProvider = null
            };

            var result = options.GetEffectiveMinimumSeverityLevel();

            result.Should().Be(Severity.Warning);
        }

        /// <summary>
        /// Tests that GetEffectiveMinimumSeverityLevel is called dynamically.
        /// Verifies that changing the provider's return value affects subsequent calls.
        /// This simulates feature flag behavior.
        /// </summary>
        [TestMethod]
        public void GetEffectiveMinimumSeverityLevel_IsDynamic_WhenProviderChanges()
        {
            var currentSeverity = Severity.Info;
            var options = new LoggingOptions
            {
                MinimumOutcomeSeverityLevelProvider = () => currentSeverity
            };

            var firstResult = options.GetEffectiveMinimumSeverityLevel();
            firstResult.Should().Be(Severity.Info);

            // Simulate feature flag change
            currentSeverity = Severity.Error;

            var secondResult = options.GetEffectiveMinimumSeverityLevel();
            secondResult.Should().Be(Severity.Error);
        }

        /// <summary>
        /// Tests that all properties can be set independently.
        /// Verifies properties are independent.
        /// </summary>
        [TestMethod]
        public void Properties_CanBeSetIndependently()
        {
            var options = new LoggingOptions
            {
                Enabled = true,
                LogMessageTemplateData = false,
                LogOutcomeEntries = true,
                MinimumOutcomeSeverityLevel = Severity.Warning
            };

            options.Enabled.Should().BeTrue();
            options.LogMessageTemplateData.Should().BeFalse();
            options.LogOutcomeEntries.Should().BeTrue();
            options.MinimumOutcomeSeverityLevel.Should().Be(Severity.Warning);
        }

        #region EnabledProvider Tests

        /// <summary>
        /// Tests that EnabledProvider can be set and invoked.
        /// Verifies function provider works correctly.
        /// </summary>
        [TestMethod]
        public void EnabledProvider_CanBeSetAndInvoked()
        {
            var options = new LoggingOptions();

            options.EnabledProvider = () => true;

            options.EnabledProvider.Should().NotBeNull();
            options.EnabledProvider().Should().BeTrue();
        }

        /// <summary>
        /// Tests that EnabledProvider defaults to null.
        /// Verifies default value.
        /// </summary>
        [TestMethod]
        public void EnabledProvider_DefaultsToNull()
        {
            var options = new LoggingOptions();

            options.EnabledProvider.Should().BeNull();
        }

        /// <summary>
        /// Tests that GetEffectiveEnabled returns provider value when set.
        /// Verifies provider takes precedence over static property.
        /// </summary>
        [TestMethod]
        public void GetEffectiveEnabled_ReturnsProviderValue_WhenProviderIsSet()
        {
            var options = new LoggingOptions
            {
                Enabled = false,
                EnabledProvider = () => true
            };

            var result = options.GetEffectiveEnabled();

            result.Should().BeTrue();
        }

        /// <summary>
        /// Tests that GetEffectiveEnabled returns static value when provider is null.
        /// Verifies fallback to static property.
        /// </summary>
        [TestMethod]
        public void GetEffectiveEnabled_ReturnsStaticValue_WhenProviderIsNull()
        {
            var options = new LoggingOptions
            {
                Enabled = true,
                EnabledProvider = null
            };

            var result = options.GetEffectiveEnabled();

            result.Should().BeTrue();
        }

        /// <summary>
        /// Tests that GetEffectiveEnabled is called dynamically.
        /// Verifies that changing the provider's return value affects subsequent calls.
        /// This simulates feature flag behavior.
        /// </summary>
        [TestMethod]
        public void GetEffectiveEnabled_IsDynamic_WhenProviderChanges()
        {
            var currentValue = false;
            var options = new LoggingOptions
            {
                EnabledProvider = () => currentValue
            };

            var firstResult = options.GetEffectiveEnabled();
            firstResult.Should().BeFalse();

            // Simulate feature flag change
            currentValue = true;

            var secondResult = options.GetEffectiveEnabled();
            secondResult.Should().BeTrue();
        }

        #endregion

        #region LogMessageTemplateDataProvider Tests

        /// <summary>
        /// Tests that LogMessageTemplateDataProvider can be set and invoked.
        /// Verifies function provider works correctly.
        /// </summary>
        [TestMethod]
        public void LogMessageTemplateDataProvider_CanBeSetAndInvoked()
        {
            var options = new LoggingOptions();

            options.LogMessageTemplateDataProvider = () => true;

            options.LogMessageTemplateDataProvider.Should().NotBeNull();
            options.LogMessageTemplateDataProvider().Should().BeTrue();
        }

        /// <summary>
        /// Tests that LogMessageTemplateDataProvider defaults to null.
        /// Verifies default value.
        /// </summary>
        [TestMethod]
        public void LogMessageTemplateDataProvider_DefaultsToNull()
        {
            var options = new LoggingOptions();

            options.LogMessageTemplateDataProvider.Should().BeNull();
        }

        /// <summary>
        /// Tests that GetEffectiveLogMessageTemplateData returns provider value when set.
        /// Verifies provider takes precedence over static property.
        /// </summary>
        [TestMethod]
        public void GetEffectiveLogMessageTemplateData_ReturnsProviderValue_WhenProviderIsSet()
        {
            var options = new LoggingOptions
            {
                LogMessageTemplateData = false,
                LogMessageTemplateDataProvider = () => true
            };

            var result = options.GetEffectiveLogMessageTemplateData();

            result.Should().BeTrue();
        }

        #endregion

        #region LogOutcomeEntriesProvider Tests

        /// <summary>
        /// Tests that LogOutcomeEntriesProvider can be set and invoked.
        /// Verifies function provider works correctly.
        /// </summary>
        [TestMethod]
        public void LogOutcomeEntriesProvider_CanBeSetAndInvoked()
        {
            var options = new LoggingOptions();

            options.LogOutcomeEntriesProvider = () => true;

            options.LogOutcomeEntriesProvider.Should().NotBeNull();
            options.LogOutcomeEntriesProvider().Should().BeTrue();
        }

        /// <summary>
        /// Tests that LogOutcomeEntriesProvider defaults to null.
        /// Verifies default value.
        /// </summary>
        [TestMethod]
        public void LogOutcomeEntriesProvider_DefaultsToNull()
        {
            var options = new LoggingOptions();

            options.LogOutcomeEntriesProvider.Should().BeNull();
        }

        /// <summary>
        /// Tests that GetEffectiveLogOutcomeEntries returns provider value when set.
        /// Verifies provider takes precedence over static property.
        /// </summary>
        [TestMethod]
        public void GetEffectiveLogOutcomeEntries_ReturnsProviderValue_WhenProviderIsSet()
        {
            var options = new LoggingOptions
            {
                LogOutcomeEntries = false,
                LogOutcomeEntriesProvider = () => true
            };

            var result = options.GetEffectiveLogOutcomeEntries();

            result.Should().BeTrue();
        }

        #endregion
    }
}

