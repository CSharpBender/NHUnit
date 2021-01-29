using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NHUnit
{
    public interface IUnitOfWork : IDisposable
    {
        /// <summary>
        /// Starts a new transaction
        /// </summary>
        void BeginTransaction();

        /// <summary>
        /// Commit transaction changes
        /// </summary>
        void CommitTransaction();

        /// <summary>
        /// Commit transaction changes, asynchronously
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task CommitTransactionAsync(CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Revert transaction changes
        /// </summary>
        void RollbackTransaction();

        /// <summary>
        /// Revert transaction changes, asynchronously
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task RollbackTransactionAsync(CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Push changes to DB
        /// </summary>
        void SaveChanges();

        /// <summary>
        /// Push changes to DB, asynchronously
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task SaveChangesAsync(CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Completely clear the session. Evict all loaded instances and cancel all pending saves, updates and deletions.
        /// </summary>
        void ClearCache();

        /// <summary>
        /// Wrap an IQueryable in a IEntityListWrapper which offers features like multi query (Deferred)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="query"></param>
        /// <returns></returns>
        IEntityListWrapper<T> WrapQuery<T>(IQueryable<T> query);

        /// <summary>
        /// Get the entity instance without any NHibernate proxies.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <returns></returns>
        T Unproxy<T>(T obj);

        /// <summary>
        /// Asynchronous Query/Procedure execution which returns a list
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="queryString"></param>
        /// <param name="parameters"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<IList<T>> ExecuteListAsync<T>(string queryString, object parameters = null, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Asynchronous Query/Procedure execution which returns a list
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="queryString"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        IList<T> ExecuteList<T>(string queryString, object parameters = null);

        /// <summary>
        /// Execute asynchronously a query/stored procedure which returns a single value
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="queryString"></param>
        /// <param name="parameters"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<T> ExecuteScalarAsync<T>(string queryString, object parameters = null, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Execute query/stored procedure which returns a single value
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="queryString"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        T ExecuteScalar<T>(string queryString, object parameters = null);

        /// <summary>
        /// Asynchronous Query/Procedure execution which has no return value
        /// </summary>
        /// <param name="queryString"></param>
        /// <param name="parameters"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task ExecuteNonQueryAsync(string queryString, object parameters = null, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Asynchronous Query/Procedure execution which has no return value
        /// </summary>
        /// <param name="queryString"></param>
        /// <param name="parameters"></param>
        void ExecuteNonQuery(string queryString, object parameters = null);


        /// <summary>
        /// Asynchronous Queries/Procedure execution which return multiple result sets
        /// </summary>
        /// <param name="queryString"></param>
        /// <param name="parameters"></param>
        /// <param name="cancellationToken"></param>
        /// <param name="returnTypes"></param>
        /// <returns></returns>
        Task<List<IEnumerable<object>>> ExecuteMultipleQueriesAsync(string queryString, object parameters = null,
            CancellationToken cancellationToken = default(CancellationToken), params Type[] returnTypes);

        /// <summary>
        /// Execute Queries/Procedure which return multiple result sets
        /// </summary>
        /// <param name="queryString"></param>
        /// <param name="parameters"></param>
        /// <param name="returnTypes"></param>
        /// <returns></returns>
        List<IEnumerable<object>> ExecuteMultipleQueries(string queryString, object parameters = null,
            params Type[] returnTypes);

        /// <summary>
        /// Get or Set command timeout in seconds
        /// Defaults to 60 seconds.
        /// </summary>
        int CommandTimeout { get; set; }
    }
}
