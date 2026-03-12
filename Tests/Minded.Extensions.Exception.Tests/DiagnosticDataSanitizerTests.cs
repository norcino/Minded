using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Minded.Extensions.Exception;
using Minded.Framework.CQRS.Abstractions;
using Minded.Framework.CQRS.Command;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Minded.Extensions.Exception.Tests
{
    /// <summary>
    /// Unit tests for DiagnosticDataSanitizer.
    /// Tests non-serializable type removal and attribute-based exclusion.
    /// </summary>
    [TestClass]
    public class DiagnosticDataSanitizerTests
    {
        #region Basic Functionality Tests

        [TestMethod]
        public void Sanitize_NullObject_ReturnsEmptyDictionary()
        {
            var result = DiagnosticDataSanitizer.Sanitize(null);

            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [TestMethod]
        public void Sanitize_SimpleObject_ReturnsAllProperties()
        {
            var obj = new SimpleCommand { Id = 1, Name = "Test" };

            var result = DiagnosticDataSanitizer.Sanitize(obj);

            result.Should().ContainKey("Id");
            result.Should().ContainKey("Name");
            result["Id"].Should().Be(1);
            result["Name"].Should().Be("Test");
        }

        #endregion

        #region CancellationToken Tests

        [TestMethod]
        public void Sanitize_ObjectWithCancellationToken_ExcludesCancellationToken()
        {
            var obj = new CommandWithCancellationToken
            {
                Id = 1,
                Token = new CancellationToken()
            };

            var result = DiagnosticDataSanitizer.Sanitize(obj);

            result.Should().ContainKey("Id");
            result.Should().NotContainKey("Token");
        }

        [TestMethod]
        public void Sanitize_ObjectWithCancellationTokenSource_ExcludesCancellationTokenSource()
        {
            using var cts = new CancellationTokenSource();
            var obj = new CommandWithCancellationTokenSource
            {
                Id = 1,
                TokenSource = cts
            };

            var result = DiagnosticDataSanitizer.Sanitize(obj);

            result.Should().ContainKey("Id");
            result.Should().NotContainKey("TokenSource");
        }

        #endregion

        #region Task and Delegate Tests

        [TestMethod]
        public void Sanitize_ObjectWithTask_ExcludesTask()
        {
            var obj = new CommandWithTask
            {
                Id = 1,
                BackgroundTask = Task.CompletedTask
            };

            var result = DiagnosticDataSanitizer.Sanitize(obj);

            result.Should().ContainKey("Id");
            result.Should().NotContainKey("BackgroundTask");
        }

        [TestMethod]
        public void Sanitize_ObjectWithGenericTask_ExcludesTask()
        {
            var obj = new CommandWithGenericTask
            {
                Id = 1,
                AsyncResult = Task.FromResult("test")
            };

            var result = DiagnosticDataSanitizer.Sanitize(obj);

            result.Should().ContainKey("Id");
            result.Should().NotContainKey("AsyncResult");
        }

        [TestMethod]
        public void Sanitize_ObjectWithFunc_ExcludesFunc()
        {
            var obj = new CommandWithFunc
            {
                Id = 1,
                Callback = () => "result"
            };

            var result = DiagnosticDataSanitizer.Sanitize(obj);

            result.Should().ContainKey("Id");
            result.Should().NotContainKey("Callback");
        }

        [TestMethod]
        public void Sanitize_ObjectWithAction_ExcludesAction()
        {
            var obj = new CommandWithAction
            {
                Id = 1,
                OnComplete = () => { }
            };

            var result = DiagnosticDataSanitizer.Sanitize(obj);

            result.Should().ContainKey("Id");
            result.Should().NotContainKey("OnComplete");
        }

        #endregion

        #region Stream Tests

        [TestMethod]
        public void Sanitize_ObjectWithStream_ExcludesStream()
        {
            var obj = new CommandWithStream
            {
                Id = 1,
                FileStream = new MemoryStream()
            };

            var result = DiagnosticDataSanitizer.Sanitize(obj);

            result.Should().ContainKey("Id");
            result.Should().NotContainKey("FileStream");
        }

        #endregion

        #region Type System Tests

        [TestMethod]
        public void Sanitize_ObjectWithType_ExcludesType()
        {
            var obj = new CommandWithType
            {
                Id = 1,
                EntityType = typeof(string)
            };

            var result = DiagnosticDataSanitizer.Sanitize(obj);

            result.Should().ContainKey("Id");
            result.Should().NotContainKey("EntityType");
        }

        [TestMethod]
        public void Sanitize_ObjectWithIServiceProvider_ExcludesServiceProvider()
        {
            var obj = new CommandWithServiceProvider
            {
                Id = 1,
                ServiceProvider = new MockServiceProvider()
            };

            var result = DiagnosticDataSanitizer.Sanitize(obj);

            result.Should().ContainKey("Id");
            result.Should().NotContainKey("ServiceProvider");
        }

        #endregion

        #region Collection Tests

        [TestMethod]
        public void Sanitize_ObjectWithCollection_IncludesCollectionItems()
        {
            var obj = new CommandWithCollection
            {
                Id = 1,
                Items = new List<string> { "a", "b", "c" }
            };

            var result = DiagnosticDataSanitizer.Sanitize(obj);

            result.Should().ContainKey("Id");
            result.Should().ContainKey("Items");
            var items = result["Items"] as List<object>;
            items.Should().HaveCount(3);
        }

        [TestMethod]
        public void Sanitize_LargeCollection_TruncatesToMaxItems()
        {
            var obj = new CommandWithCollection
            {
                Id = 1,
                Items = new List<string>()
            };
            for (int i = 0; i < 20; i++)
                obj.Items.Add($"item{i}");

            var result = DiagnosticDataSanitizer.Sanitize(obj);

            var items = result["Items"] as List<object>;
            items.Should().HaveCount(11); // 10 items + "... (truncated)"
            items[10].Should().Be("... (truncated)");
        }

        #endregion

        #region Complex Object Tests

        [TestMethod]
        public void Sanitize_ComplexCommand_ExcludesAllNonSerializableProperties()
        {
            using var cts = new CancellationTokenSource();
            var obj = new ComplexCommand
            {
                Id = 1,
                Name = "Test",
                Token = cts.Token,
                BackgroundTask = Task.CompletedTask,
                Callback = () => "result",
                FileStream = new MemoryStream(),
                InternalData = "internal"
            };

            var result = DiagnosticDataSanitizer.Sanitize(obj);

            result.Should().ContainKey("Id");
            result.Should().ContainKey("Name");
            result.Should().ContainKey("InternalData"); // InternalData is a regular string property now
            result.Should().NotContainKey("Token");
            result.Should().NotContainKey("BackgroundTask");
            result.Should().NotContainKey("Callback");
            result.Should().NotContainKey("FileStream");
        }

        [TestMethod]
        public void Sanitize_NestedObject_SanitizesNestedProperties()
        {
            var obj = new CommandWithNestedObject
            {
                Id = 1,
                Nested = new NestedObjectWithNonSerializable
                {
                    Value = "nested",
                    Token = new CancellationToken()
                }
            };

            var result = DiagnosticDataSanitizer.Sanitize(obj);

            result.Should().ContainKey("Id");
            result.Should().ContainKey("Nested");
            var nested = result["Nested"] as IDictionary<string, object>;
            nested.Should().ContainKey("Value");
            nested.Should().NotContainKey("Token");
        }

        #endregion
    }

    #region Test Classes

    public class SimpleCommand
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class CommandWithCancellationToken
    {
        public int Id { get; set; }
        public CancellationToken Token { get; set; }
    }

    public class CommandWithCancellationTokenSource
    {
        public int Id { get; set; }
        public CancellationTokenSource TokenSource { get; set; }
    }

    public class CommandWithTask
    {
        public int Id { get; set; }
        public Task BackgroundTask { get; set; }
    }

    public class CommandWithGenericTask
    {
        public int Id { get; set; }
        public Task<string> AsyncResult { get; set; }
    }

    public class CommandWithFunc
    {
        public int Id { get; set; }
        public Func<string> Callback { get; set; }
    }

    public class CommandWithAction
    {
        public int Id { get; set; }
        public Action OnComplete { get; set; }
    }

    public class CommandWithStream
    {
        public int Id { get; set; }
        public Stream FileStream { get; set; }
    }

    public class CommandWithExcludeAttribute
    {
        public int Id { get; set; }
        public string Name { get; set; }

        // Note: ExcludeFromSerializedDiagnosticLogging attribute has been removed
        // Property exclusion is now handled by the LoggingSanitizerPipeline.ExcludeProperties method
        public string InternalData { get; set; }
    }

    public class CommandWithJsonIgnore
    {
        public int Id { get; set; }
        public string Name { get; set; }

        [JsonIgnore]
        public string CachedValue { get; set; }
    }

    public class CommandWithBothAttributes
    {
        public int Id { get; set; }

        // Note: ExcludeFromSerializedDiagnosticLogging attribute has been removed
        // Property exclusion is now handled by the LoggingSanitizerPipeline.ExcludeProperties method
        public string ExcludedByDiagnostic { get; set; }

        [JsonIgnore]
        public string ExcludedByJson { get; set; }
    }

    public class CommandWithType
    {
        public int Id { get; set; }
        public Type EntityType { get; set; }
    }

    public class CommandWithServiceProvider
    {
        public int Id { get; set; }
        public IServiceProvider ServiceProvider { get; set; }
    }

    public class MockServiceProvider : IServiceProvider
    {
        public object GetService(Type serviceType) => null;
    }

    public class CommandWithCollection
    {
        public int Id { get; set; }
        public List<string> Items { get; set; }
    }

    public class ComplexCommand
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public CancellationToken Token { get; set; }
        public Task BackgroundTask { get; set; }
        public Func<string> Callback { get; set; }
        public Stream FileStream { get; set; }

        // Note: ExcludeFromSerializedDiagnosticLogging attribute has been removed
        // Property exclusion is now handled by the LoggingSanitizerPipeline.ExcludeProperties method
        public string InternalData { get; set; }
    }

    public class CommandWithNestedObject
    {
        public int Id { get; set; }
        public NestedObjectWithNonSerializable Nested { get; set; }
    }

    public class NestedObjectWithNonSerializable
    {
        public string Value { get; set; }
        public CancellationToken Token { get; set; }
    }

    #endregion
}

