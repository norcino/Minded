using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Minded.Extensions.Exception;
using Minded.Framework.CQRS.Abstractions;
using Minded.Framework.CQRS.Command;
using Minded.Framework.CQRS.Query;

namespace Minded.Extensions.WebApi.IntegrationTests
{
    #region Entity

    /// <summary>
    /// Simple entity class for testing REST operations.
    /// </summary>
    public class Entity
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    #endregion

    #region Create Commands and Handlers

    /// <summary>
    /// Command to create an entity with content returned.
    /// </summary>
    public class CreateEntityCommand : ICommand<Entity>
    {
        public Guid TraceId { get; set; } = Guid.NewGuid();
        public int Id { get; set; }
        public string Name { get; set; }
        public bool ShouldSucceed { get; set; }
        public bool IsNotAuthorized { get; set; }
        public bool IsNotAuthenticated { get; set; }
    }

    /// <summary>
    /// Handler for CreateEntityCommand.
    /// Simulates success/failure scenarios based on command properties.
    /// </summary>
    public class CreateEntityCommandHandler : ICommandHandler<CreateEntityCommand, Entity>
    {
        public Task<ICommandResponse<Entity>> HandleAsync(CreateEntityCommand command, CancellationToken cancellationToken = default)
        {
            if (command.IsNotAuthorized)
            {
                var response = new CommandResponse<Entity>(null) { Successful = false };
                var entry = new OutcomeEntry(string.Empty, "Not authorized") { ErrorCode = GenericErrorCodes.NotAuthorized };
                response.OutcomeEntries.Add(entry);
                return Task.FromResult<ICommandResponse<Entity>>(response);
            }

            if (command.IsNotAuthenticated)
            {
                var response = new CommandResponse<Entity>(null) { Successful = false };
                var entry = new OutcomeEntry(string.Empty, "Not authenticated") { ErrorCode = GenericErrorCodes.NotAuthenticated };
                response.OutcomeEntries.Add(entry);
                return Task.FromResult<ICommandResponse<Entity>>(response);
            }

            if (command.ShouldSucceed)
            {
                var entity = new Entity { Id = command.Id, Name = command.Name };
                return Task.FromResult<ICommandResponse<Entity>>(new CommandResponse<Entity>(entity) { Successful = true });
            }
            else
            {
                var response = new CommandResponse<Entity>(null) { Successful = false };
                var entry = new OutcomeEntry(string.Empty, "Validation failed") { ErrorCode = "VALIDATION_ERROR" };
                response.OutcomeEntries.Add(entry);
                return Task.FromResult<ICommandResponse<Entity>>(response);
            }
        }
    }

    /// <summary>
    /// Command to create an entity without content returned.
    /// </summary>
    public class CreateEntityWithoutContentCommand : ICommand
    {
        public Guid TraceId { get; set; } = Guid.NewGuid();
        public int Id { get; set; }
        public bool ShouldSucceed { get; set; }
    }

    /// <summary>
    /// Handler for CreateEntityWithoutContentCommand.
    /// </summary>
    public class CreateEntityWithoutContentCommandHandler : ICommandHandler<CreateEntityWithoutContentCommand>
    {
        public Task<ICommandResponse> HandleAsync(CreateEntityWithoutContentCommand command, CancellationToken cancellationToken = default)
        {
            if (command.ShouldSucceed)
            {
                return Task.FromResult<ICommandResponse>(new CommandResponse { Successful = true });
            }
            else
            {
                var response = new CommandResponse { Successful = false };
                var entry = new OutcomeEntry(string.Empty, "Validation failed") { ErrorCode = "VALIDATION_ERROR" };
                response.OutcomeEntries.Add(entry);
                return Task.FromResult<ICommandResponse>(response);
            }
        }
    }

    #endregion

    #region Update Commands and Handlers

    /// <summary>
    /// Command to update an entity with content returned.
    /// </summary>
    public class UpdateEntityCommand : ICommand<Entity>
    {
        public Guid TraceId { get; set; } = Guid.NewGuid();
        public int Id { get; set; }
        public string Name { get; set; }
        public bool ShouldSucceed { get; set; }
        public bool EntityExists { get; set; }
    }

