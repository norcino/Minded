using AnonymousData;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Minded.Framework.CQRS.Abstractions;
using Minded.Framework.CQRS.Command;

namespace Minded.Framework.CQRS.Tests
{
    /// <summary>
    /// Unit tests for the CommandResponse and CommandResponse<TResult> classes.
    /// Tests all constructors, properties, and behavior to ensure correct functionality.
    /// </summary>
    [TestClass]
    public class CommandResponseTests
    {
        #region CommandResponse (Non-Generic) Tests

        /// <summary>
        /// Tests the default constructor of CommandResponse.
        /// Verifies that OutcomeEntries is initialized as an empty list.
        /// </summary>
        [TestMethod]
        public void CommandResponse_DefaultConstructor_InitializesOutcomeEntriesAsEmptyList()
        {
            var response = new CommandResponse();

            response.OutcomeEntries.Should().NotBeNull();
            response.OutcomeEntries.Should().BeEmpty();
            response.Successful.Should().BeFalse();
        }

        /// <summary>
        /// Tests that Successful property can be set.
        /// Verifies that the setter works correctly.
        /// </summary>
        [TestMethod]
        public void CommandResponse_Successful_CanBeSet()
        {
            var response = new CommandResponse();

            response.Successful = true;

            response.Successful.Should().BeTrue();
        }

        /// <summary>
        /// Tests that OutcomeEntries can be modified after construction.
        /// Verifies that entries can be added to the list.
        /// </summary>
        [TestMethod]
        public void CommandResponse_OutcomeEntries_CanBeModified()
        {
            var response = new CommandResponse();
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
        public void CommandResponse_OutcomeEntries_CanBeReplaced()
        {
            var response = new CommandResponse();
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

        #region CommandResponse<TResult> (Generic) Tests

        /// <summary>
        /// Tests the default constructor of CommandResponse<TResult>.
        /// Verifies that OutcomeEntries is initialized and Result is default.
        /// </summary>
        [TestMethod]
        public void CommandResponseGeneric_DefaultConstructor_InitializesCorrectly()
        {
            var response = new CommandResponse<int>();

            response.OutcomeEntries.Should().NotBeNull();
            response.OutcomeEntries.Should().BeEmpty();
            response.Result.Should().Be(0);
            response.Successful.Should().BeFalse();
        }

        /// <summary>
        /// Tests the constructor with result parameter.
        /// Verifies that Result is set correctly.
        /// </summary>
        [TestMethod]
        public void CommandResponseGeneric_ConstructorWithResult_SetsResultCorrectly()
        {
            var expectedResult = Any.Int();

            var response = new CommandResponse<int>(expectedResult);

            response.Result.Should().Be(expectedResult);
            response.OutcomeEntries.Should().NotBeNull();
            response.OutcomeEntries.Should().BeEmpty();
        }

        /// <summary>
        /// Tests CommandResponse<TResult> with reference type.
        /// Verifies that complex objects can be used as results.
        /// </summary>
        [TestMethod]
        public void CommandResponseGeneric_WithReferenceType_StoresObjectCorrectly()
        {
            var expectedResult = new { Id = Any.Int(), Name = Any.String() };

            var response = new CommandResponse<object>(expectedResult);

            response.Result.Should().Be(expectedResult);
        }

        /// <summary>
        /// Tests CommandResponse<TResult> with null result.
        /// Verifies that null values are handled correctly for reference types.
        /// </summary>
        [TestMethod]
        public void CommandResponseGeneric_WithNullResult_StoresNullCorrectly()
        {
            var response = new CommandResponse<string>((string)null);
            response.Result.Should().BeNull();
        }

        /// <summary>
        /// Tests that Result property is read-only.
        /// Verifies that Result can only be set through constructor.
        /// </summary>
        [TestMethod]
        public void CommandResponseGeneric_Result_IsReadOnly()
        {
            var initialResult = Any.String();
            var response = new CommandResponse<string>(initialResult);

            response.Result.Should().Be(initialResult);
        }

        /// <summary>
        /// Tests that generic CommandResponse inherits from non-generic CommandResponse.
        /// Verifies inheritance and that base properties are accessible.
        /// </summary>
        [TestMethod]
        public void CommandResponseGeneric_InheritsFromCommandResponse()
        {
            var response = new CommandResponse<int>(Any.Int());

            response.Should().BeAssignableTo<CommandResponse>();
            response.Should().BeAssignableTo<ICommandResponse<int>>();
            response.Successful = true;
            response.Successful.Should().BeTrue();
        }

        /// <summary>
        /// Tests CommandResponse<TResult> with OutcomeEntries.
        /// Verifies that outcome entries can be added to generic response.
        /// </summary>
        [TestMethod]
        public void CommandResponseGeneric_OutcomeEntries_CanBeAdded()
        {
            var response = new CommandResponse<string>(Any.String());
            var entry = new OutcomeEntry(Any.String(), Any.String(), null, Severity.Error);

            response.OutcomeEntries.Add(entry);

            response.OutcomeEntries.Should().HaveCount(1);
            response.OutcomeEntries[0].Should().Be(entry);
        }

        #endregion
    }
}


