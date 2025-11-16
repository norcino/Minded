using AnonymousData;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Minded.Framework.CQRS.Abstractions;
using Minded.Framework.CQRS.Query;

namespace Minded.Framework.CQRS.Tests
{
    /// <summary>
    /// Unit tests for the QueryResponse<TResult> class.
    /// Tests all constructors, properties, and behavior to ensure correct functionality.
    /// </summary>
    [TestClass]
    public class QueryResponseTests
    {
        #region Constructor Tests

        /// <summary>
        /// Tests the default constructor of QueryResponse<TResult>.
        /// Verifies that OutcomeEntries is initialized as an empty list and Result is default.
        /// </summary>
        [TestMethod]
        public void QueryResponse_DefaultConstructor_InitializesCorrectly()
        {
            var response = new QueryResponse<int>();

            response.OutcomeEntries.Should().NotBeNull();
            response.OutcomeEntries.Should().BeEmpty();
            response.Result.Should().Be(0);
            response.Successful.Should().BeFalse();
        }

        /// <summary>
        /// Tests the constructor with result parameter.
        /// Verifies that Result is set correctly and Successful is set to true.
        /// </summary>
        [TestMethod]
        public void QueryResponse_ConstructorWithResult_SetsResultAndSuccessful()
        {
            var expectedResult = Any.Int();

            var response = new QueryResponse<int>(expectedResult);

            response.Result.Should().Be(expectedResult);
            response.Successful.Should().BeTrue();
            response.OutcomeEntries.Should().NotBeNull();
            response.OutcomeEntries.Should().BeEmpty();
        }

        /// <summary>
        /// Tests QueryResponse with reference type result.
        /// Verifies that complex objects can be used as results.
        /// </summary>
        [TestMethod]
        public void QueryResponse_WithReferenceType_StoresObjectCorrectly()
        {
            var expectedResult = new { Id = Any.Int(), Name = Any.String(), Value = Any.Double() };

            var response = new QueryResponse<object>(expectedResult);

            response.Result.Should().Be(expectedResult);
            response.Successful.Should().BeTrue();
        }

        /// <summary>
        /// Tests QueryResponse with null result.
        /// Verifies that null values are handled correctly for reference types.
        /// </summary>
        [TestMethod]
        public void QueryResponse_WithNullResult_StoresNullCorrectly()
        {
            var response = new QueryResponse<string>(null);

            response.Result.Should().BeNull();
            response.Successful.Should().BeTrue();
        }

        /// <summary>
        /// Tests QueryResponse with string result.
        /// Verifies that string results are stored correctly.
        /// </summary>
        [TestMethod]
        public void QueryResponse_WithStringResult_StoresStringCorrectly()
        {
            var expectedResult = Any.String();

            var response = new QueryResponse<string>(expectedResult);

            response.Result.Should().Be(expectedResult);
            response.Successful.Should().BeTrue();
        }

        /// <summary>
        /// Tests QueryResponse with collection result.
        /// Verifies that collections can be used as results.
        /// </summary>
        [TestMethod]
        public void QueryResponse_WithCollectionResult_StoresCollectionCorrectly()
        {
            var expectedResult = new List<int> { Any.Int(), Any.Int(), Any.Int(), Any.Int(), Any.Int() };

            var response = new QueryResponse<List<int>>(expectedResult);

            response.Result.Should().BeSameAs(expectedResult);
            response.Result.Should().HaveCount(5);
            response.Successful.Should().BeTrue();
        }

        #endregion

        #region Property Tests

        /// <summary>
        /// Tests that Result property is read-only.
        /// Verifies that Result can only be set through constructor.
        /// </summary>
        [TestMethod]
        public void QueryResponse_Result_IsReadOnly()
        {
            var initialResult = Any.String();
            var response = new QueryResponse<string>(initialResult);

            response.Result.Should().Be(initialResult);
        }

        /// <summary>
        /// Tests that Successful property can be modified after construction.
        /// Verifies that the setter works correctly.
        /// </summary>
        [TestMethod]
        public void QueryResponse_Successful_CanBeModified()
        {
            var response = new QueryResponse<int>(Any.Int());
            response.Successful.Should().BeTrue();

            response.Successful = false;

            response.Successful.Should().BeFalse();
        }

        /// <summary>
        /// Tests that OutcomeEntries can be modified after construction.
        /// Verifies that entries can be added to the list.
        /// </summary>
        [TestMethod]
        public void QueryResponse_OutcomeEntries_CanBeModified()
        {
            var response = new QueryResponse<string>(Any.String());
            var entry = new OutcomeEntry(Any.String(), Any.String());

            response.OutcomeEntries.Add(entry);

            response.OutcomeEntries.Should().HaveCount(1);
            response.OutcomeEntries[0].Should().Be(entry);
        }

        /// <summary>
        /// Tests that OutcomeEntries can be replaced with a new list.
        /// Verifies that the setter works correctly.
        /// </summary>
        [TestMethod]
        public void QueryResponse_OutcomeEntries_CanBeReplaced()
        {
            var response = new QueryResponse<int>();
            var newList = new List<IOutcomeEntry>
            {
                new OutcomeEntry(Any.String(), Any.String()),
                new OutcomeEntry(Any.String(), Any.String())
            };

            response.OutcomeEntries = newList;

            response.OutcomeEntries.Should().BeSameAs(newList);
            response.OutcomeEntries.Should().HaveCount(2);
        }

        #endregion

        #region Interface Implementation Tests

        /// <summary>
        /// Tests that QueryResponse implements IQueryResponse<TResult>.
        /// Verifies interface implementation.
        /// </summary>
        [TestMethod]
        public void QueryResponse_ImplementsIQueryResponse()
        {
            var response = new QueryResponse<int>(Any.Int());

            response.Should().BeAssignableTo<IQueryResponse<int>>();
        }

        #endregion

        #region Edge Case Tests

        /// <summary>
        /// Tests QueryResponse with default constructor and then modifying Successful.
        /// Verifies that Successful can be set to true manually.
        /// </summary>
        [TestMethod]
        public void QueryResponse_DefaultConstructor_SuccessfulCanBeSetManually()
        {
            var response = new QueryResponse<int>();
            response.Successful.Should().BeFalse();

            response.Successful = true;

            response.Successful.Should().BeTrue();
        }

        /// <summary>
        /// Tests QueryResponse with result constructor and multiple outcome entries.
        /// Verifies that successful responses can still have outcome entries (e.g., warnings).
        /// </summary>
        [TestMethod]
        public void QueryResponse_WithResult_CanHaveOutcomeEntries()
        {
            var response = new QueryResponse<string>(Any.String());
            var warningEntry = new OutcomeEntry(Any.String(), Any.String(), null, Severity.Warning);
            var infoEntry = new OutcomeEntry(Any.String(), Any.String(), null, Severity.Info);

            response.OutcomeEntries.Add(warningEntry);
            response.OutcomeEntries.Add(infoEntry);

            response.Successful.Should().BeTrue();
            response.OutcomeEntries.Should().HaveCount(2);
            response.OutcomeEntries[0].Severity.Should().Be(Severity.Warning);
            response.OutcomeEntries[1].Severity.Should().Be(Severity.Info);
        }

        #endregion
    }
}


