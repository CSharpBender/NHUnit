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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace NHUnit
{
    public class EntityListWrapper<T> : IEntityListWrapper<T>
    {
        private EntityNodeInfo _childNodesInfo;
        private bool _unproxy;
        private bool _deferred;
        private IFutureEnumerable<T> _mainFuture;

        public IQueryable<T> Query { get; set; }
        public ISession Session { get; set; }

        public IEntityListWrapper<T> Include(params Expression<Func<T, object>>[] includeChildNodes)
        {
            _childNodesInfo = NhibernateHelper.GetExpressionTreeInfo<T>(includeChildNodes, _childNodesInfo ?? new EntityNodeInfo(), true);
            return this;
        }

        public IEntityListWrapper<T> Unproxy()
        {
            _unproxy = true;
            return this;
        }

        public IEntityListWrapper<T> Deferred()
        {
            _deferred = true;
            _mainFuture = Query.ToFuture();
            return this;
        }


        public async Task<T> GetValueSyncOrAsync(bool sync, CancellationToken token = default(CancellationToken))
        {
            var result = default(T);
            if (_deferred)
            {
                if (sync)
                {
                    result = _mainFuture.GetEnumerable().FirstOrDefault();
                }
                else
                {
                    var tmp = await _mainFuture.GetEnumerableAsync(token);
                    result = tmp.FirstOrDefault();
                }
            }
            else
            {
                if (sync)
                {
                    result = await Query.FirstOrDefaultAsync(token);
                }
                else
                {
                    result = Query.FirstOrDefault();
                }
            }

            if (result != null)
            {
                if (_childNodesInfo != null)
                {
                    NhibernateHelper.VisitNodes(result, Session, _childNodesInfo);
                }
                if (_unproxy)
                {
                    result = NhibernateHelper.Unproxy(result, Session);
                }
            }

            return result;
        }

        public T Value()
        {
            return GetValueSyncOrAsync(true)
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();
        }

        public async Task<T> ValueAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return await GetValueSyncOrAsync(false, cancellationToken);
        }

        public List<T> List()
        {
            return GetValuesSyncOrAsync(true)
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();
        }

        public async Task<List<T>> ListAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return await GetValuesSyncOrAsync(false, cancellationToken);
        }

        public async Task<List<T>> GetValuesSyncOrAsync(bool sync, CancellationToken token = default(CancellationToken))
        {
            List<T> result = null;
            if (_deferred)
            {
                if (sync)
                {
                    result = _mainFuture.GetEnumerable().ToList();
                }
                else
                {
                    var tmp = await _mainFuture.GetEnumerableAsync(token);
                    result = tmp.ToList();
                }
            }
            else
            {
                if (sync)
                {
                    result = Query.ToList();
                }
                else
                {
                    result = await Query.ToListAsync(token);
                }
            }

            if (result != null)
            {
                if (_childNodesInfo != null)
                {
                    NhibernateHelper.VisitNodes(result, Session, _childNodesInfo);
                }
                if (_unproxy)
                {
                    result = NhibernateHelper.Unproxy(result, Session);
                }
            }

            return result;
        }
    }
}
