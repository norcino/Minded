using AnonymousData;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Minded.Framework.CQRS.Query.Trait;

namespace Minded.Framework.CQRS.Tests
{
    /// <summary>
    /// Unit tests for the OrderDescriptor class.
    /// Tests constructor, properties, and ToString method to ensure correct functionality.
    /// </summary>
    [TestClass]
    public class OrderDescriptorTests
    {
        #region Constructor Tests

        /// <summary>
        /// Tests the constructor with Ascending order.
        /// Verifies that properties are set correctly.
        /// </summary>
        [TestMethod]
        public void Constructor_WithAscendingOrder_SetsPropertiesCorrectly()
        {
            var propertyName = Any.String();
            Order order = Order.Ascending;

            var descriptor = new OrderDescriptor(order, propertyName);

            descriptor.Order.Should().Be(order);
            descriptor.PropertyName.Should().Be(propertyName);
        }

        /// <summary>
        /// Tests the constructor with Descending order.
        /// Verifies that properties are set correctly.
        /// </summary>
        [TestMethod]
        public void Constructor_WithDescendingOrder_SetsPropertiesCorrectly()
        {
            var propertyName = Any.String();
            Order order = Order.Descending;

            var descriptor = new OrderDescriptor(order, propertyName);

            descriptor.Order.Should().Be(order);
            descriptor.PropertyName.Should().Be(propertyName);
        }

        /// <summary>
        /// Tests the constructor with null property name.
        /// Verifies that null values are handled.
        /// </summary>
        [TestMethod]
        public void Constructor_WithNullPropertyName_SetsPropertyNameToNull()
        {
            Order order = Order.Ascending;

            var descriptor = new OrderDescriptor(order, null);

            descriptor.PropertyName.Should().BeNull();
            descriptor.Order.Should().Be(order);
        }

        /// <summary>
        /// Tests the constructor with empty property name.
        /// Verifies that empty strings are handled.
        /// </summary>
        [TestMethod]
        public void Constructor_WithEmptyPropertyName_SetsPropertyNameToEmpty()
        {
            Order order = Order.Descending;

            var descriptor = new OrderDescriptor(order, string.Empty);

            descriptor.PropertyName.Should().BeEmpty();
            descriptor.Order.Should().Be(order);
        }

        #endregion

        #region Property Tests

        /// <summary>
        /// Tests that Order property is read-only.
        /// Verifies that Order can only be set through constructor.
        /// </summary>
        [TestMethod]
        public void Order_IsReadOnly()
        {
            var descriptor = new OrderDescriptor(Order.Ascending, Any.String());

            descriptor.Order.Should().Be(Order.Ascending);
        }

        /// <summary>
        /// Tests that PropertyName property is read-only.
        /// Verifies that PropertyName can only be set through constructor.
        /// </summary>
        [TestMethod]
        public void PropertyName_IsReadOnly()
        {
            var propertyName = Any.String();
            var descriptor = new OrderDescriptor(Order.Descending, propertyName);

            descriptor.PropertyName.Should().Be(propertyName);
        }

        #endregion

        #region ToString Tests

        /// <summary>
        /// Tests ToString with Ascending order.
        /// Verifies that the string representation is correct.
        /// </summary>
        [TestMethod]
        public void ToString_WithAscendingOrder_ReturnsCorrectFormat()
        {
            var propertyName = Any.String();
            var descriptor = new OrderDescriptor(Order.Ascending, propertyName);

            var result = descriptor.ToString();

            result.Should().Be($"{propertyName} Ascending");
        }

        /// <summary>
        /// Tests ToString with Descending order.
        /// Verifies that the string representation is correct.
        /// </summary>
        [TestMethod]
        public void ToString_WithDescendingOrder_ReturnsCorrectFormat()
        {
            var propertyName = Any.String();
            var descriptor = new OrderDescriptor(Order.Descending, propertyName);

            var result = descriptor.ToString();

            result.Should().Be($"{propertyName} Descending");
        }

        /// <summary>
        /// Tests ToString with null property name.
        /// Verifies that null is handled in string representation.
        /// </summary>
        [TestMethod]
        public void ToString_WithNullPropertyName_HandlesNull()
        {
            var descriptor = new OrderDescriptor(Order.Ascending, null);

            var result = descriptor.ToString();

            result.Should().Be(" Ascending");
        }

        #endregion
    }
}

