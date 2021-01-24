using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Minded.Validation;
using Service.Category.Validator;
using Builder;
using FluentAssertion.MSTest;

namespace Service.Category.Tests
{
    [TestClass]
    public class CategoryValidatorTest
    {
        private CategoryValidator SUT;

        [TestInitialize]
        public void TestInitialize()
        {
            SUT = new CategoryValidator();
        }

        [TestMethod]
        public async Task Validation_suceed_when_category_is_valid()
        {
            var category = Builder<Data.Entity.Category>.New().Build(e => { e.Name = "Category name"; });
            var result = await SUT.ValidateAsync(category);

            Assert.That.This(result)
                .IsNotNull().And()
                .Has(e => e.ValidationEntries.Count == 0).And()
                .HasProperty(r => r.IsValid).WithValue(true);
        }

        [TestMethod]
        public async Task Validation_fails_when_name_is_null()
        {
            var category = Builder<Data.Entity.Category>.New().Build(e => e.Name = null);
            var result = await SUT.ValidateAsync(category);

            Assert.That.This(result)
                .IsNotNull().And()
                .Has(e => e.ValidationEntries.Any(e =>
                e.PropertyName == nameof(category.Name) &&
                e.Severity == Severity.Error &&
                e.ErrorMessage == "{0} is mandatory")).And()
                .HasProperty(r => r.IsValid)
                .WithValue(false);
        }

        [TestMethod]
        public async Task Validation_fails_when_name_is_empty()
        {
            var category = Builder<Data.Entity.Category>.New().Build(e => { e.Name = ""; });
            var result = await SUT.ValidateAsync(category);

            Assert.IsNotNull(result);
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.ValidationEntries.Any(e =>
                e.PropertyName == nameof(category.Name) &&
                e.Severity == Severity.Error &&
                e.ErrorMessage == "{0} is mandatory"));
        }

        [TestMethod]
        public async Task Validation_fails_when_name_is_made_of_spaces()
        {
            var category = Builder<Data.Entity.Category>.New().Build(e => { e.Name = "    "; });
            var result = await SUT.ValidateAsync(category);

            Assert.IsNotNull(result);
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.ValidationEntries.Any(e =>
                e.PropertyName == nameof(category.Name) &&
                e.Severity == Severity.Error &&
                e.ErrorMessage == "{0} is mandatory"));
        }
    }
}
