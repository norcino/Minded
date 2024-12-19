using Minded.Extensions.Caching.Abstractions.Decorator;

namespace Minded.Extensions.Caching.Abstractions.Tests
{
    [TestClass]
    public class CachingDecoratorAttributeValidatorTests
    {
        private CachingDecoratorAttributeValidator _sut;

        [TestInitialize]
        public void Setup()
        {
            _sut = new CachingDecoratorAttributeValidator();
        }

        [TestMethod]
        public void Validate_WhenTypeWithCacheAttributeDoesNotImplementIGenerateCacheKey_ThrowsInvalidOperationException()
        {
            // Only needed to make sure the assembly is included in the bin folder during compilation
            var querySupposedToFail = new InvalidTestQueryWithCachingAttribute();
            Assert.IsNotNull(querySupposedToFail);

            try
            {
                _sut.Validate((a) => a.Name == "Minded.Testing.Common");
            }
            catch (InvalidOperationException ex)
            {
                Assert.AreEqual("The class Minded.Extensions.Caching.Abstractions.Tests.InvalidTestQueryWithCachingAttribute has a CacheAttribute (or a derived class) but does not implement IGenerateCacheKey.", ex.Message);
                return;
            }

            Assert.Fail("The method did not throw an exception as expected.");
        }

        [TestMethod]
        public void Validate_WhenAllTypesWithCacheAttributeImplementIGenerateCacheKey_DoesNotThrowException()
        {
            _sut.Validate((a) => a.Name == "Minded.Extensions.Caching.Abstractions.Tests");
        }
    }
}
