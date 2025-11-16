using FluentAssertions;
using Minded.Extensions.Retry.Configuration;

namespace Minded.Extensions.Retry.Tests
{
    /// <summary>
    /// Unit tests for RetryOptions configuration class.
    /// Verifies default values and delay calculation logic.
    /// </summary>
    [TestClass]
    public class RetryOptionsTests
    {
        [TestMethod]
        public void RetryOptions_HasCorrectDefaultValues()
        {
            var options = new RetryOptions();

            options.DefaultRetryCount.Should().Be(3);
            options.DefaultDelay1.Should().Be(0);
            options.DefaultDelay2.Should().Be(0);
            options.DefaultDelay3.Should().Be(0);
            options.DefaultDelay4.Should().Be(0);
            options.DefaultDelay5.Should().Be(0);
            options.ApplyToAllQueries.Should().BeFalse();
        }

        [TestMethod]
        public void GetDefaultDelayForIteration_ReturnsZero_WhenNoDelaysConfigured()
        {
            var options = new RetryOptions();

            options.GetDefaultDelayForIteration(1).Should().Be(0);
            options.GetDefaultDelayForIteration(2).Should().Be(0);
            options.GetDefaultDelayForIteration(3).Should().Be(0);
        }

        [TestMethod]
        public void GetDefaultDelayForIteration_ReturnsSameDelay_WhenOnlyDelay1Configured()
        {
            var options = new RetryOptions { DefaultDelay1 = 100 };

            options.GetDefaultDelayForIteration(1).Should().Be(100);
            options.GetDefaultDelayForIteration(2).Should().Be(100);
            options.GetDefaultDelayForIteration(3).Should().Be(100);
            options.GetDefaultDelayForIteration(4).Should().Be(100);
        }

        [TestMethod]
        public void GetDefaultDelayForIteration_ReturnsCorrectDelays_WhenMultipleDelaysConfigured()
        {
            var options = new RetryOptions
            {
                DefaultDelay1 = 100,
                DefaultDelay2 = 200,
                DefaultDelay3 = 300
            };

            options.GetDefaultDelayForIteration(1).Should().Be(100);
            options.GetDefaultDelayForIteration(2).Should().Be(200);
            options.GetDefaultDelayForIteration(3).Should().Be(300);
            options.GetDefaultDelayForIteration(4).Should().Be(300); // Uses last configured delay
            options.GetDefaultDelayForIteration(5).Should().Be(300); // Uses last configured delay
        }

        [TestMethod]
        public void GetDefaultDelayForIteration_ReturnsCorrectDelays_WhenAllDelaysConfigured()
        {
            var options = new RetryOptions
            {
                DefaultDelay1 = 100,
                DefaultDelay2 = 200,
                DefaultDelay3 = 300,
                DefaultDelay4 = 400,
                DefaultDelay5 = 500
            };

            options.GetDefaultDelayForIteration(1).Should().Be(100);
            options.GetDefaultDelayForIteration(2).Should().Be(200);
            options.GetDefaultDelayForIteration(3).Should().Be(300);
            options.GetDefaultDelayForIteration(4).Should().Be(400);
            options.GetDefaultDelayForIteration(5).Should().Be(500);
            options.GetDefaultDelayForIteration(6).Should().Be(500); // Uses last configured delay
        }

        [TestMethod]
        public void GetDefaultDelayForIteration_UsesLastDelay_ForIterationsBeyondFive()
        {
            var options = new RetryOptions
            {
                DefaultDelay1 = 100,
                DefaultDelay2 = 200,
                DefaultDelay3 = 300,
                DefaultDelay4 = 400,
                DefaultDelay5 = 500
            };

            options.GetDefaultDelayForIteration(6).Should().Be(500);
            options.GetDefaultDelayForIteration(7).Should().Be(500);
            options.GetDefaultDelayForIteration(10).Should().Be(500);
        }

        [TestMethod]
        public void ApplyToAllQueries_CanBeSetToTrue()
        {
            var options = new RetryOptions { ApplyToAllQueries = true };

            options.ApplyToAllQueries.Should().BeTrue();
        }

        [TestMethod]
        public void DefaultRetryCount_CanBeChanged()
        {
            var options = new RetryOptions { DefaultRetryCount = 5 };

            options.DefaultRetryCount.Should().Be(5);
        }

        [TestMethod]
        public void GetDefaultDelayForIteration_FallsBackCorrectly_WhenSomeDelaysNotConfigured()
        {
            var options = new RetryOptions
            {
                DefaultDelay1 = 100,
                DefaultDelay2 = 200
                // Delay3, Delay4, Delay5 not configured (remain 0)
            };

            options.GetDefaultDelayForIteration(1).Should().Be(100);
            options.GetDefaultDelayForIteration(2).Should().Be(200);
            options.GetDefaultDelayForIteration(3).Should().Be(200); // Falls back to Delay2
            options.GetDefaultDelayForIteration(4).Should().Be(200); // Falls back to Delay2
        }
    }
}