    /// <summary>
    /// Handler for UpdateEntityCommand.
    /// </summary>
    public class UpdateEntityCommandHandler : ICommandHandler<UpdateEntityCommand, Entity>
    {
        public Task<ICommandResponse<Entity>> HandleAsync(UpdateEntityCommand command, CancellationToken cancellationToken = default)
        {
            if (!command.EntityExists)
            {
                var response = new CommandResponse<Entity>(null) { Successful = false };
                var entry = new OutcomeEntry(string.Empty, "Entity not found") { ErrorCode = GenericErrorCodes.SubjectNotFound };
                response.OutcomeEntries.Add(entry);
                return Task.FromResult<ICommandResponse<Entity>>(response);
            }

            if (command.ShouldSucceed)
            {
                var entity = new Entity { Id = command.Id, Name = command.Name };
                return Task.FromResult<ICommandResponse<Entity>>(new CommandResponse<Entity>(entity) { Successful = true });
            }
            else
            {
                var response = new CommandResponse<Entity>(null) { Successful = false };
                var entry = new OutcomeEntry(string.Empty, "Validation failed") { ErrorCode = "VALIDATION_ERROR" };
                response.OutcomeEntries.Add(entry);
                return Task.FromResult<ICommandResponse<Entity>>(response);
            }
        }
    }

    /// <summary>
    /// Command to update an entity without content returned.
    /// </summary>
    public class UpdateEntityWithoutContentCommand : ICommand
    {
        public Guid TraceId { get; set; } = Guid.NewGuid();
        public int Id { get; set; }
        public bool ShouldSucceed { get; set; }
        public bool EntityExists { get; set; }
    }

    /// <summary>
    /// Handler for UpdateEntityWithoutContentCommand.
    /// </summary>
    public class UpdateEntityWithoutContentCommandHandler : ICommandHandler<UpdateEntityWithoutContentCommand>
    {
        public Task<ICommandResponse> HandleAsync(UpdateEntityWithoutContentCommand command, CancellationToken cancellationToken = default)
        {
            if (!command.EntityExists)
            {
                var response = new CommandResponse { Successful = false };
                var entry = new OutcomeEntry(string.Empty, "Entity not found") { ErrorCode = GenericErrorCodes.SubjectNotFound };
                response.OutcomeEntries.Add(entry);
                return Task.FromResult<ICommandResponse>(response);
            }

            if (command.ShouldSucceed)
            {
                return Task.FromResult<ICommandResponse>(new CommandResponse { Successful = true });
            }
            else
            {
                var response = new CommandResponse { Successful = false };
                var entry = new OutcomeEntry(string.Empty, "Validation failed") { ErrorCode = "VALIDATION_ERROR" };
                response.OutcomeEntries.Add(entry);
                return Task.FromResult<ICommandResponse>(response);
            }
        }
    }

    #endregion

    #region Patch Commands and Handlers

    /// <summary>
    /// Command to patch an entity with content returned.
    /// </summary>
    public class PatchEntityCommand : ICommand<Entity>
    {
        public Guid TraceId { get; set; } = Guid.NewGuid();
        public int Id { get; set; }
        public string Name { get; set; }
        public bool ShouldSucceed { get; set; }
        public bool EntityExists { get; set; }
    }

    /// <summary>
    /// Handler for PatchEntityCommand.
    /// </summary>
    public class PatchEntityCommandHandler : ICommandHandler<PatchEntityCommand, Entity>
    {
        public Task<ICommandResponse<Entity>> HandleAsync(PatchEntityCommand command, CancellationToken cancellationToken = default)
        {
            if (!command.EntityExists)
            {
                var response = new CommandResponse<Entity>(null) { Successful = false };
                var entry = new OutcomeEntry(string.Empty, "Entity not found") { ErrorCode = GenericErrorCodes.SubjectNotFound };
                response.OutcomeEntries.Add(entry);
                return Task.FromResult<ICommandResponse<Entity>>(response);
            }

            if (command.ShouldSucceed)
            {
                var entity = new Entity { Id = command.Id, Name = command.Name };
                return Task.FromResult<ICommandResponse<Entity>>(new CommandResponse<Entity>(entity) { Successful = true });
            }
            else
            {
                var response = new CommandResponse<Entity>(null) { Successful = false };
                var entry = new OutcomeEntry(string.Empty, "Validation failed") { ErrorCode = "VALIDATION_ERROR" };
                response.OutcomeEntries.Add(entry);
                return Task.FromResult<ICommandResponse<Entity>>(response);
            }
        }
    }

    /// <summary>
    /// Command to patch an entity without content returned.
    /// </summary>
    public class PatchEntityWithoutContentCommand : ICommand
    {
        public Guid TraceId { get; set; } = Guid.NewGuid();
        public int Id { get; set; }
        public bool ShouldSucceed { get; set; }
        public bool EntityExists { get; set; }
    }

