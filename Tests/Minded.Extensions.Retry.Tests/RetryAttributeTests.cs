using FluentAssertions;
using Minded.Extensions.Retry.Decorator;

namespace Minded.Extensions.Retry.Tests
{
    /// <summary>
    /// Unit tests for RetryCommandAttribute and RetryQueryAttribute.
    /// Verifies delay calculation logic for different retry iterations.
    /// </summary>
    [TestClass]
    public class RetryAttributeTests
    {
        [TestMethod]
        public void RetryCommandAttribute_GetDelayForIteration_ReturnsZero_WhenNoDelaysSpecified()
        {
            var attribute = new RetryCommandAttribute(3);

            attribute.GetDelayForIteration(1).Should().Be(0);
            attribute.GetDelayForIteration(2).Should().Be(0);
            attribute.GetDelayForIteration(3).Should().Be(0);
        }

        [TestMethod]
        public void RetryCommandAttribute_GetDelayForIteration_ReturnsSameDelay_WhenOnlyOneDelaySpecified()
        {
            var attribute = new RetryCommandAttribute(3, 100);

            attribute.GetDelayForIteration(1).Should().Be(100);
            attribute.GetDelayForIteration(2).Should().Be(100);
            attribute.GetDelayForIteration(3).Should().Be(100);
            attribute.GetDelayForIteration(4).Should().Be(100);
        }

        [TestMethod]
        public void RetryCommandAttribute_GetDelayForIteration_ReturnsCorrectDelays_WhenMultipleDelaysSpecified()
        {
            var attribute = new RetryCommandAttribute(5, 100, 200, 300);

            attribute.GetDelayForIteration(1).Should().Be(100);
            attribute.GetDelayForIteration(2).Should().Be(200);
            attribute.GetDelayForIteration(3).Should().Be(300);
            attribute.GetDelayForIteration(4).Should().Be(300); // Uses last specified delay
            attribute.GetDelayForIteration(5).Should().Be(300); // Uses last specified delay
        }

        [TestMethod]
        public void RetryCommandAttribute_GetDelayForIteration_ReturnsCorrectDelays_WhenAllDelaysSpecified()
        {
            var attribute = new RetryCommandAttribute(5, 100, 200, 300, 400, 500);

            attribute.GetDelayForIteration(1).Should().Be(100);
            attribute.GetDelayForIteration(2).Should().Be(200);
            attribute.GetDelayForIteration(3).Should().Be(300);
            attribute.GetDelayForIteration(4).Should().Be(400);
            attribute.GetDelayForIteration(5).Should().Be(500);
            attribute.GetDelayForIteration(6).Should().Be(500); // Uses last specified delay for iterations beyond 5
        }

        [TestMethod]
        public void RetryCommandAttribute_GetDelayForIteration_UsesLastDelay_ForIterationsBeyondFive()
        {
            var attribute = new RetryCommandAttribute(10, 100, 200, 300, 400, 500);

            attribute.GetDelayForIteration(6).Should().Be(500);
            attribute.GetDelayForIteration(7).Should().Be(500);
            attribute.GetDelayForIteration(10).Should().Be(500);
        }

        [TestMethod]
        public void RetryQueryAttribute_GetDelayForIteration_ReturnsZero_WhenNoDelaysSpecified()
        {
            var attribute = new RetryQueryAttribute(3);

            attribute.GetDelayForIteration(1).Should().Be(0);
            attribute.GetDelayForIteration(2).Should().Be(0);
            attribute.GetDelayForIteration(3).Should().Be(0);
        }

        [TestMethod]
        public void RetryQueryAttribute_GetDelayForIteration_ReturnsSameDelay_WhenOnlyOneDelaySpecified()
        {
            var attribute = new RetryQueryAttribute(3, 150);

            attribute.GetDelayForIteration(1).Should().Be(150);
            attribute.GetDelayForIteration(2).Should().Be(150);
            attribute.GetDelayForIteration(3).Should().Be(150);
        }

        [TestMethod]
        public void RetryQueryAttribute_GetDelayForIteration_ReturnsCorrectDelays_WhenMultipleDelaysSpecified()
        {
            var attribute = new RetryQueryAttribute(5, 50, 100, 150);

            attribute.GetDelayForIteration(1).Should().Be(50);
            attribute.GetDelayForIteration(2).Should().Be(100);
            attribute.GetDelayForIteration(3).Should().Be(150);
            attribute.GetDelayForIteration(4).Should().Be(150); // Uses last specified delay
            attribute.GetDelayForIteration(5).Should().Be(150); // Uses last specified delay
        }

        [TestMethod]
        public void RetryCommandAttribute_DefaultConstructor_SetsNullRetryCount()
        {
            var attribute = new RetryCommandAttribute();

            attribute.RetryCount.Should().BeNull();
            attribute.Delay1.Should().BeNull();
        }

        [TestMethod]
        public void RetryQueryAttribute_DefaultConstructor_SetsNullRetryCount()
        {
            var attribute = new RetryQueryAttribute();

            attribute.RetryCount.Should().BeNull();
            attribute.Delay1.Should().BeNull();
        }

        [TestMethod]
        public void RetryCommandAttribute_ConstructorWithRetryCount_SetsRetryCountOnly()
        {
            var attribute = new RetryCommandAttribute(5);

            attribute.RetryCount.Should().Be(5);
            attribute.Delay1.Should().BeNull();
        }

        [TestMethod]
        public void RetryQueryAttribute_ConstructorWithRetryCount_SetsRetryCountOnly()
        {
            var attribute = new RetryQueryAttribute(5);

            attribute.RetryCount.Should().Be(5);
            attribute.Delay1.Should().BeNull();
        }

        [TestMethod]
        public void RetryCommandAttribute_ConstructorWithRetryCountAndDelay_SetsBothValues()
        {
            var attribute = new RetryCommandAttribute(3, 200);

            attribute.RetryCount.Should().Be(3);
            attribute.Delay1.Should().Be(200);
            attribute.Delay2.Should().BeNull();
        }

        [TestMethod]
        public void RetryQueryAttribute_ConstructorWithRetryCountAndDelay_SetsBothValues()
        {
            var attribute = new RetryQueryAttribute(3, 200);

            attribute.RetryCount.Should().Be(3);
            attribute.Delay1.Should().Be(200);
            attribute.Delay2.Should().BeNull();
        }
    }
}

