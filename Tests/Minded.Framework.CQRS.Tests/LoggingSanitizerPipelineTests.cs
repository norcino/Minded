using FluentAssertions;
using Minded.Framework.CQRS.Abstractions.Sanitization;
using Minded.Framework.CQRS.Sanitization;

namespace Minded.Framework.CQRS.Tests
{
    /// <summary>
    /// Unit tests for LoggingSanitizerPipeline.
    /// Tests object-to-dictionary conversion, sanitizer execution, property exclusion, and non-serializable type handling.
    /// </summary>
    [TestClass]
    public class LoggingSanitizerPipelineTests
    {
        private LoggingSanitizerPipeline _sut;

        [TestInitialize]
        public void Setup()
        {
            _sut = new LoggingSanitizerPipeline(null);
        }

        /// <summary>
        /// Tests that Sanitize converts a simple object to a dictionary with all public properties.
        /// </summary>
        [TestMethod]
        public void Sanitize_SimpleObject_ConvertsToDictionary()
        {
            // Arrange
            var obj = new SimpleTestClass
            {
                Id = 123,
                Name = "Test",
                Value = 45.67m
            };

            // Act
            var result = _sut.Sanitize(obj);

            // Assert
            result.Should().NotBeNull();
            result.Should().ContainKey("Id").WhoseValue.Should().Be(123);
            result.Should().ContainKey("Name").WhoseValue.Should().Be("Test");
            result.Should().ContainKey("Value").WhoseValue.Should().Be(45.67m);
        }

