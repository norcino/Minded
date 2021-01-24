using System;
using System.Collections.Generic;

namespace Data.Common.Testing.Builder
{
    /// <summary>
    /// Generic persister allow to save test data in the database.
    /// This data can be used for integration testing and helper methods are available to customize the data creation specifying 
    /// custom property values and for multiple entities creation, you can specify how each created entity should differ.
    /// </summary>
    /// <typeparam name="TE"></typeparam>
    public interface IPersister<TE> where TE : class, new()
    {
        /// <summary>
        /// Using the generic builder will create a default Entity using anonymous data
        /// </summary>
        /// <returns>Persisted entity</returns>
        TE Persist();

        /// <summary>
        /// Persist the provided object
        /// </summary>
        /// <param name="entity">Entity to be saved</param>
        /// <returns>The saved entity</returns>
        TE Persist(TE entity);

        /// <summary>
        /// Allow the creation of multiple entities with on single method.
        /// Requires the number of entities to be created and an action which will be use to initialize
        /// every entity created for saving.
        /// </summary>
        /// <param name="numberOfEntities">Number of entities to be created</param>
        /// <param name="entitySetupAction">Action to be used to create each entity, this action generic type int is the index of the entity which will be created</param>
        /// <returns>List of created entities</returns>
        List<TE> Persist(int numberOfEntities, Action<TE, int> entitySetupAction = null);

        /// <summary>
        /// Persist a new entity using the parameter action to create it
        /// </summary>
        /// <param name="entitySetupAction">Action to be used to initialize the entity to be saved</param>
        /// <returns>Saved entity</returns>
        TE Persist(Action<TE> entitySetupAction);
    }
}
