using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Minded.Validation;
using Service.Category.Validator;
using Builder;
using FluentAssertions;

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
            var category = Builder<Data.Entity.Category>.New().Build();
            var result = await _sut.ValidateAsync(category);

            result.ValidationEntries.Should().BeEmpty();
            result.IsValid.Should().BeTrue();
        }

        [TestMethod]
        public async Task Validation_fails_when_name_is_null()
        {
            var category = Builder<Data.Entity.Category>.New().Build(e => e.Name = null);
            var result = await _sut.ValidateAsync(category);

            result.IsValid.Should().BeFalse();
            result.ValidationEntries.Any(e =>
                    e.PropertyName == nameof(category.Name) &&
                    e.Severity == Severity.Error &&
                    e.ErrorMessage == "{0} is mandatory")
                .Should().BeTrue();
        }

        [TestMethod]
        public async Task Validation_fails_when_name_is_empty()
        {
            var category = Builder<Data.Entity.Category>.New().Build(e => { e.Name = ""; });
            var result = await _sut.ValidateAsync(category);

            result.IsValid.Should().BeFalse();
            result.ValidationEntries.Any(e =>
                    e.PropertyName == nameof(category.Name) &&
                    e.Severity == Severity.Error &&
                    e.ErrorMessage == "{0} is mandatory")
                .Should().BeTrue();
        }

        [TestMethod]
        public async Task Validation_fails_when_name_is_made_of_spaces()
        {
            var category = Builder<Data.Entity.Category>.New().Build(e => { e.Name = "    "; });
            var result = await _sut.ValidateAsync(category);

            result.IsValid.Should().BeFalse();
            result.ValidationEntries.Any(e =>
                    e.PropertyName == nameof(category.Name) &&
                    e.Severity == Severity.Error &&
                    e.ErrorMessage == "{0} is mandatory")
                .Should().BeTrue();
        }
    }
}
