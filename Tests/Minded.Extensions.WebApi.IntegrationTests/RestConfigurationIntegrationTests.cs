using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Minded.Extensions.Exception;
using Minded.Extensions.WebApi;
using Minded.Framework.CQRS;
using Minded.Framework.CQRS.Abstractions;
using Minded.Framework.CQRS.Command;
using Minded.Framework.CQRS.Query;

namespace Minded.Extensions.WebApi.IntegrationTests
{
    /// <summary>
    /// Integration tests for REST configuration as documented in DefaultRestRulesProvider.
    /// Tests verify that the complete flow from RestMediator → RulesProcessor → Rules produces
    /// the correct HTTP status codes and response content for each REST operation scenario.
    /// 
    /// REST Configuration Coverage:
    /// 
    /// Create:
    ///   - Success: 201 Created - Return created object
    ///   - Failure: 400 Bad Request - Return details about the failure
    /// 
    /// Update:
    ///   - Success: 200 Ok - Return the updated object
    ///   - Success: 204 NoContent
    ///   - Failure: 404 NotFound - The targeted entity identifier does not exist
    ///   - Failure: 400 Bad Request - Return details about the failure
    /// 
    /// Patch/Put:
    ///   - Success: 200 Ok - Return the patched object
    ///   - Success: 204 NoContent
    ///   - Failure: 404 NotFound - The targeted entity identifier does not exist
    ///   - Failure: 400 Bad Request - Return details about the failure
    /// 
    /// Delete:
    ///   - Success: 200 Ok - No content
    ///   - Failure: 404 NotFound - The targeted entity does not exist
    /// 
    /// Get:
    ///   - Success: 200 Ok - With the list of resulting entities matching the search criteria
    ///   - Success: 200 Ok - With an empty array
    /// 
    /// Get specific:
    ///   - Success: 200 Ok - The entity matching the identifier specified is returned as content
    ///   - Failure: 404 NotFound - No content
    /// 
    /// Action:
    ///   - Success: 200 Ok - Return content where appropriate
    ///   - Success: 204 NoContent
    ///   - Failure: 400 Bad Request - Return details about the failure
    /// 
    /// Generic results:
    ///   - Authorization error: 401 Unauthorized
    ///   - Authentication error: 403 Forbidden
    /// </summary>
    [TestClass]
    public class RestConfigurationIntegrationTests
    {
        private IRestMediator _restMediator;
        private IServiceProvider _serviceProvider;

        [TestInitialize]
        public void Setup()
        {
            var services = new ServiceCollection();
            
            // Register handlers
            services.AddTransient<ICommandHandler<CreateEntityCommand, Entity>, CreateEntityCommandHandler>();
            services.AddTransient<ICommandHandler<CreateEntityWithoutContentCommand>, CreateEntityWithoutContentCommandHandler>();
            services.AddTransient<ICommandHandler<UpdateEntityCommand, Entity>, UpdateEntityCommandHandler>();
            services.AddTransient<ICommandHandler<UpdateEntityWithoutContentCommand>, UpdateEntityWithoutContentCommandHandler>();
            services.AddTransient<ICommandHandler<PatchEntityCommand, Entity>, PatchEntityCommandHandler>();
            services.AddTransient<ICommandHandler<PatchEntityWithoutContentCommand>, PatchEntityWithoutContentCommandHandler>();
            services.AddTransient<ICommandHandler<DeleteEntityCommand>, DeleteEntityCommandHandler>();
            services.AddTransient<ICommandHandler<ExecuteActionCommand>, ExecuteActionCommandHandler>();
            services.AddTransient<ICommandHandler<ExecuteActionWithContentCommand, Entity>, ExecuteActionWithContentCommandHandler>();
            services.AddTransient<ICommandHandler<ExecuteActionWithResultContentCommand, int>, ExecuteActionWithResultContentCommandHandler>();
            services.AddTransient<IQueryHandler<GetEntityByIdQuery, IQueryResponse<Entity>>, GetEntityByIdQueryHandler>();
            services.AddTransient<IQueryHandler<GetEntitiesQuery, List<Entity>>, GetEntitiesQueryHandler>();
            
            // Register WebApi components
            services.AddTransient<IRestRulesProvider, DefaultRestRulesProvider>();
            services.AddTransient<IRulesProcessor, DefaultRulesProcessor>();
            services.AddTransient<IRestMediator, RestMediator>();
            
            _serviceProvider = services.BuildServiceProvider();
            _restMediator = _serviceProvider.GetRequiredService<IRestMediator>();
        }

