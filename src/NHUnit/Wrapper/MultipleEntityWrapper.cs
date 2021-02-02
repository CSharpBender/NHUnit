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
using NHibernate.Criterion;
using NHibernate.SqlCommand;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace NHUnit
{
    public class MultipleEntityWrapper<T> : IMultipleEntityWrapper<T> where T : class, new()
    {
        private EntityNodeInfo _childNodesInfo;
        private bool _unproxy;
        private bool _deferred;
        private ICriteria _mainCriteria;
        private IFutureEnumerable<T> _mainFuture;
        private IEnumerable<IFutureEnumerable<T>> _childFutures;
        private readonly ICollection _ids;
        private readonly ISession _session;
        private readonly int _timeoutInSeconds;

        public MultipleEntityWrapper(ICollection ids, ISession session, int commandTimeout)
        {
            _ids = ids;
            _session = session;
            _timeoutInSeconds = commandTimeout;
        }

        #region IMultipleEntityWrapper
        public IMultipleEntityWrapper<T> Include(params Expression<Func<T, object>>[] includeChildNodes)
        {
            CheckDeferred();
            _childNodesInfo = NHUnitHelper.GetExpressionTreeInfo<T>(includeChildNodes, _childNodesInfo ?? new EntityNodeInfo(), true);
            return this;
        }

        public IMultipleEntityWrapper<T> Unproxy()
        {
            CheckDeferred();
            _unproxy = true;
            return this;
        }

        public IMultipleEntityWrapper<T> Deferred()
        {
            CheckDeferred();
            _deferred = true;
            CreateCriteriaOrFutures();
            return this;
        }

        public List<T> List()
        {
            return GetListSyncOrAsync(true)
                    .ConfigureAwait(false)
                    .GetAwaiter()
                    .GetResult();
        }

        public async Task<List<T>> ListAsync(CancellationToken token = default(CancellationToken))
        {
            return await GetListSyncOrAsync(false, token);
        }
        #endregion

        private async Task<List<T>> GetListSyncOrAsync(bool sync, CancellationToken token = default(CancellationToken))
        {
            List<T> result;
            if (!_deferred)
            {
                CreateCriteriaOrFutures();
            }

            if (_mainFuture != null)
            {
                if (_childFutures != null)
                {
                    foreach (var cq in _childFutures) //we need to force execution: oracle client doesn't support multiple queries (for now)
                    {
                        if (sync)
                        {
                            cq.GetEnumerable();
                        }
                        else
                        {
                            await cq.GetEnumerableAsync(token);
                        }
                    }
                }
                if (sync)
                {
                    result = _mainFuture.GetEnumerable() as List<T>;
                }
                else
                {
                    result = await _mainFuture.GetEnumerableAsync(token) as List<T>;
                }
            }
            else
            {
                if (sync)
                {
                    result = _mainCriteria.List<T>() as List<T>;
                }
                else
                {
                    result = await _mainCriteria.ListAsync<T>(token) as List<T>; ;
                }
            }

            if (_childNodesInfo != null)
            {
                NHUnitHelper.VisitNodes(result, _session, _childNodesInfo);
            }

            if (_unproxy)
            {
                return NHUnitHelper.Unproxy(result, _session);
            }

            return result;
        }

        private void CreateCriteriaOrFutures()
        {
            _mainCriteria = _session.CreateCriteria<T>().Add(Restrictions.In(_session.SessionFactory.GetClassMetadata(typeof(T)).IdentifierPropertyName, _ids)).SetTimeout(_timeoutInSeconds);
            if (_childNodesInfo != null)
            {
                bool rootHasList = false; //only one list can be joined with the main query
                var childCriterias = new List<ICriteria>();
                PopulateChildrenCriteria(_childNodesInfo, CriteriaTransformer.Clone(_mainCriteria), _mainCriteria, ref rootHasList, childCriterias);
                if (childCriterias.Count > 0 || _deferred)
                {
                    _mainFuture = _mainCriteria.Future<T>();
                    _childFutures = childCriterias.Select(c => c.Future<T>()).ToList();
                }
            }
            else if (_deferred)
            {
                _mainFuture = _mainCriteria.Future<T>();
            }
        }

        private void CheckDeferred()
        {
            if (_deferred)
            {
                throw new Exception("After deferring the execution you can't do other configurations.");
            }
        }

        private void PopulateChildrenCriteria(EntityNodeInfo nodeInfo, ICriteria rootCriteria, ICriteria parentCriteria, ref bool rootHasList, List<ICriteria> childCriterias)
        {
            if (nodeInfo == null)
            {
                return;
            }

            if (nodeInfo.Level > 0) //it's not the root node
            {
                if (nodeInfo.IsList)
                {
                    if (rootHasList)
                    {
                        if (nodeInfo.Level == 1) //create new criteria
                        {
                            parentCriteria = rootCriteria.CreateCriteria(nodeInfo.PathName, JoinType.LeftOuterJoin);
                            childCriterias.Add(parentCriteria);
                        }
                        else //batch fetch should be faster
                        {
                            return;
                        }
                    }
                    else
                    {
                        parentCriteria.Fetch(SelectMode.Fetch, nodeInfo.PathName);
                        rootHasList = true;
                    }
                }
                else
                {
                    parentCriteria.Fetch(SelectMode.Fetch, nodeInfo.PathName);
                }
            }

            foreach (var childInfo in nodeInfo.Children)
            {
                PopulateChildrenCriteria(childInfo, rootCriteria, parentCriteria, ref rootHasList, childCriterias);
            }
        }
    }
}
