using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace NHUnit
{
    public interface IEntityListWrapper<T>
    {
        /// <summary>
        /// A list of child nodes to be fetched from the database using batch fetch.
        /// This basically visits the child nodes triggering the batch fetch on lazy properties.
        /// </summary>
        /// <param name="includeChildNodes"></param>
        /// <returns></returns>
        IEntityListWrapper<T> Include(params Expression<Func<T, object>>[] includeChildNodes);

        /// <summary>
        /// Instructs the framework to return the entity instance without any NHibernate proxies.
        /// This is just a configuration, the query is not executed until the Value is retrieved.
        /// </summary>
        /// <returns></returns>
        IEntityListWrapper<T> Unproxy();

        /// <summary>
        /// Instruct the framework to compute the number of results
        /// This is just a configuration, the query is not executed until the Value is retrieved, which will execute the other deferred queries as well.
        /// </summary>
        /// <returns></returns>
        ICountWrapper<T> Count();

        /// <summary>
        /// Notifies framework that multi queries should be used to fetch data, using a single server roundtrip.
        /// This is just a configuration, the query is not executed until the Value is retrieved, which will execute the other deferred queries as well.
        /// </summary>
        /// <returns></returns>
        IEntityListWrapper<T> Deferred();

        /// <summary>
        /// Executes Query and returns the first value.
        /// The query should use Single() to avoid loading the whole collection in memory.
        /// If Deferred execution was used, the query will be evaluated only once for the whole batch.
        /// </summary>
        /// <returns></returns>
        T Value();

        /// <summary>
        /// Executes Query asynchronously and returns the first value.
        /// The query should use Single() to avoid loading the whole collection in memory.
        /// If Deferred execution was used, the query will be evaluated only once for the whole batch.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<T> ValueAsync(CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Executes Query.
        /// If Deferred execution was used, the query will be evaluated only once for the whole batch.
        /// </summary>
        /// <returns></returns>
        List<T> List();

        /// <summary>
        /// Executes Query asynchronously.
        /// If Deferred execution was used, the query will be evaluated only once for the whole batch.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<List<T>> ListAsync(CancellationToken cancellationToken = default(CancellationToken));
    }
}