        #region Create Tests

        /// <summary>
        /// Create - Success: 201 Created - Return created object
        /// </summary>
        [TestMethod]
        public async Task Create_Success_Returns201CreatedWithContent()
        {
            var command = new CreateEntityCommand { Id = 1, Name = "Test Entity", ShouldSucceed = true };

            var result = await _restMediator.ProcessRestCommandAsync(RestOperation.CreateWithContent, command);

            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult.StatusCode.Should().Be((int)HttpStatusCode.Created);
            objectResult.Value.Should().BeOfType<Entity>();
            var entity = objectResult.Value as Entity;
            entity.Id.Should().Be(1);
            entity.Name.Should().Be("Test Entity");
        }

        /// <summary>
        /// Create - Success: 201 Created - No content (Create without content)
        /// </summary>
        [TestMethod]
        public async Task Create_SuccessWithoutContent_Returns201CreatedNoContent()
        {
            var command = new CreateEntityWithoutContentCommand { Id = 1, ShouldSucceed = true };

            var result = await _restMediator.ProcessRestCommandAsync(RestOperation.Create, command);

            result.Should().BeOfType<StatusCodeResult>();
            var statusResult = result as StatusCodeResult;
            statusResult.StatusCode.Should().Be((int)HttpStatusCode.Created);
        }

        /// <summary>
        /// Create - Failure: 400 Bad Request - Return details about the failure
        /// </summary>
        [TestMethod]
        public async Task Create_Failure_Returns400BadRequestWithDetails()
        {
            var command = new CreateEntityCommand { Id = 1, Name = "Invalid", ShouldSucceed = false };

            var result = await _restMediator.ProcessRestCommandAsync(RestOperation.CreateWithContent, command);

            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
            objectResult.Value.Should().BeOfType<CommandResponse<Entity>>();
            var response = objectResult.Value as CommandResponse<Entity>;
            response.Successful.Should().BeFalse();
            response.OutcomeEntries.Should().NotBeEmpty();
        }

        #endregion

        #region Update Tests

        /// <summary>
        /// Update - Success: 200 Ok - Return the updated object
        /// </summary>
        [TestMethod]
        public async Task Update_Success_Returns200OkWithContent()
        {
            var command = new UpdateEntityCommand { Id = 1, Name = "Updated Entity", ShouldSucceed = true, EntityExists = true };

            var result = await _restMediator.ProcessRestCommandAsync(RestOperation.UpdateWithContent, command);

            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult.StatusCode.Should().Be((int)HttpStatusCode.OK);
            objectResult.Value.Should().BeOfType<Entity>();
            var entity = objectResult.Value as Entity;
            entity.Id.Should().Be(1);
            entity.Name.Should().Be("Updated Entity");
        }

        /// <summary>
        /// Update - Success: 204 NoContent
        /// </summary>
        [TestMethod]
        public async Task Update_SuccessWithoutContent_Returns204NoContent()
        {
            var command = new UpdateEntityWithoutContentCommand { Id = 1, ShouldSucceed = true, EntityExists = true };

            var result = await _restMediator.ProcessRestCommandAsync(RestOperation.Update, command);

            result.Should().BeOfType<StatusCodeResult>();
            var statusResult = result as StatusCodeResult;
            statusResult.StatusCode.Should().Be((int)HttpStatusCode.NoContent);
        }

        /// <summary>
        /// Update - Failure: 404 NotFound - The targeted entity identifier does not exist
        /// </summary>
        [TestMethod]
        public async Task Update_EntityNotFound_Returns404NotFound()
        {
            var command = new UpdateEntityCommand { Id = 999, Name = "Non-existent", ShouldSucceed = false, EntityExists = false };

            var result = await _restMediator.ProcessRestCommandAsync(RestOperation.UpdateWithContent, command);

            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult.StatusCode.Should().Be((int)HttpStatusCode.NotFound);
        }