    /// <summary>
    /// Handler for PatchEntityWithoutContentCommand.
    /// </summary>
    public class PatchEntityWithoutContentCommandHandler : ICommandHandler<PatchEntityWithoutContentCommand>
    {
        public Task<ICommandResponse> HandleAsync(PatchEntityWithoutContentCommand command, CancellationToken cancellationToken = default)
        {
            if (!command.EntityExists)
            {
                var response = new CommandResponse { Successful = false };
                var entry = new OutcomeEntry(string.Empty, "Entity not found") { ErrorCode = GenericErrorCodes.SubjectNotFound };
                response.OutcomeEntries.Add(entry);
                return Task.FromResult<ICommandResponse>(response);
            }

            if (command.ShouldSucceed)
            {
                return Task.FromResult<ICommandResponse>(new CommandResponse { Successful = true });
            }
            else
            {
                var response = new CommandResponse { Successful = false };
                var entry = new OutcomeEntry(string.Empty, "Validation failed") { ErrorCode = "VALIDATION_ERROR" };
                response.OutcomeEntries.Add(entry);
                return Task.FromResult<ICommandResponse>(response);
            }
        }
    }

    #endregion

    #region Delete Commands and Handlers

    /// <summary>
    /// Command to delete an entity.
    /// </summary>
    public class DeleteEntityCommand : ICommand
    {
        public Guid TraceId { get; set; } = Guid.NewGuid();
        public int Id { get; set; }
        public bool ShouldSucceed { get; set; }
        public bool EntityExists { get; set; }
    }

    /// <summary>
    /// Handler for DeleteEntityCommand.
    /// </summary>
    public class DeleteEntityCommandHandler : ICommandHandler<DeleteEntityCommand>
    {
        public Task<ICommandResponse> HandleAsync(DeleteEntityCommand command, CancellationToken cancellationToken = default)
        {
            if (!command.EntityExists)
            {
                var response = new CommandResponse { Successful = false };
                var entry = new OutcomeEntry(string.Empty, "Entity not found") { ErrorCode = GenericErrorCodes.SubjectNotFound };
                response.OutcomeEntries.Add(entry);
                return Task.FromResult<ICommandResponse>(response);
            }

            if (command.ShouldSucceed)
            {
                return Task.FromResult<ICommandResponse>(new CommandResponse { Successful = true });
            }
            else
            {
                var response = new CommandResponse { Successful = false };
                var entry = new OutcomeEntry(string.Empty, "Delete failed") { ErrorCode = "DELETE_ERROR" };
                response.OutcomeEntries.Add(entry);
                return Task.FromResult<ICommandResponse>(response);
            }
        }
    }

    #endregion

    #region Action Commands and Handlers

    /// <summary>
    /// Command to execute an action without content returned.
    /// </summary>
    public class ExecuteActionCommand : ICommand
    {
        public Guid TraceId { get; set; } = Guid.NewGuid();
        public bool ShouldSucceed { get; set; }
    }

    /// <summary>
    /// Handler for ExecuteActionCommand.
    /// </summary>
    public class ExecuteActionCommandHandler : ICommandHandler<ExecuteActionCommand>
    {
        public Task<ICommandResponse> HandleAsync(ExecuteActionCommand command, CancellationToken cancellationToken = default)
        {
            if (command.ShouldSucceed)
            {
                return Task.FromResult<ICommandResponse>(new CommandResponse { Successful = true });
            }
            else
            {
                var response = new CommandResponse { Successful = false };
                var entry = new OutcomeEntry(string.Empty, "Action failed") { ErrorCode = "ACTION_ERROR" };
                response.OutcomeEntries.Add(entry);
                return Task.FromResult<ICommandResponse>(response);
            }
        }
    }

    /// <summary>
    /// Command to execute an action with full content returned.
    /// </summary>
    public class ExecuteActionWithContentCommand : ICommand<Entity>
    {
        public Guid TraceId { get; set; } = Guid.NewGuid();
        public int Id { get; set; }
        public bool ShouldSucceed { get; set; }
    }

    /// <summary>
    /// Handler for ExecuteActionWithContentCommand.
    /// </summary>
    public class ExecuteActionWithContentCommandHandler : ICommandHandler<ExecuteActionWithContentCommand, Entity>
    {
        public Task<ICommandResponse<Entity>> HandleAsync(ExecuteActionWithContentCommand command, CancellationToken cancellationToken = default)
        {
            if (command.ShouldSucceed)
            {
                var entity = new Entity { Id = command.Id, Name = "Action Result" };
                return Task.FromResult<ICommandResponse<Entity>>(new CommandResponse<Entity>(entity) { Successful = true });
            }
            else
            {
                var response = new CommandResponse<Entity>(null) { Successful = false };
                var entry = new OutcomeEntry(string.Empty, "Action failed") { ErrorCode = "ACTION_ERROR" };
                response.OutcomeEntries.Add(entry);
                return Task.FromResult<ICommandResponse<Entity>>(response);
            }
        }
    }

