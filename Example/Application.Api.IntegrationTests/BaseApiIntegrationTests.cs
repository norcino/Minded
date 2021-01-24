//using System;
//using Common.IntegrationTests;
//using Data.Common.Testing.Builder;
//using Data.Entity;
//using Microsoft.VisualStudio.TestTools.UnitTesting;
//using System.Collections.Generic;
//using System.Threading.Tasks;
//using Data.Context;
//using Microsoft.EntityFrameworkCore.Storage;
//using System.Linq;
//using Common.Configuration;
//using Microsoft.EntityFrameworkCore;
//using System.Data;

//namespace Application.Api.IntegrationTests
//{
//    [TestClass]
//    public class BaseApiIntegrationTests
//    {
//        protected const int MaxPageItemNumber = 100;
//        protected TestServerApiClient _client;
//        protected IMindedExampleContext _context;

//        [TestInitialize]
//        public void BaseTestInitialize()
//        {
//            _context = ContextProvider.GetContext();
//            _client = new TestServerApiClient();
//            ContextProvider.ResetDatabase();
//        }

//        [TestCleanup]
//        public void BaseTestCleanup()
//        {
//            _client?.Dispose();
//            _context?.Dispose();
//            ContextProvider.Dispose();
//        }
//    }
//}
