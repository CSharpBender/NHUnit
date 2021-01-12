using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace NHUnit
{
    public interface ISingleEntityWrapper<T> where T : class, new()
    {
        /// <summary>
        /// A list of child nodes to be fetched from the database using joins, future queries or batch fetch.
        /// The framework determines the fastest fetch mode based on child relation and depth level.
        /// </summary>
        /// <param name="includeChildNodes"></param>
        /// <returns></returns>
        ISingleEntityWrapper<T> Include(params Expression<Func<T, object>>[] includeChildNodes);

        /// <summary>
        /// Instructs the framework to return the entity instance without any NHibernate proxies.
        /// This is just a configuration, the query is not executed until the Value is retrieved.
        /// </summary>
        /// <returns></returns>
        ISingleEntityWrapper<T> Unproxy();

        /// <summary>
        /// Notifies framework that multi queries should be used to fetch data, using a single server roundtrip.
        /// It must be called after Include and Unproxy
        /// This is just a configuration, the query is not executed until the Value is retrieved, which will execute the other deferred queries as well.
        /// </summary>
        /// <returns></returns>
        ISingleEntityWrapper<T> Deferred();

        /// <summary>
        /// Executes Query.
        /// If Deferred execution is used, the query will be evaluated only once for the whole batch.
        /// </summary>
        /// <returns></returns>
        T Value();

        /// <summary>
        /// Executes Query asynchronously.
        /// If Deferred execution is used, the query will be evaluated only once for the whole batch.
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<T> ValueAsync(CancellationToken token = default(CancellationToken));
    }
}
