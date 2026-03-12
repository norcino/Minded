using System;
using System.Collections.Generic;
using System.Linq;
using Application.Api.OData;
using Data.Entity;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Application.Api.Tests.OData
{
    /// <summary>
    /// Unit tests for IgnoreNavigationPropertiesResolver.
    /// Tests verify that the resolver correctly controls serialization of navigation properties based on $expand parameters.
    /// </summary>
    [TestClass]
    public class IgnoreNavigationPropertiesResolverTests
    {
        private Mock<IHttpContextAccessor> _httpContextAccessorMock;
        private Mock<HttpContext> _httpContextMock;
        private IDictionary<object, object> _httpContextItems;
        private IgnoreNavigationPropertiesResolver _resolver;

        [TestInitialize]
        public void TestInitialize()
        {
            _httpContextMock = new Mock<HttpContext>();
            _httpContextItems = new Dictionary<object, object>();
            _httpContextMock.Setup(c => c.Items).Returns(_httpContextItems);

            _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            _httpContextAccessorMock.Setup(a => a.HttpContext).Returns(_httpContextMock.Object);

            _resolver = new IgnoreNavigationPropertiesResolver(_httpContextAccessorMock.Object);
        }

        [TestMethod]
        public void CreateProperties_without_expanded_properties_in_HttpContext_does_not_serialize_virtual_navigation_properties()
        {
            // Arrange
            _httpContextItems[ODataConstants.ExpandedPropertiesKey] = new HashSet<string>();

            var category = new Category
            {
                Id = 1,
                Name = "Electronics",
                Description = "Electronic items",
                Transactions = new List<Transaction>
                {
                    new Transaction { Id = 1, Description = "Purchase" }
                }
            };

            // Act
            var json = SerializeWithResolver(category);

            // Assert
            Assert.Contains("\"Id\":1", json);
            Assert.Contains("\"Name\":\"Electronics\"", json);
            Assert.Contains("\"Description\":\"Electronic items\"", json);
            Assert.DoesNotContain("Transactions", json, "Transactions should not be serialized when not expanded");
        }

        [TestMethod]
        public void CreateProperties_with_expanded_property_in_HttpContext_serializes_that_navigation_property()
        {
            // Arrange
            var expandedProperties = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Transactions" };
            _httpContextItems[ODataConstants.ExpandedPropertiesKey] = expandedProperties;

            var category = new Category
            {
                Id = 1,
                Name = "Electronics",
                Description = "Electronic items",
                Transactions = new List<Transaction>
                {
                    new Transaction { Id = 1, Description = "Purchase" }
                }
            };

            // Act
            var json = SerializeWithResolver(category);

            // Assert
            Assert.Contains("\"Id\":1", json);
            Assert.Contains("\"Name\":\"Electronics\"", json);
            Assert.Contains("\"Transactions\"", json, "Transactions should be serialized when expanded");
            Assert.Contains("\"Description\":\"Purchase\"", json);
        }

        [TestMethod]
        public void CreateProperties_with_multiple_expanded_properties_serializes_all_expanded_navigation_properties()
        {
            // Arrange
            var expandedProperties = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Category", "User" };
            _httpContextItems[ODataConstants.ExpandedPropertiesKey] = expandedProperties;

            var transaction = new Transaction
            {
                Id = 1,
                Description = "Purchase",
                Credit = 99.99m,
                Debit = 0m,
                Category = new Category { Id = 1, Name = "Electronics" },
                User = new User { Id = 1, Name = "John Doe" }
            };

            // Act
            var json = SerializeWithResolver(transaction);

            // Assert
            Assert.Contains("\"Category\"", json, "Category should be serialized when expanded");
            Assert.Contains("\"User\"", json, "User should be serialized when expanded");
            Assert.Contains("\"Electronics\"", json);
            Assert.Contains("\"John Doe\"", json);
        }

        [TestMethod]
        public void CreateProperties_with_one_expanded_property_does_not_serialize_other_navigation_properties()
        {
            // Arrange
            var expandedProperties = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Category" };
            _httpContextItems[ODataConstants.ExpandedPropertiesKey] = expandedProperties;

            var transaction = new Transaction
            {
                Id = 1,
                Description = "Purchase",
                Credit = 99.99m,
                Debit = 0m,
                Category = new Category { Id = 1, Name = "Electronics" },
                User = new User { Id = 1, Name = "John Doe" }
            };

            // Act
            var json = SerializeWithResolver(transaction);

            // Assert
            Assert.Contains("\"Category\"", json, "Category should be serialized when expanded");
            Assert.DoesNotContain("\"User\"", json, "User should not be serialized when not expanded");
            Assert.Contains("\"Electronics\"", json);
            Assert.DoesNotContain("\"John Doe\"", json);
        }

        [TestMethod]
        public void CreateProperties_is_case_insensitive_for_expanded_property_names()
        {
            // Arrange
            var expandedProperties = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "transactions" };
            _httpContextItems[ODataConstants.ExpandedPropertiesKey] = expandedProperties;

            var category = new Category
            {
                Id = 1,
                Name = "Electronics",
                Transactions = new List<Transaction>
                {
                    new Transaction { Id = 1, Description = "Purchase" }
                }
            };

            // Act
            var json = SerializeWithResolver(category);

            // Assert
            Assert.Contains("\"Transactions\"", json, "Transactions should be serialized (case-insensitive match)");
        }

        [TestMethod]
        public void CreateProperties_without_HttpContext_does_not_serialize_navigation_properties()
        {
            // Arrange
            _httpContextAccessorMock.Setup(a => a.HttpContext).Returns((HttpContext)null);

            var category = new Category
            {
                Id = 1,
                Name = "Electronics",
                Transactions = new List<Transaction>
                {
                    new Transaction { Id = 1, Description = "Purchase" }
                }
            };

            // Act
            var json = SerializeWithResolver(category);

            // Assert
            Assert.DoesNotContain("Transactions", json, "Transactions should not be serialized when HttpContext is null");
        }

        [TestMethod]
        public void CreateProperties_with_custom_isNavigationProperty_function_uses_custom_logic()
        {
            // Arrange
            Func<Type, bool> customIsNavigationProperty = type => type.Name == "Category";
            var customResolver = new IgnoreNavigationPropertiesResolver(_httpContextAccessorMock.Object, customIsNavigationProperty);

            var expandedProperties = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Category" };
            _httpContextItems[ODataConstants.ExpandedPropertiesKey] = expandedProperties;

            var transaction = new Transaction
            {
                Id = 1,
                Description = "Purchase",
                Category = new Category { Id = 1, Name = "Electronics" }
            };

            // Act
            var json = SerializeWithCustomResolver(transaction, customResolver);

            // Assert
            Assert.Contains("\"Category\"", json, "Category should be serialized when expanded with custom logic");
        }

        [TestMethod]
        public void CreateProperties_always_serializes_non_virtual_properties()
        {
            // Arrange
            _httpContextItems[ODataConstants.ExpandedPropertiesKey] = new HashSet<string>();

            var category = new Category
            {
                Id = 1,
                Name = "Electronics",
                Description = "Electronic items"
            };

            // Act
            var json = SerializeWithResolver(category);

            // Assert
            Assert.Contains("\"Id\":1", json, "Id should always be serialized (non-virtual)");
            Assert.Contains("\"Name\":\"Electronics\"", json, "Name should always be serialized (non-virtual)");
            Assert.Contains("\"Description\":\"Electronic items\"", json, "Description should always be serialized (non-virtual)");
        }

        #region Helper Methods

        /// <summary>
        /// Serializes an object using the resolver under test.
        /// </summary>
        private string SerializeWithResolver(object obj)
        {
            var settings = new JsonSerializerSettings
            {
                ContractResolver = _resolver,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };

            return JsonConvert.SerializeObject(obj, settings);
        }

        /// <summary>
        /// Serializes an object using a custom resolver.
        /// </summary>
        private string SerializeWithCustomResolver(object obj, IContractResolver resolver)
        {
            var settings = new JsonSerializerSettings
            {
                ContractResolver = resolver,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };

            return JsonConvert.SerializeObject(obj, settings);
        }

        #endregion
    }
}

