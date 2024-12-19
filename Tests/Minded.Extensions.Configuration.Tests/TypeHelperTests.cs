namespace Minded.Extensions.Configuration.Tests
{
    [TestClass]
    public class TypeHelperTests
    {
        public interface ITestInterface { }
        public interface IGenericInterface<T> { }
        public class TestClass : ITestInterface { }
        public class GenericTestClass : IGenericInterface<int>, ITestInterface { }

        [TestMethod]
        public void IsInterfaceOrImplementation_ShouldReturnsTrue_WhenInterfaceItselfIsTested()
        {
            Type interfaceType = typeof(ITestInterface);
            Type type = typeof(ITestInterface);

            bool result = TypeHelper.IsInterfaceOrImplementation(interfaceType, type);

            Assert.IsTrue(result);
        }

        [TestMethod]
        public void IsInterfaceOrImplementation_ShouldReturnTrue_WhenClassImplementingInterfaceIsTested()
        {
            Type interfaceType = typeof(ITestInterface);
            Type type = typeof(TestClass);

            bool result = TypeHelper.IsInterfaceOrImplementation(interfaceType, type);

            Assert.IsTrue(result);
        }

        [TestMethod]
        public void IsInterfaceOrImplementation_ShouldReturnTrue_WhenGenericInterfaceTypeImplementsGeneric()
        {
            Type interfaceType = typeof(IGenericInterface<>);
            Type type = typeof(IGenericInterface<int>);

            bool result = TypeHelper.IsInterfaceOrImplementation(interfaceType, type);

            Assert.IsTrue(result);
        }

        [TestMethod]
        public void IsInterfaceOrImplementation_ShouldReturnTrue_WhenGenericClassImplementsGenericInterface()
        {
            Type interfaceType = typeof(IGenericInterface<>);
            Type type = typeof(GenericTestClass);

            bool result = TypeHelper.IsInterfaceOrImplementation(interfaceType, type);

            Assert.IsTrue(result);
        }

        [TestMethod]
        public void IsInterfaceOrImplementation_ShouldReturnFalse_WhenTypeUrelated()
        {
            Type interfaceType = typeof(ITestInterface);
            Type type = typeof(string);

            bool result = TypeHelper.IsInterfaceOrImplementation(interfaceType, type);

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void IsInterfaceOrImplementation_ShouldReturnTrue_WhenTypeImplementsParentInterface()
        {
            Type interfaceType = typeof(ITestInterface);
            Type type = typeof(GenericTestClass);

            bool result = TypeHelper.IsInterfaceOrImplementation(interfaceType, type);

            Assert.IsTrue(result);
        }
    }
}
