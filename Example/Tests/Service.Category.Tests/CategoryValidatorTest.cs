using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Service.Category.Validator;
using Builder;
using FluentAssertions;
using Minded.Framework.CQRS.Abstractions;
using Minded.Extensions.Validation;

namespace Service.Category.Tests
{
    [TestClass]
    public class CategoryValidatorTest
    {
        private CategoryValidator _sut;

        [TestInitialize]
        public void TestInitialize()
        {
            _sut = new CategoryValidator();
        }

        [TestMethod]
        public async Task Validation_suceed_when_category_is_valid()
        {
            Data.Entity.Category category = Builder<Data.Entity.Category>.New().Build();
            IValidationResult result = await _sut.ValidateAsync(category);

            result.OutcomeEntries.Should().BeEmpty();
            result.IsValid.Should().BeTrue();
        }

        [TestMethod]
        public async Task Validation_fails_when_name_is_null()
        {
            Data.Entity.Category category = Builder<Data.Entity.Category>.New().Build(e => e.Name = null);
            IValidationResult result = await _sut.ValidateAsync(category);

            result.IsValid.Should().BeFalse();
            result.OutcomeEntries.Any(e =>
                    e.PropertyName == nameof(category.Name) &&
                    e.Severity == Severity.Error &&
                    e.Message == "{0} is mandatory")
                .Should().BeTrue();
        }

        [TestMethod]
        public async Task Validation_fails_when_name_is_empty()
        {
            Data.Entity.Category category = Builder<Data.Entity.Category>.New().Build(e => { e.Name = ""; });
            IValidationResult result = await _sut.ValidateAsync(category);

            result.IsValid.Should().BeFalse();
            result.OutcomeEntries.Any(e =>
                    e.PropertyName == nameof(category.Name) &&
                    e.Severity == Severity.Error &&
                    e.Message == "{0} is mandatory")
                .Should().BeTrue();
        }

        [TestMethod]
        public async Task Validation_fails_when_name_is_made_of_spaces()
        {
            Data.Entity.Category category = Builder<Data.Entity.Category>.New().Build(e => { e.Name = "    "; });
            IValidationResult result = await _sut.ValidateAsync(category);

            result.IsValid.Should().BeFalse();
            result.OutcomeEntries.Any(e =>
                    e.PropertyName == nameof(category.Name) &&
                    e.Severity == Severity.Error &&
                    e.Message == "{0} is mandatory")
                .Should().BeTrue();
        }
    }
}
