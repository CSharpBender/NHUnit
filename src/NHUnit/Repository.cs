//
//   CSharpBender - https://github.com/CSharpBender/NHUnit
//
// License: MIT
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS
// IN THE SOFTWARE.
//
using NHibernate;
using NHibernate.Engine;
using NHibernate.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace NHUnit
{
    public class Repository<T> : IRepository<T> where T : class, new()
    {
        private readonly UnitOfWork _unitOfWork;
        public Repository(IUnitOfWork unitOfWork)
        {
            //todo: clean this cast
            //reason: DI should inject the same instance for each request
            _unitOfWork = (UnitOfWork)unitOfWork;
        }

        protected ISession Session { get { return _unitOfWork.Session; } }

        public ISingleEntityWrapper<T> Get(object id)
        {
            return new SingleEntityWrapper<T>(id, Session, _unitOfWork.CommandTimeout);
        }

        public IMultipleEntityWrapper<T> GetMany(ICollection ids)
        {
            return new MultipleEntityWrapper<T>(ids, Session, _unitOfWork.CommandTimeout);
        }

        public IQueryable<T> All()
        {
            return Session.Query<T>();
        }

        public async Task InsertAsync(T entity, CancellationToken cancellationToken = default(CancellationToken))
        {
            await Session.SaveAsync(entity, cancellationToken);
        }

        public void Insert(T entity)
        {
            Session.Save(entity);
        }

        public async Task InsertManyAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default(CancellationToken))
        {
            foreach (var entity in entities)
            {
                await InsertAsync(entity, cancellationToken);
            }
        }

        public void InsertMany(IEnumerable<T> entities)
        {
            foreach (var entity in entities)
            {
                Insert(entity);
            }
        }

        public async Task UpdateAsync(T entity, CancellationToken cancellationToken = default(CancellationToken))
        {
            await UpdateSyncOrAsync(false, entity, cancellationToken);
        }

        public void Update(T entity)
        {
            UpdateSyncOrAsync(true, entity)
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();
        }

        public void UpdateMany(IEnumerable<T> entities)
        {
            foreach (var entity in entities)
            {
                Update(entity);
            }
        }

        public async Task UpdateManyAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default(CancellationToken))
        {
            foreach (var entity in entities)
            {
                await UpdateAsync(entity, cancellationToken);
            }
        }

        protected async Task UpdateSyncOrAsync(bool sync, T entity, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (!Session.Contains(entity))
            {
                var metadata = Session.SessionFactory.GetClassMetadata(typeof(T));
                var id = metadata.GetIdentifier(entity);
                if (id != null && id.ToString() != "0")
                {
                    var sessionImplementation = Session.GetSessionImplementation();
                    var entityPersister = sessionImplementation.Factory.TryGetEntityPersister(typeof(T).FullName);
                    if (sessionImplementation.PersistenceContext.ContainsEntity(new EntityKey(id, entityPersister)))
                    {
                        T mergedEntity;
                        if (sync)
                        {
                            mergedEntity = Session.Merge(entity);
                        }
                        else
                        {
                            mergedEntity = await Session.MergeAsync(entity, cancellationToken);
                        }

                        MoveProperties(entity, mergedEntity); //copy registered children
                        if (sync)
                        {
                            Session.Evict(mergedEntity);
                        }
                        else
                        {
                            await Session.EvictAsync(mergedEntity, cancellationToken);
                        }
                    }
                }
            }

            if (sync)
            {
                Session.Update(entity);
            }
            else
            {
                await Session.UpdateAsync(entity, cancellationToken);
            }
        }

        public async Task<int> UpdateWhereAsync(Expression<Func<T, bool>> filterCondition, Expression<Func<T, object>> partialExpression, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await Session.Query<T>().Where(filterCondition).UpdateAsync(partialExpression, cancellationToken);
        }

        public void UpdateWhere(Expression<Func<T, bool>> filterCondition, Expression<Func<T, object>> partialExpression)
        {
            Session.Query<T>().Where(filterCondition).Update(partialExpression);
        }

        public async Task InsertOrUpdateAsync(T entity, CancellationToken cancellationToken = default(CancellationToken))
        {
            await InsertOrUpdateSyncOrAsync(false, entity, cancellationToken);
        }

        public void InsertOrUpdate(T entity)
        {
            InsertOrUpdateSyncOrAsync(true, entity)
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();
        }

        public async Task InsertOrUpdateManyAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default(CancellationToken))
        {
            foreach (var entity in entities)
            {
                await InsertOrUpdateAsync(entity, cancellationToken);
            }
        }

        public void InsertOrUpdateMany(IEnumerable<T> entities)
        {
            foreach (var entity in entities)
            {
                InsertOrUpdate(entity);
            }
        }

        protected async Task InsertOrUpdateSyncOrAsync(bool sync, T entity, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (!Session.Contains(entity))
            {
                var metadata = Session.SessionFactory.GetClassMetadata(typeof(T));
                var id = metadata.GetIdentifier(entity);
                if (id != null && id.ToString() != "0")
                {
                    var sessionImplementation = Session.GetSessionImplementation();
                    var entityPersister = sessionImplementation.Factory.TryGetEntityPersister(typeof(T).FullName);
                    if (sessionImplementation.PersistenceContext.ContainsEntity(new EntityKey(id, entityPersister)))
                    {
                        var mergedEntity = await Session.MergeAsync(entity, cancellationToken);
                        MoveProperties(entity, mergedEntity); //copy registered children
                        if (sync)
                        {
                            Session.Evict(mergedEntity);
                        }
                        else
                        {
                            await Session.EvictAsync(mergedEntity, cancellationToken);
                        }
                        if (sync)
                        {
                            Session.Update(entity);
                        }
                        else
                        {
                            await Session.UpdateAsync(entity, cancellationToken);
                        }
                    }
                    else
                    {
                        if (sync)
                        {
                            Session.SaveOrUpdate(entity);
                        }
                        else
                        {
                            await Session.SaveOrUpdateAsync(entity, cancellationToken);
                        }
                    }
                }
                else
                {
                    if (sync)
                    {
                        Session.Save(entity);
                    }
                    else
                    {
                        await Session.SaveAsync(entity, cancellationToken);
                    }
                }
            }
            else
            {
                if (sync)
                {
                    Session.SaveOrUpdate(entity);
                }
                else
                {
                    await Session.SaveOrUpdateAsync(entity, cancellationToken);
                }
            }
        }

        public async Task DeleteAsync(object id, CancellationToken cancellationToken = default(CancellationToken))
        {
            var obj = await Session.LoadAsync<T>(id, cancellationToken);
            await Session.DeleteAsync(obj, cancellationToken);
        }

        public void Delete(object id)
        {
            Session.Delete(Session.Load<T>(id));
        }

        public void DeleteMany(IEnumerable<object> ids)
        {
            foreach (var id in ids)
            {
                Delete(id);
            }
        }

        public async Task DeleteManyAsync(IEnumerable<object> ids, CancellationToken cancellationToken = default(CancellationToken))
        {
            foreach (var id in ids)
            {
                await DeleteAsync(id, cancellationToken);
            }
        }

        public async Task<int> DeleteWhereAsync(Expression<Func<T, bool>> filterCondition, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await Session.Query<T>().Where(filterCondition).DeleteAsync(cancellationToken);
        }

        public void DeleteWhere(Expression<Func<T, bool>> filterCondition)
        {
            Session.Query<T>().Where(filterCondition).Delete();
        }

        #region Helper
        private void MoveProperties(T destinationObj, T sourceObj)
        {
            var properties = typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.GetProperty | BindingFlags.SetProperty);
            foreach (var property in properties)
            {
                property.SetValue(destinationObj, property.GetValue(sourceObj));
                //remove source property
                if (!property.PropertyType.IsValueType)
                {
                    property.SetValue(sourceObj, null);
                }
            }
        }
        #endregion
    }
}