        /// <summary>
        /// Update - Failure: 400 Bad Request - Return details about the failure
        /// </summary>
        [TestMethod]
        public async Task Update_Failure_Returns400BadRequestWithDetails()
        {
            var command = new UpdateEntityCommand { Id = 1, Name = "Invalid", ShouldSucceed = false, EntityExists = true };

            var result = await _restMediator.ProcessRestCommandAsync(RestOperation.UpdateWithContent, command);

            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
            objectResult.Value.Should().BeOfType<CommandResponse<Entity>>();
            var response = objectResult.Value as CommandResponse<Entity>;
            response.Successful.Should().BeFalse();
            response.OutcomeEntries.Should().NotBeEmpty();
        }

        #endregion

        #region Patch Tests

        /// <summary>
        /// Patch - Success: 200 Ok - Return the patched object
        /// </summary>
        [TestMethod]
        public async Task Patch_Success_Returns200OkWithContent()
        {
            var command = new PatchEntityCommand { Id = 1, Name = "Patched Entity", ShouldSucceed = true, EntityExists = true };

            var result = await _restMediator.ProcessRestCommandAsync(RestOperation.PatchWithContent, command);

            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult.StatusCode.Should().Be((int)HttpStatusCode.OK);
            objectResult.Value.Should().BeOfType<Entity>();
            var entity = objectResult.Value as Entity;
            entity.Id.Should().Be(1);
            entity.Name.Should().Be("Patched Entity");
        }

        /// <summary>
        /// Patch - Success: 204 NoContent
        /// </summary>
        [TestMethod]
        public async Task Patch_SuccessWithoutContent_Returns204NoContent()
        {
            var command = new PatchEntityWithoutContentCommand { Id = 1, ShouldSucceed = true, EntityExists = true };

            var result = await _restMediator.ProcessRestCommandAsync(RestOperation.Patch, command);

            result.Should().BeOfType<StatusCodeResult>();
            var statusResult = result as StatusCodeResult;
            statusResult.StatusCode.Should().Be((int)HttpStatusCode.NoContent);
        }

        /// <summary>
        /// Patch - Failure: 404 NotFound - The targeted entity identifier does not exist
        /// </summary>
        [TestMethod]
        public async Task Patch_EntityNotFound_Returns404NotFound()
        {
            var command = new PatchEntityCommand { Id = 999, Name = "Non-existent", ShouldSucceed = false, EntityExists = false };

            var result = await _restMediator.ProcessRestCommandAsync(RestOperation.PatchWithContent, command);

            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult.StatusCode.Should().Be((int)HttpStatusCode.NotFound);
        }

        /// <summary>
        /// Patch - Failure: 400 Bad Request - Return details about the failure
        /// </summary>
        [TestMethod]
        public async Task Patch_Failure_Returns400BadRequestWithDetails()
        {
            var command = new PatchEntityCommand { Id = 1, Name = "Invalid", ShouldSucceed = false, EntityExists = true };

            var result = await _restMediator.ProcessRestCommandAsync(RestOperation.PatchWithContent, command);

            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
            objectResult.Value.Should().BeOfType<CommandResponse<Entity>>();
            var response = objectResult.Value as CommandResponse<Entity>;
            response.Successful.Should().BeFalse();
            response.OutcomeEntries.Should().NotBeEmpty();
        }

        #endregion

        #region Delete Tests

        /// <summary>
        /// Delete - Success: 200 Ok - No content
        /// </summary>
        [TestMethod]
        public async Task Delete_Success_Returns200Ok()
        {
            var command = new DeleteEntityCommand { Id = 1, ShouldSucceed = true, EntityExists = true };

            var result = await _restMediator.ProcessRestCommandAsync(RestOperation.Delete, command);

            result.Should().BeOfType<StatusCodeResult>();
            var statusResult = result as StatusCodeResult;
            statusResult.StatusCode.Should().Be((int)HttpStatusCode.OK);
        }

        /// <summary>
        /// Delete - Failure: 404 NotFound - The targeted entity does not exist
        /// </summary>
        [TestMethod]
        public async Task Delete_EntityNotFound_Returns404NotFound()
        {
            var command = new DeleteEntityCommand { Id = 999, ShouldSucceed = false, EntityExists = false };

            var result = await _restMediator.ProcessRestCommandAsync(RestOperation.Delete, command);

            result.Should().BeOfType<StatusCodeResult>();
            var statusResult = result as StatusCodeResult;
            statusResult.StatusCode.Should().Be((int)HttpStatusCode.NotFound);
        }