    /// <summary>
    /// Command to execute an action with result content returned.
    /// </summary>
    public class ExecuteActionWithResultContentCommand : ICommand<int>
    {
        public Guid TraceId { get; set; } = Guid.NewGuid();
        public int Value { get; set; }
        public bool ShouldSucceed { get; set; }
    }

    /// <summary>
    /// Handler for ExecuteActionWithResultContentCommand.
    /// </summary>
    public class ExecuteActionWithResultContentCommandHandler : ICommandHandler<ExecuteActionWithResultContentCommand, int>
    {
        public Task<ICommandResponse<int>> HandleAsync(ExecuteActionWithResultContentCommand command, CancellationToken cancellationToken = default)
        {
            if (command.ShouldSucceed)
            {
                return Task.FromResult<ICommandResponse<int>>(new CommandResponse<int>(command.Value) { Successful = true });
            }
            else
            {
                var response = new CommandResponse<int>(0) { Successful = false };
                var entry = new OutcomeEntry(string.Empty, "Action failed") { ErrorCode = "ACTION_ERROR" };
                response.OutcomeEntries.Add(entry);
                return Task.FromResult<ICommandResponse<int>>(response);
            }
        }
    }

    #endregion

    #region Queries and Handlers

    /// <summary>
    /// Query to get an entity by ID.
    /// For authorization/authentication tests, we use IQueryResponse<Entity> as the result type.
    /// For normal scenarios, we use Entity as the result type.
    /// </summary>
    public class GetEntityByIdQuery : IQuery<IQueryResponse<Entity>>
    {
        public Guid TraceId { get; set; } = Guid.NewGuid();
        public int Id { get; set; }
        public bool EntityExists { get; set; }
        public bool IsNotAuthorized { get; set; }
        public bool IsNotAuthenticated { get; set; }
    }

    /// <summary>
    /// Handler for GetEntityByIdQuery.
    /// Simulates success/failure/authorization scenarios based on query properties.
    /// Returns IQueryResponse<Entity> with appropriate success/failure status and error codes.
    /// </summary>
    public class GetEntityByIdQueryHandler : IQueryHandler<GetEntityByIdQuery, IQueryResponse<Entity>>
    {
        public Task<IQueryResponse<Entity>> HandleAsync(GetEntityByIdQuery query, CancellationToken cancellationToken = default)
        {
            if (query.IsNotAuthorized)
            {
                var response = new QueryResponse<Entity>(null) { Successful = false };
                var entry = new OutcomeEntry(string.Empty, "Not authorized") { ErrorCode = GenericErrorCodes.NotAuthorized };
                response.OutcomeEntries.Add(entry);
                return Task.FromResult<IQueryResponse<Entity>>(response);
            }

            if (query.IsNotAuthenticated)
            {
                var response = new QueryResponse<Entity>(null) { Successful = false };
                var entry = new OutcomeEntry(string.Empty, "Not authenticated") { ErrorCode = GenericErrorCodes.NotAuthenticated };
                response.OutcomeEntries.Add(entry);
                return Task.FromResult<IQueryResponse<Entity>>(response);
            }

            if (query.EntityExists)
            {
                var entity = new Entity { Id = query.Id, Name = $"Entity {query.Id}" };
                return Task.FromResult<IQueryResponse<Entity>>(new QueryResponse<Entity>(entity) { Successful = true });
            }
            else
            {
                // Return null for not found scenarios - this will trigger 404
                return Task.FromResult<IQueryResponse<Entity>>(null);
            }
        }
    }

    /// <summary>
    /// Query to get multiple entities.
    /// </summary>
    public class GetEntitiesQuery : IQuery<List<Entity>>
    {
        public Guid TraceId { get; set; } = Guid.NewGuid();
        public bool ReturnEmpty { get; set; }
    }

    /// <summary>
    /// Handler for GetEntitiesQuery.
    /// </summary>
    public class GetEntitiesQueryHandler : IQueryHandler<GetEntitiesQuery, List<Entity>>
    {
        public Task<List<Entity>> HandleAsync(GetEntitiesQuery query, CancellationToken cancellationToken = default)
        {
            if (query.ReturnEmpty)
            {
                return Task.FromResult(new List<Entity>());
            }
            else
            {
                var entities = new List<Entity>
                {
                    new Entity { Id = 1, Name = "Entity 1" },
                    new Entity { Id = 2, Name = "Entity 2" },
                    new Entity { Id = 3, Name = "Entity 3" }
                };
                return Task.FromResult(entities);
            }
        }
    }

    #endregion
}
