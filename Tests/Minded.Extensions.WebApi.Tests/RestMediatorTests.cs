using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Minded.Framework.CQRS.Command;
using Minded.Framework.CQRS.Query;
using Minded.Framework.Mediator;
using Minded.Extensions.WebApi;
using Minded.Extensions.Configuration;
using System.Xml.Serialization;
using AnonymousData;
using Minded.Framework.CQRS.Abstractions;

namespace Minded.Extensions.WebApi.Tests
{
    public class TestCommand : ICommandResponse<string>
    {
        public string Result { get; set; }

        public bool Successful { get; set; }

        public List<IOutcomeEntry> OutcomeEntries { get; set;  }
    }

    [TestClass]
    public class RestMediatorTests
    {
        private Mock<IRulesProcessor> _rulesProcessorMock;
        private Mock<IServiceProvider> _serviceProviderMock;
        private Mock<IQuery<string>> _queryWithSimpleResultMock;
        private Mock<ICommand<string>> _commandWithResultMock;
        private Mock<ICommand> _commandWithoutResultMock;
        private Mock<ICommandResponse> _commandResponseMock;
        private ICommandResponse<string> _commandResponseWithResult;
        private Mock<IQueryHandler<IQuery<string>,string>> _queryHandlerWithSimpleResultMock;
        private Mock<ICommandHandler<ICommand<string>, string>> _commandHandlerWithResultMock;
        private Mock<ICommandHandler<ICommand>> _commandHandlerWithoutResultMock;

        private RestMediator _sut;

        [TestInitialize]
        public void Setup()
        {
            _rulesProcessorMock = new Mock<IRulesProcessor>();
            _serviceProviderMock = new Mock<IServiceProvider>();

            _commandResponseMock = new Mock<ICommandResponse>();
            _commandResponseWithResult = new TestCommand();
            _queryWithSimpleResultMock = new Mock<IQuery<string>>();
            _commandWithResultMock = new Mock<ICommand<string>>();
            _commandWithoutResultMock = new Mock<ICommand>();

            _queryHandlerWithSimpleResultMock = new Mock<IQueryHandler<IQuery<string>, string>>();
            _commandHandlerWithResultMock = new Mock<ICommandHandler<ICommand<string>, string>>();
            _commandHandlerWithoutResultMock = new Mock<ICommandHandler<ICommand>>();

            _sut = new RestMediator(_serviceProviderMock.Object, _rulesProcessorMock.Object);
        }

        [TestMethod]
        public async Task ProcessRestQueryAsync_ShouldReturnExpectedResult()
        {
            var expectedResult = Any.String();
            var operation = Any.In<RestOperation>();

            _queryHandlerWithSimpleResultMock.Setup(h => h.HandleAsync(_queryWithSimpleResultMock.Object)).Returns(Task.FromResult(expectedResult));
            _serviceProviderMock.Setup(sp => sp.GetService(It.IsAny<Type>())).Returns(_queryHandlerWithSimpleResultMock.Object);

            _rulesProcessorMock.Setup(rp => rp.ProcessQueryRules(operation, expectedResult)).Returns(new OkObjectResult(expectedResult));

            var result = await _sut.ProcessRestQueryAsync(operation, _queryWithSimpleResultMock.Object);

            Assert.IsInstanceOfType(result, typeof(OkObjectResult));
            var okResult = result as OkObjectResult;
            Assert.AreEqual(expectedResult, okResult.Value);
        }

        [TestMethod]
        public async Task ProcessRestCommandAsync_ShouldReturnExpectedResult()
        {
            var expectedResponse = new OkResult();
            var operation = Any.In<RestOperation>();

            _commandHandlerWithoutResultMock.Setup(h => h.HandleAsync(_commandWithoutResultMock.Object)).Returns(Task.FromResult(_commandResponseMock.Object));
            _serviceProviderMock.Setup(sp => sp.GetService(It.IsAny<Type>())).Returns(_commandHandlerWithoutResultMock.Object);
            _rulesProcessorMock.Setup(rp => rp.ProcessCommandRules(operation, _commandResponseMock.Object)).Returns(expectedResponse);

            var result = await _sut.ProcessRestCommandAsync(operation, _commandWithoutResultMock.Object);

            Assert.AreEqual(result, expectedResponse);
        }

        [TestMethod]
        public async Task ProcessRestCommandAsync_ShouldReturnExpectedResult_WhenUsingCommandResult()
        {
            var expectedResponse = new OkResult();
            var operation = Any.In<RestOperation>();

            _commandHandlerWithResultMock.Setup(h => h.HandleAsync(_commandWithResultMock.Object)).Returns(Task.FromResult(_commandResponseWithResult));
            _serviceProviderMock.Setup(sp => sp.GetService(It.IsAny<Type>())).Returns(_commandHandlerWithResultMock.Object);
            //_rulesProcessorMock.Setup(rp => rp.ProcessCommandRules<string>(operation, It.Is<ICommandResponse<string>>(r => r.re))).Returns(expectedResponse);
            _rulesProcessorMock.Setup(rp => rp.ProcessCommandRules<string>(operation, _commandResponseWithResult)).Returns(expectedResponse);

            var result = await _sut.ProcessRestCommandAsync(operation, _commandWithResultMock.Object);

            Assert.AreEqual(result, expectedResponse);
        }
    }
}