        #endregion

        #region Get Tests

        /// <summary>
        /// Get - Success: 200 Ok - With the list of resulting entities matching the search criteria
        /// </summary>
        [TestMethod]
        public async Task GetMany_Success_Returns200OkWithList()
        {
            var query = new GetEntitiesQuery { ReturnEmpty = false };

            var result = await _restMediator.ProcessRestQueryAsync(RestOperation.GetMany, query);

            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult.StatusCode.Should().Be(200);
            objectResult.Value.Should().BeOfType<List<Entity>>();
            var entities = objectResult.Value as List<Entity>;
            entities.Should().NotBeEmpty();
        }

        /// <summary>
        /// Get - Success: 200 Ok - With an empty array
        /// </summary>
        [TestMethod]
        public async Task GetMany_EmptyResult_Returns200OkWithEmptyArray()
        {
            var query = new GetEntitiesQuery { ReturnEmpty = true };

            var result = await _restMediator.ProcessRestQueryAsync(RestOperation.GetMany, query);

            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult.StatusCode.Should().Be(200);
            objectResult.Value.Should().BeOfType<List<Entity>>();
            var entities = objectResult.Value as List<Entity>;
            entities.Should().BeEmpty();
        }

        #endregion

        #region Get Specific Tests

        /// <summary>
        /// Get specific - Success: 200 Ok - The entity matching the identifier specified is returned as content
        /// </summary>
        [TestMethod]
        public async Task GetSingle_Success_Returns200OkWithEntity()
        {
            var query = new GetEntityByIdQuery { Id = 1, EntityExists = true };

            var result = await _restMediator.ProcessRestQueryAsync(RestOperation.GetSingle, query);

            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult.StatusCode.Should().Be(200);
            objectResult.Value.Should().BeOfType<Entity>();
            var entity = objectResult.Value as Entity;
            entity.Id.Should().Be(1);
        }

        /// <summary>
        /// Get specific - Failure: 404 NotFound - No content
        /// </summary>
        [TestMethod]
        public async Task GetSingle_EntityNotFound_Returns404NotFound()
        {
            var query = new GetEntityByIdQuery { Id = 999, EntityExists = false };

            var result = await _restMediator.ProcessRestQueryAsync(RestOperation.GetSingle, query);

            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult.StatusCode.Should().Be((int)HttpStatusCode.NotFound);
        }

        #endregion

        #region Action Tests

        /// <summary>
        /// Action - Success: 200 Ok - Return content where appropriate (ActionWithContent)
        /// </summary>
        [TestMethod]
        public async Task Action_SuccessWithContent_Returns200OkWithFullResponse()
        {
            var command = new ExecuteActionWithContentCommand { Id = 1, ShouldSucceed = true };

            var result = await _restMediator.ProcessRestCommandAsync(RestOperation.ActionWithContent, command);

            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult.StatusCode.Should().Be((int)HttpStatusCode.OK);
            objectResult.Value.Should().BeOfType<CommandResponse<Entity>>();
            var response = objectResult.Value as CommandResponse<Entity>;
            response.Successful.Should().BeTrue();
            response.Result.Should().NotBeNull();
        }

        /// <summary>
        /// Action - Success: 200 Ok - Return result content (ActionWithResultContent)
        /// </summary>
        [TestMethod]
        public async Task Action_SuccessWithResultContent_Returns200OkWithResult()
        {
            var command = new ExecuteActionWithResultContentCommand { Value = 42, ShouldSucceed = true };

            var result = await _restMediator.ProcessRestCommandAsync(RestOperation.ActionWithResultContent, command);

            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult.StatusCode.Should().Be((int)HttpStatusCode.OK);
            objectResult.Value.Should().Be(42);
        }

        /// <summary>
        /// Action - Success: 200 Ok - No content
        /// </summary>
        [TestMethod]
        public async Task Action_SuccessWithoutContent_Returns200Ok()
        {
            var command = new ExecuteActionCommand { ShouldSucceed = true };

            var result = await _restMediator.ProcessRestCommandAsync(RestOperation.Action, command);

            result.Should().BeOfType<StatusCodeResult>();
            var statusResult = result as StatusCodeResult;
            statusResult.StatusCode.Should().Be((int)HttpStatusCode.OK);
        }

