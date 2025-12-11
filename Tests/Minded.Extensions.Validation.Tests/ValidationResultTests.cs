using AnonymousData;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Minded.Extensions.Validation;
using Minded.Framework.CQRS;
using Minded.Framework.CQRS.Abstractions;
using System.Collections.Generic;
using System.Linq;

namespace Minded.Extensions.Validation.Tests
{
    /// <summary>
    /// Unit tests for the ValidationResult class.
    /// Validates the behavior of validation result creation, property access, and merging functionality.
    /// </summary>
    [TestClass]
    public class ValidationResultTests
    {
        /// <summary>
        /// Verifies that a new ValidationResult has an empty OutcomeEntries collection.
        /// </summary>
        [TestMethod]
        public void Constructor_Default_InitializesEmptyOutcomeEntries()
        {
            var sut = new ValidationResult();

            sut.OutcomeEntries.Should().NotBeNull();
            sut.OutcomeEntries.Should().BeEmpty();
        }

        /// <summary>
        /// Verifies that a new ValidationResult is valid when no entries are present.
        /// </summary>
        [TestMethod]
        public void IsValid_WhenNoEntries_ReturnsTrue()
        {
            var sut = new ValidationResult();

            sut.IsValid.Should().BeTrue();
        }

        /// <summary>
        /// Verifies that ValidationResult is valid when all entries have non-error severity.
        /// </summary>
        [TestMethod]
        public void IsValid_WhenAllEntriesAreWarnings_ReturnsTrue()
        {
            var entries = new List<IOutcomeEntry>
            {
                new OutcomeEntry(Any.String(), Any.String(), null, Severity.Warning),
                new OutcomeEntry(Any.String(), Any.String(), null, Severity.Info)
            };

            var sut = new ValidationResult(entries);

            sut.IsValid.Should().BeTrue();
        }

        /// <summary>
        /// Verifies that ValidationResult is invalid when at least one entry has error severity.
        /// </summary>
        [TestMethod]
        public void IsValid_WhenAtLeastOneEntryIsError_ReturnsFalse()
        {
            var entries = new List<IOutcomeEntry>
            {
                new OutcomeEntry(Any.String(), Any.String(), null, Severity.Warning),
                new OutcomeEntry(Any.String(), Any.String(), null, Severity.Error),
                new OutcomeEntry(Any.String(), Any.String(), null, Severity.Info)
            };

            var sut = new ValidationResult(entries);

            sut.IsValid.Should().BeFalse();
        }

        /// <summary>
        /// Verifies that ValidationResult constructor with failures parameter copies the entries.
        /// </summary>
        [TestMethod]
        public void Constructor_WithFailures_CopiesEntries()
        {
            var entries = new List<IOutcomeEntry>
            {
                new OutcomeEntry(Any.String(), Any.String(), null, Severity.Error),
                new OutcomeEntry(Any.String(), Any.String(), null, Severity.Warning)
            };

            var sut = new ValidationResult(entries);

            sut.OutcomeEntries.Should().HaveCount(2);
            sut.OutcomeEntries.Should().Contain(entries);
        }

        /// <summary>
        /// Verifies that ValidationResult constructor filters out null entries.
        /// </summary>
        [TestMethod]
        public void Constructor_WithNullEntries_FiltersOutNulls()
        {
            var entries = new List<IOutcomeEntry>
            {
                new OutcomeEntry(Any.String(), Any.String(), null, Severity.Error),
                null,
                new OutcomeEntry(Any.String(), Any.String(), null, Severity.Warning),
                null
            };

            var sut = new ValidationResult(entries);

            sut.OutcomeEntries.Should().HaveCount(2);
            sut.OutcomeEntries.Should().NotContainNulls();
        }

        /// <summary>
        /// Verifies that Merge adds all entries from the other validation result.
        /// </summary>
        [TestMethod]
        public void Merge_AddsEntriesFromOtherValidationResult()
        {
            var entry1 = new OutcomeEntry(Any.String(), Any.String(), null, Severity.Error);
            var entry2 = new OutcomeEntry(Any.String(), Any.String(), null, Severity.Warning);
            var entry3 = new OutcomeEntry(Any.String(), Any.String(), null, Severity.Info);

            var sut = new ValidationResult(new[] { entry1 });
            var other = new ValidationResult(new[] { entry2, entry3 });

            sut.Merge(other);

            sut.OutcomeEntries.Should().HaveCount(3);
            sut.OutcomeEntries.Should().Contain(new[] { entry1, entry2, entry3 });
        }

        /// <summary>
        /// Verifies that Merge returns the current instance for fluent chaining.
        /// </summary>
        [TestMethod]
        public void Merge_ReturnsCurrentInstance()
        {
            var sut = new ValidationResult();
            var other = new ValidationResult();

            IValidationResult result = sut.Merge(other);

            result.Should().BeSameAs(sut);
        }

        /// <summary>
        /// Verifies that Merge with empty validation result does not add entries.
        /// </summary>
        [TestMethod]
        public void Merge_WithEmptyValidationResult_DoesNotAddEntries()
        {
            var entry = new OutcomeEntry(Any.String(), Any.String(), null, Severity.Error);
            var sut = new ValidationResult(new[] { entry });
            var other = new ValidationResult();

            sut.Merge(other);

            sut.OutcomeEntries.Should().HaveCount(1);
            sut.OutcomeEntries.Should().Contain(entry);
        }
    }
}

