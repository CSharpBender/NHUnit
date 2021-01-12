using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace NHUnit
{
    public interface IRepository<T> where T : class, new()
    {
        /// <summary>
        /// Query all the records
        /// </summary>
        /// <returns></returns>
        IQueryable<T> All();

        /// <summary>
        /// Get a proxy for an Id
        /// The DB is not hit until the value is requested
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        /// <example>
        /// <code>
        /// IRepository&lt;EntityType&gt; _repository;
        /// await _repository.Get(1).Include(e=&gt;e.Child1, e=&gt;e.Child2).Unproxy().ValueAsync(token);
        /// </code>
        /// </example>
        ISingleEntityWrapper<T> Get(object id);

        /// <summary>
        /// Insert entity
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task InsertAsync(T entity, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Insert entity
        /// </summary>
        /// <param name="entity"></param>
        void Insert(T entity);

        /// <summary>
        /// Insert entities
        /// </summary>
        /// <param name="entities"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task InsertManyAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Insert entities
        /// </summary>
        /// <param name="entities"></param>
        void InsertMany(IEnumerable<T> entities);

        /// <summary>
        /// Update entity
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task UpdateAsync(T entity, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Update entity
        /// </summary>
        /// <param name="entity"></param>
        void Update(T entity);

        /// <summary>
        /// Update entities
        /// </summary>
        /// <param name="entities"></param>
        void UpdateMany(IEnumerable<T> entities);

        /// <summary>
        /// Update entity
        /// </summary>
        /// <param name="entities"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task UpdateManyAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Update multiple entities based on condition
        /// </summary>
        /// <param name="filterCondition"></param>
        /// <param name="partialEntity"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task UpdateWhereAsync(Expression<Func<T, bool>> filterCondition, T partialEntity, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Update multiple entities based on condition
        /// </summary>
        /// <param name="filterCondition"></param>
        /// <param name="partialEntity"></param>
        void UpdateWhere(Expression<Func<T, bool>> filterCondition, T partialEntity);

        /// <summary>
        /// Save or update entity
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task InsertOrUpdateAsync(T entity, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Save or update entity
        /// </summary>
        /// <param name="entity"></param>
        void InsertOrUpdate(T entity);

        /// <summary>
        /// Save or update entities
        /// </summary>
        /// <param name="entities"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task InsertOrUpdateManyAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Save or update entities
        /// </summary>
        /// <param name="entities"></param>
        void InsertOrUpdateMany(IEnumerable<T> entities);

        /// <summary>
        /// Delete entity by Id
        /// </summary>
        /// <param name="id"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task DeleteAsync(object id, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Delete entity by Id
        /// </summary>
        /// <param name="id"></param>
        void Delete(object id);

        /// <summary>
        /// Delete entity by Id
        /// </summary>
        /// <param name="ids"></param>
        void DeleteMany(IEnumerable<object> ids);

        /// <summary>
        /// Delete entity by Id
        /// </summary>
        /// <param name="ids"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task DeleteManyAsync(IEnumerable<object> ids, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Delete multiple entities that meet condition
        /// </summary>
        /// <param name="filterCondition"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task DeleteWhereAsync(Expression<Func<T, bool>> filterCondition, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Delete multiple entities that meet condition
        /// </summary>
        /// <param name="filterCondition"></param>
        void DeleteWhere(Expression<Func<T, bool>> filterCondition);

    }
}