        /// <summary>
        /// Action - Failure: 400 Bad Request - Return details about the failure
        /// </summary>
        [TestMethod]
        public async Task Action_Failure_Returns400BadRequestWithDetails()
        {
            var command = new ExecuteActionWithContentCommand { Id = 1, ShouldSucceed = false };

            var result = await _restMediator.ProcessRestCommandAsync(RestOperation.ActionWithContent, command);

            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
            objectResult.Value.Should().BeOfType<CommandResponse<Entity>>();
            var response = objectResult.Value as CommandResponse<Entity>;
            response.Successful.Should().BeFalse();
            response.OutcomeEntries.Should().NotBeEmpty();
        }

        #endregion

        #region Generic Results Tests

        /// <summary>
        /// Generic results - Authorization error: 401 Unauthorized (Command)
        /// </summary>
        [TestMethod]
        public async Task Command_NotAuthorized_Returns401Unauthorized()
        {
            var command = new CreateEntityCommand { Id = 1, Name = "Test", ShouldSucceed = false, IsNotAuthorized = true };

            var result = await _restMediator.ProcessRestCommandAsync(RestOperation.CreateWithContent, command);

            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult.StatusCode.Should().Be((int)HttpStatusCode.Unauthorized);
            objectResult.Value.Should().BeOfType<CommandResponse<Entity>>();
            var response = objectResult.Value as CommandResponse<Entity>;
            response.Successful.Should().BeFalse();
            response.OutcomeEntries.Should().Contain(e => e.ErrorCode == GenericErrorCodes.NotAuthorized);
        }

        /// <summary>
        /// Generic results - Authentication error: 403 Forbidden (Command)
        /// </summary>
        [TestMethod]
        public async Task Command_NotAuthenticated_Returns403Forbidden()
        {
            var command = new CreateEntityCommand { Id = 1, Name = "Test", ShouldSucceed = false, IsNotAuthenticated = true };

            var result = await _restMediator.ProcessRestCommandAsync(RestOperation.CreateWithContent, command);

            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult.StatusCode.Should().Be((int)HttpStatusCode.Forbidden);
            objectResult.Value.Should().BeOfType<CommandResponse<Entity>>();
            var response = objectResult.Value as CommandResponse<Entity>;
            response.Successful.Should().BeFalse();
            response.OutcomeEntries.Should().Contain(e => e.ErrorCode == GenericErrorCodes.NotAuthenticated);
        }

        /// <summary>
        /// Generic results - Authorization error: 401 Unauthorized (Query)
        /// </summary>
        [TestMethod]
        public async Task Query_NotAuthorized_Returns401Unauthorized()
        {
            var query = new GetEntityByIdQuery { Id = 1, EntityExists = true, IsNotAuthorized = true };

            var result = await _restMediator.ProcessRestQueryAsync(RestOperation.GetSingle, query);

            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult.StatusCode.Should().Be((int)HttpStatusCode.Unauthorized);
            objectResult.Value.Should().BeOfType<QueryResponse<Entity>>();
            var response = objectResult.Value as QueryResponse<Entity>;
            response.Successful.Should().BeFalse();
            response.OutcomeEntries.Should().Contain(e => e.ErrorCode == GenericErrorCodes.NotAuthorized);
        }

        /// <summary>
        /// Generic results - Authentication error: 403 Forbidden (Query)
        /// </summary>
        [TestMethod]
        public async Task Query_NotAuthenticated_Returns403Forbidden()
        {
            var query = new GetEntityByIdQuery { Id = 1, EntityExists = true, IsNotAuthenticated = true };

            var result = await _restMediator.ProcessRestQueryAsync(RestOperation.GetSingle, query);

            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult.StatusCode.Should().Be((int)HttpStatusCode.Forbidden);
            objectResult.Value.Should().BeOfType<QueryResponse<Entity>>();
            var response = objectResult.Value as QueryResponse<Entity>;
            response.Successful.Should().BeFalse();
            response.OutcomeEntries.Should().Contain(e => e.ErrorCode == GenericErrorCodes.NotAuthenticated);
        }

        #endregion
    }
}
