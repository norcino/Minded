using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Minded.Extensions.Configuration.Tests
{
    [TestClass]
    public class TypeHelperTests
    {
        public interface ITestInterface { }
        public interface IGenericInterface<T> { }
        public class TestClass : ITestInterface { }
        public class GenericTestClass : IGenericInterface<int> { }

        [TestMethod]
        public void IsInterfaceOrImplementation_InterfaceType_ReturnsTrue()
        {
            Type interfaceType = typeof(ITestInterface);
            Type type = typeof(ITestInterface);

            bool result = TypeHelper.IsInterfaceOrImplementation(interfaceType, type);

            Assert.IsTrue(result);
        }

        [TestMethod]
        public void IsInterfaceOrImplementation_ImplementationType_ReturnsTrue()
        {
            Type interfaceType = typeof(ITestInterface);
            Type type = typeof(TestClass);

            bool result = TypeHelper.IsInterfaceOrImplementation(interfaceType, type);

            Assert.IsTrue(result);
        }

        [TestMethod]
        public void IsInterfaceOrImplementation_GenericInterfaceType_ReturnsTrue()
        {
            Type interfaceType = typeof(IGenericInterface<>);
            Type type = typeof(IGenericInterface<int>);

            bool result = TypeHelper.IsInterfaceOrImplementation(interfaceType, type);

            Assert.IsTrue(result);
        }

        [TestMethod]
        public void IsInterfaceOrImplementation_GenericImplementationType_ReturnsTrue()
        {
            Type interfaceType = typeof(IGenericInterface<>);
            Type type = typeof(GenericTestClass);

            bool result = TypeHelper.IsInterfaceOrImplementation(interfaceType, type);

            Assert.IsTrue(result);
        }

        [TestMethod]
        public void IsInterfaceOrImplementation_UnrelatedType_ReturnsFalse()
        {
            Type interfaceType = typeof(ITestInterface);
            Type type = typeof(string);

            bool result = TypeHelper.IsInterfaceOrImplementation(interfaceType, type);

            Assert.IsFalse(result);
        }
    }
}
