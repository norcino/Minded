using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MindedExample.Application.Category.Validator;
using Builder;
using FluentAssertions;
using Minded.Framework.CQRS.Abstractions;
using Minded.Extensions.Validation;

namespace MindedExample.Application.Category.UnitTests
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
            MindedExample.Domain.Category category = Builder<MindedExample.Domain.Category>.New().Build();
            IValidationResult result = await _sut.ValidateAsync(category);

            result.OutcomeEntries.Should().BeEmpty();
            result.IsValid.Should().BeTrue();
        }

        [TestMethod]
        public async Task Validation_fails_when_name_is_null()
        {
            MindedExample.Domain.Category category = Builder<MindedExample.Domain.Category>.New().Build(e => e.Name = null);
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
            MindedExample.Domain.Category category = Builder<MindedExample.Domain.Category>.New().Build(e => { e.Name = ""; });
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
            MindedExample.Domain.Category category = Builder<MindedExample.Domain.Category>.New().Build(e => { e.Name = "    "; });
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
