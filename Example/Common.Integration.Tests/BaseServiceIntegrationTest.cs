using System;
using Minded.Mediator;

namespace Common.IntegrationTests
{    
    public class BaseServiceIntegrationTest : BaseIdempotentIntegrationTest
    {
        protected IMediator mediator;

        public BaseServiceIntegrationTest() : base()
        {
            mediator = new Mediator(ServiceProvider);
        }
    }
}