        /// <summary>
        /// Tests that Sanitize handles null input gracefully.
        /// </summary>
        [TestMethod]
        public void Sanitize_NullObject_ReturnsEmptyDictionary()
        {
            // Act
            var result = _sut.Sanitize(null);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        /// <summary>
        /// Tests that non-serializable types (CancellationToken, Task, Stream, Delegate) are excluded.
        /// </summary>
        [TestMethod]
        public void Sanitize_NonSerializableTypes_AreExcluded()
        {
            // Arrange
            var obj = new ClassWithNonSerializableTypes
            {
                Id = 1,
                Token = CancellationToken.None,
                BackgroundTask = Task.CompletedTask,
                FileStream = new MemoryStream(),
                Callback = () => "test"
            };

            // Act
            var result = _sut.Sanitize(obj);

            // Assert
            result.Should().ContainKey("Id").WhoseValue.Should().Be(1);
            result.Should().NotContainKey("Token");
            result.Should().NotContainKey("BackgroundTask");
            result.Should().NotContainKey("FileStream");
            result.Should().NotContainKey("Callback");
        }

        /// <summary>
        /// Tests that registered sanitizers are executed in order.
        /// </summary>
        [TestMethod]
        public void Sanitize_WithRegisteredSanitizers_ExecutesInOrder()
        {
            // Arrange
            var sanitizer1 = new TestSanitizer("Sanitizer1");
            var sanitizer2 = new TestSanitizer("Sanitizer2");
            _sut = new LoggingSanitizerPipeline(new[] { sanitizer1, sanitizer2 });

            var obj = new SimpleTestClass { Id = 1, Name = "Test" };

            // Act
            var result = _sut.Sanitize(obj);

            // Assert
            result.Should().ContainKey("ProcessedBy");
            var processedBy = result["ProcessedBy"] as List<string>;
            processedBy.Should().NotBeNull();
            processedBy.Should().HaveCount(2);
            processedBy[0].Should().Be("Sanitizer1");
            processedBy[1].Should().Be("Sanitizer2");
        }

        /// <summary>
        /// Tests that ExcludeProperties removes specified properties from interface implementations.
        /// </summary>
        [TestMethod]
        public void ExcludeProperties_InterfaceProperties_AreExcluded()
        {
            // Arrange
            _sut.ExcludeProperties(typeof(ITestInterface), "ExcludedProperty");

            var obj = new ClassImplementingInterface
            {
                Id = 1,
                Name = "Test",
                ExcludedProperty = "ShouldNotAppear"
            };

            // Act
            var result = _sut.Sanitize(obj);

            // Assert
            result.Should().ContainKey("Id");
            result.Should().ContainKey("Name");
            result.Should().NotContainKey("ExcludedProperty");
        }

        /// <summary>
        /// Tests that nested objects are recursively converted to dictionaries.
        /// </summary>
        [TestMethod]
        public void Sanitize_NestedObject_IsRecursivelyConverted()
        {
            // Arrange
            var obj = new ClassWithNestedObject
            {
                Id = 1,
                Nested = new SimpleTestClass
                {
                    Id = 2,
                    Name = "Nested",
                    Value = 99.99m
                }
            };

            // Act
            var result = _sut.Sanitize(obj);

            // Assert
            result.Should().ContainKey("Id").WhoseValue.Should().Be(1);
            result.Should().ContainKey("Nested");

            var nested = result["Nested"] as IDictionary<string, object>;
            nested.Should().NotBeNull();
            nested.Should().ContainKey("Id").WhoseValue.Should().Be(2);
            nested.Should().ContainKey("Name").WhoseValue.Should().Be("Nested");
            nested.Should().ContainKey("Value").WhoseValue.Should().Be(99.99m);
        }

        /// <summary>
        /// Tests that collections are converted to lists and truncated at max items.
        /// </summary>
        [TestMethod]
        public void Sanitize_Collection_IsTruncatedAtMaxItems()
        {
            // Arrange
            var obj = new ClassWithCollection
            {
                Id = 1,
                Items = Enumerable.Range(1, 20).Select(i => $"Item{i}").ToList()
            };

            // Act
            var result = _sut.Sanitize(obj);

            // Assert
            result.Should().ContainKey("Items");
            var items = result["Items"] as List<object>;
            items.Should().NotBeNull();
            items.Count.Should().BeLessThanOrEqualTo(11); // 10 items + "... (truncated)"
            items.Last().Should().Be("... (truncated)");
        }

        /// <summary>
        /// Tests that public fields are also included in sanitization.
        /// </summary>
        [TestMethod]
        public void Sanitize_PublicFields_AreIncluded()
        {
            // Arrange
            var obj = new ClassWithFields
            {
                Id = 1,
                PublicField = "FieldValue"
            };

            // Act
            var result = _sut.Sanitize(obj);

            // Assert
            result.Should().ContainKey("Id").WhoseValue.Should().Be(1);
            result.Should().ContainKey("PublicField").WhoseValue.Should().Be("FieldValue");
        }

        /// <summary>
        /// Tests that RegisterSanitizer throws ArgumentNullException for null sanitizer.
        /// </summary>
        [TestMethod]
        public void RegisterSanitizer_NullSanitizer_ThrowsArgumentNullException()
        {
            // Act & Assert
            Action act = () => _sut.RegisterSanitizer(null);
            act.Should().Throw<ArgumentNullException>();
        }

        /// <summary>
        /// Tests that ExcludeProperties throws ArgumentNullException for null interface type.
        /// </summary>
        [TestMethod]
        public void ExcludeProperties_NullInterfaceType_ThrowsArgumentNullException()
        {
            // Act & Assert
            Action act = () => _sut.ExcludeProperties(null, "Property");
            act.Should().Throw<ArgumentNullException>();
        }

        #region Test Classes

        private class SimpleTestClass
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public decimal Value { get; set; }
        }

        private class ClassWithNonSerializableTypes
        {
            public int Id { get; set; }
            public CancellationToken Token { get; set; }
            public Task BackgroundTask { get; set; }
            public Stream FileStream { get; set; }
            public Func<string> Callback { get; set; }
        }

        private class ClassWithNestedObject
        {
            public int Id { get; set; }
            public SimpleTestClass Nested { get; set; }
        }

        private class ClassWithCollection
        {
            public int Id { get; set; }
            public List<string> Items { get; set; }
        }

        private class ClassWithFields
        {
            public int Id { get; set; }
            public string PublicField;
        }

        private interface ITestInterface
        {
            string ExcludedProperty { get; set; }
        }

        private class ClassImplementingInterface : ITestInterface
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string ExcludedProperty { get; set; }
        }

        private class TestSanitizer : ILoggingSanitizer
        {
            private readonly string _name;

            public TestSanitizer(string name)
            {
                _name = name;
            }

            public IDictionary<string, object> Sanitize(IDictionary<string, object> data, Type sourceType)
            {
                if (!data.ContainsKey("ProcessedBy"))
                {
                    data["ProcessedBy"] = new List<string>();
                }

                var processedBy = data["ProcessedBy"] as List<string>;
                processedBy.Add(_name);

                return data;
            }
        }

        #endregion
    }
}


