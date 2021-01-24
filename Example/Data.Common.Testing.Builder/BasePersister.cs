
using System;
using Data.Context;

namespace Data.Common.Testing.Builder
{
//    public abstract class BasePersister<TE> : IDisposable where TE : class, new()
//    {
//        protected IHouseKeeperContext Context;
//        protected bool doNotDisposeContext = false;
//        protected readonly string ConnectionString;
//
//        protected BasePersister(string connectionString)
//        {
//            ConnectionString = connectionString;
//            if (Context == null)
//            {
//                Context = new CommonContext(connectionString);
//            }
//        }
//
//        protected BasePersister() : this(Constants.LIVEV5_EMPTY_CONNECTION_STRING_KEY)
//        {
//            DomainEntityAutoMapperConfiguration.Initialise(null);
//        }
//
//        protected BasePersister(ICommonContext context) : this(Constants.LIVEV5_EMPTY_CONNECTION_STRING_KEY)
//        {
//            DomainEntityAutoMapperConfiguration.Initialise(null);
//            Context = context;
//            doNotDisposeContext = true;
//        }
//
//        public abstract int Count();
//
//        public abstract TE Persist(TE entity);
//
//        /*
//         * This method is used to persist all child entyties but not the parent entity
//         * this is used from the generic tests
//         */
//        public abstract TE PersistChildEntities(TE entity);
//
//        protected bool Changed(object first, object second, params string[] exclusions)
//        {
//            foreach (var propertyInfo in first.GetType().GetProperties())
//            {
//                if (propertyInfo.PropertyType.IsValueType || propertyInfo.PropertyType == typeof(string))
//                {
//                    var firstValue = first.GetType().GetProperty(propertyInfo.Name).GetValue(first, null);
//                    var secondValue = second.GetType().GetProperty(propertyInfo.Name).GetValue(second, null);
//
//                    if (firstValue is DateTime)
//                    {
//                        TimeSpan difference = (DateTime)firstValue - (DateTime)secondValue;
//                        if (difference < TimeSpan.FromSeconds(1))
//                        {
//                            continue;
//                        }
//                    }
//
//                    return true;
//                }
//            }
//            return false;
//        }

//        public void Dispose()
//        {
//            if (!doNotDisposeContext && Context != null)
//            {
//                try
//                {
//                    Context.Dispose();
//                }
//                catch (InvalidOperationException)
//                {
//                }
//
//                Context = null;
//            }
//        }
    }
//}