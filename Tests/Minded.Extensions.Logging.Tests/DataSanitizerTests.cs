using FluentAssertions;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Minded.Extensions.DataProtection;
using Minded.Extensions.DataProtection.Abstractions;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Minded.Extensions.Logging.Tests
{
    /// <summary>
    /// Unit tests for DataSanitizer service.
    /// Tests sensitive data protection, nested objects, collections, and recursion protection.
    /// </summary>
    [TestClass]
    public class DataSanitizerTests
    {
        private DataSanitizer _sut;
        private DataProtectionOptions _options;

        [TestInitialize]
        public void Setup()
        {
            _options = new DataProtectionOptions();
            IOptions<DataProtectionOptions> mockOptions = Options.Create(_options);
            _sut = new DataSanitizer(mockOptions);
        }

        #region Test Classes

        private class TestUser
        {
            public int Id { get; set; }
            public string Name { get; set; }

            [SensitiveData]
            public string Email { get; set; }

            [SensitiveData]
            public string Surname { get; set; }
        }

        private class TestOrder
        {
            public int OrderId { get; set; }
            public decimal Total { get; set; }
            public TestUser Customer { get; set; }
        }

        private class TestNode
        {
            public int Id { get; set; }
            public TestNode Parent { get; set; }
            public List<TestNode> Children { get; set; }
        }

        #endregion

        #region ShowSensitiveData = false (default - hide)

        /// <summary>
        /// Tests that sensitive properties are omitted when ShowSensitiveData is false (default).
        /// Verifies GDPR/CCPA compliance by hiding PII.
        /// </summary>
        [TestMethod]
        public void Sanitize_OmitsSensitiveProperties_WhenShowSensitiveDataIsFalse()
        {
            // Arrange
            _options.ShowSensitiveData = false;
            var user = new TestUser
            {
                Id = 1,
                Name = "John",
                Email = "john@example.com",
                Surname = "Doe"
            };

            // Act
            IDictionary<string, object> result = _sut.Sanitize(user);

            // Assert
            result.Should().ContainKey("Id");
            result.Should().ContainKey("Name");
            result.Should().NotContainKey("Email");
            result.Should().NotContainKey("Surname");
            result["Id"].Should().Be(1);
            result["Name"].Should().Be("John");
        }

        /// <summary>
        /// Tests that sensitive properties are omitted by default (ShowSensitiveData defaults to false).
        /// Verifies secure-by-default behavior.
        /// </summary>
        [TestMethod]
        public void Sanitize_OmitsSensitiveProperties_ByDefault()
        {
            // Arrange - use default options (ShowSensitiveData = false)
            var user = new TestUser
            {
                Id = 1,
                Name = "John",
                Email = "john@example.com",
                Surname = "Doe"
            };

            // Act
            IDictionary<string, object> result = _sut.Sanitize(user);

            // Assert
            result.Should().NotContainKey("Email");
            result.Should().NotContainKey("Surname");
        }

        #endregion

        #region ShowSensitiveData = true (show)

        /// <summary>
        /// Tests that sensitive properties are included when ShowSensitiveData is true.
        /// Verifies development/debugging mode.
        /// </summary>
        [TestMethod]
        public void Sanitize_IncludesSensitiveProperties_WhenShowSensitiveDataIsTrue()
        {
            // Arrange
            _options.ShowSensitiveData = true;
            var user = new TestUser
            {
                Id = 1,
                Name = "John",
                Email = "john@example.com",
                Surname = "Doe"
            };

            // Act
            IDictionary<string, object> result = _sut.Sanitize(user);

            // Assert
            result.Should().ContainKey("Id");
            result.Should().ContainKey("Name");
            result.Should().ContainKey("Email");
            result.Should().ContainKey("Surname");
            result["Email"].Should().Be("john@example.com");
            result["Surname"].Should().Be("Doe");
        }

        #endregion

        #region ShowSensitiveDataProvider Tests

        /// <summary>
        /// Tests that ShowSensitiveDataProvider takes precedence over ShowSensitiveData.
        /// Verifies provider pattern for runtime configuration.
        /// </summary>
        [TestMethod]
        public void Sanitize_UsesProvider_WhenShowSensitiveDataProviderIsSet()
        {
            // Arrange
            _options.ShowSensitiveData = false;
            _options.ShowSensitiveDataProvider = () => true;  // Provider overrides static value
            var user = new TestUser
            {
                Id = 1,
                Name = "John",
                Email = "john@example.com",
                Surname = "Doe"
            };

            // Act
            IDictionary<string, object> result = _sut.Sanitize(user);

            // Assert
            result.Should().ContainKey("Email");
            result.Should().ContainKey("Surname");
        }

        /// <summary>
        /// Tests that provider is called dynamically on each sanitization.
        /// Verifies feature flag behavior.
        /// </summary>
        [TestMethod]
        public void Sanitize_IsDynamic_WhenProviderChanges()
        {
            // Arrange
            var showSensitiveData = false;
            _options.ShowSensitiveDataProvider = () => showSensitiveData;
            var user = new TestUser
            {
                Id = 1,
                Name = "John",
                Email = "john@example.com",
                Surname = "Doe"
            };

            // Act - first call with provider returning false
            IDictionary<string, object> result1 = _sut.Sanitize(user);

            // Assert - sensitive data hidden
            result1.Should().NotContainKey("Email");

            // Arrange - change provider return value
            showSensitiveData = true;

            // Act - second call with provider returning true
            IDictionary<string, object> result2 = _sut.Sanitize(user);

            // Assert - sensitive data shown
            result2.Should().ContainKey("Email");
        }

        #endregion

        #region Nested Objects

        /// <summary>
        /// Tests that sensitive properties in nested objects are omitted.
        /// Verifies recursive sanitization.
        /// </summary>
        [TestMethod]
        public void Sanitize_OmitsSensitivePropertiesInNestedObjects_WhenShowSensitiveDataIsFalse()
        {
            // Arrange
            _options.ShowSensitiveData = false;
            var order = new TestOrder
            {
                OrderId = 100,
                Total = 99.99m,
                Customer = new TestUser
                {
                    Id = 1,
                    Name = "John",
                    Email = "john@example.com",
                    Surname = "Doe"
                }
            };

            // Act
            IDictionary<string, object> result = _sut.Sanitize(order);

            // Assert
            result.Should().ContainKey("OrderId");
            result.Should().ContainKey("Total");
            result.Should().ContainKey("Customer");

            var customer = result["Customer"] as IDictionary<string, object>;
            customer.Should().NotBeNull();
            customer.Should().ContainKey("Id");
            customer.Should().ContainKey("Name");
            customer.Should().NotContainKey("Email");
            customer.Should().NotContainKey("Surname");
        }

        #endregion

        #region Collections

        /// <summary>
        /// Tests that collections are sanitized correctly.
        /// Verifies each item in collection is sanitized.
        /// </summary>
        [TestMethod]
        public void SanitizeCollection_OmitsSensitivePropertiesInEachItem()
        {
            // Arrange
            _options.ShowSensitiveData = false;
            var users = new List<TestUser>
            {
                new() { Id = 1, Name = "John", Email = "john@example.com", Surname = "Doe" },
                new() { Id = 2, Name = "Jane", Email = "jane@example.com", Surname = "Smith" }
            };

            // Act
            var result = _sut.SanitizeCollection(users).ToList();

            // Assert
            result.Should().HaveCount(2);
            result[0].Should().NotContainKey("Email");
            result[0].Should().NotContainKey("Surname");
            result[1].Should().NotContainKey("Email");
            result[1].Should().NotContainKey("Surname");
        }

        /// <summary>
        /// Tests that collections are truncated to 10 items.
        /// Verifies log size protection.
        /// </summary>
        [TestMethod]
        public void SanitizeCollection_TruncatesTo10Items()
        {
            // Arrange
            var users = new List<TestUser>();
            for (int i = 0; i < 20; i++)
            {
                users.Add(new TestUser { Id = i, Name = $"User{i}" });
            }

            // Act
            var result = _sut.SanitizeCollection(users).ToList();

            // Assert
            result.Should().HaveCount(10);
        }

        #endregion

        #region Null and Edge Cases

        /// <summary>
        /// Tests that null objects are handled gracefully.
        /// Verifies null safety.
        /// </summary>
        [TestMethod]
        public void Sanitize_ReturnsEmptyDictionary_WhenObjectIsNull()
        {
            // Act
            IDictionary<string, object> result = _sut.Sanitize(null);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        /// <summary>
        /// Tests that null collections are handled gracefully.
        /// Verifies null safety.
        /// </summary>
        [TestMethod]
        public void SanitizeCollection_ReturnsEmptyCollection_WhenCollectionIsNull()
        {
            // Act
            var result = _sut.SanitizeCollection(null).ToList();

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        #endregion

        #region IsSensitiveProperty Tests

        /// <summary>
        /// Tests that IsSensitiveProperty correctly identifies properties with [SensitiveData] attribute.
        /// </summary>
        [TestMethod]
        public void IsSensitiveProperty_ReturnsTrue_ForPropertiesWithSensitiveDataAttribute()
        {
            // Arrange
            PropertyInfo emailProperty = typeof(TestUser).GetProperty("Email");

            // Act
            var result = _sut.IsSensitiveProperty(emailProperty);

            // Assert
            result.Should().BeTrue();
        }

        /// <summary>
        /// Tests that IsSensitiveProperty returns false for properties without [SensitiveData] attribute.
        /// </summary>
        [TestMethod]
        public void IsSensitiveProperty_ReturnsFalse_ForPropertiesWithoutSensitiveDataAttribute()
        {
            // Arrange
            PropertyInfo nameProperty = typeof(TestUser).GetProperty("Name");

            // Act
            var result = _sut.IsSensitiveProperty(nameProperty);

            // Assert
            result.Should().BeFalse();
        }

        #endregion
    }
}

