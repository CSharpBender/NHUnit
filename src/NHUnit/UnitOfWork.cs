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
using NHibernate.Linq;
using NHibernate.Transform;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace NHUnit
{
    public class UnitOfWork : IUnitOfWork
    {
        private ITransaction _transaction;
        private readonly Lazy<ISession> _lazySession;

        public UnitOfWork(ISessionFactory sessionFactory)
        {
            CommandTimeout = 60;
            _lazySession = new Lazy<ISession>(() =>
            {
                var session = sessionFactory.OpenSession();
                session.FlushMode = FlushMode.Commit;
                return session;
            });
        }

        public UnitOfWork(ISessionFactory sessionFactory, bool initializeRepositories) : this(sessionFactory)
        {
            if (initializeRepositories)
            {
                var properties = GetType()
                    .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.SetProperty)
                    .Where(p => p.PropertyType.IsGenericType &&
                                p.PropertyType.GetGenericTypeDefinition() == typeof(IRepository<>));
                foreach (var property in properties)
                {
                    var genericArgument = property.PropertyType.GetGenericArguments()[0];
                    var repositoryType = typeof(Repository<>).MakeGenericType(genericArgument);
                    var repository = Activator.CreateInstance(repositoryType, this);
                    property.SetValue(this, repository);
                }
            }
        }

        public int CommandTimeout { get; set; }

        public ISession Session
        {
            get { return _lazySession.Value; }
        }

        public bool IsSessionCreated
        {
            get { return _lazySession.IsValueCreated; }
        }

        public void BeginTransaction()
        {
            _transaction = Session.BeginTransaction();
        }

        public void RollbackTransaction()
        {
            if (_transaction != null && _transaction.IsActive)
            {
                _transaction.Rollback();
            }
            else
            {
                throw new Exception("No transaction");
            }
        }

        public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            if (_transaction != null && _transaction.IsActive)
            {
                await _transaction.RollbackAsync(cancellationToken);
            }
            else
            {
                throw new Exception("No transaction");
            }
        }

        public void ClearCache()
        {
            Session.Clear();
        }

        public IEntityListWrapper<T> WrapQuery<T>(IQueryable<T> query)
        {
            return new EntityListWrapper<T>(query.WithOptions(o => o.SetTimeout(CommandTimeout)), Session);
        }

        public void SaveChanges()
        {
            Session.Flush();
        }

        public async Task SaveChangesAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            await Session.FlushAsync(cancellationToken);
        }

        public void CommitTransaction()
        {
            // commit transaction if there is one active
            if (_transaction != null && _transaction.IsActive)
            {
                _transaction.Commit();
            }
            else
            {
                throw new Exception("No transaction");
            }
        }

        public async Task CommitTransactionAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            // commit transaction if there is one active
            if (_transaction != null && _transaction.IsActive)
            {
                await _transaction.CommitAsync(cancellationToken);
            }
            else
            {
                throw new Exception("No transaction");
            }
        }

        public T Unproxy<T>(T obj)
        {
            return NHUnitHelper.Unproxy(obj, Session);
        }

        public async Task<IList<T>> ExecuteListAsync<T>(string queryString, object parameters = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await CreateQueryWithParameters(queryString, parameters)
                            .SetResultTransformer(Transformers.AliasToBean(typeof(T)))
                            .ListAsync<T>(cancellationToken);
        }

        public IList<T> ExecuteList<T>(string queryString, object parameters = null)
        {
            return CreateQueryWithParameters(queryString, parameters)
                    .SetResultTransformer(Transformers.AliasToBean(typeof(T)))
                    .List<T>();
        }

        public async Task<T> ExecuteScalarAsync<T>(string queryString, object parameters = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await CreateQueryWithParameters(queryString, parameters)
                            .SetResultTransformer(Transformers.AliasToBean(typeof(T)))
                            .UniqueResultAsync<T>(cancellationToken);
        }

        public T ExecuteScalar<T>(string queryString, object parameters = null)
        {
            return CreateQueryWithParameters(queryString, parameters)
                    .SetResultTransformer(Transformers.AliasToBean(typeof(T)))
                    .UniqueResult<T>();
        }

        public async Task ExecuteNonQueryAsync(string queryString, object parameters = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            await CreateQueryWithParameters(queryString, parameters).UniqueResultAsync(cancellationToken);
        }

        public void ExecuteNonQuery(string queryString, object parameters = null)
        {
            CreateQueryWithParameters(queryString, parameters).UniqueResult();
        }

        protected IQuery CreateQueryWithParameters(string queryString, object parameters = null)
        {
            var query = Session.CreateSQLQuery(queryString);
            if (parameters != null)
            {
                var properties = parameters.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.GetProperty);
                if (!properties.Any())
                {
                    throw new ArgumentException("Object with public properties expected for query creation", nameof(parameters));
                }
                foreach (var property in properties)
                {
                    var name = property.Name;
                    dynamic value = property.GetValue(parameters);
                    query.SetParameter(name, value);
                }
            }
            return query.SetTimeout(CommandTimeout);
        }

        public void Dispose()
        {
            try
            {
                if (_transaction != null && _transaction.IsActive)
                {
                    _transaction.Rollback();
                }
            }
            finally
            {
                if (IsSessionCreated)
                {
                    Session.Dispose();
                }
            }
        }
    }
}
