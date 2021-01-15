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
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace NHUnit
{
    public class SingleEntityWrapper<T> : ISingleEntityWrapper<T> where T : class, new()
    {
        private EntityNodeInfo _childNodesInfo;
        private bool _unproxy;
        private bool _deferred;
        private ICriteria _mainCriteria;
        private IFutureValue<T> _mainFuture;
        private IEnumerable<IFutureEnumerable<T>> _childFutures;
        private readonly object _id;
        private readonly ISession _session;
        private readonly int _timeoutInSeconds;

        public SingleEntityWrapper(object id, ISession session, int commandTimeout)
        {
            _id = id;
            _session = session;
            _timeoutInSeconds = commandTimeout;
        }

        #region ISingleEntityWrapper
        public ISingleEntityWrapper<T> Include(params Expression<Func<T, object>>[] includeChildNodes)
        {
            CheckDeferred();
            _childNodesInfo = NHUnitHelper.GetExpressionTreeInfo<T>(includeChildNodes, _childNodesInfo ?? new EntityNodeInfo(), true);
            return this;
        }

        public ISingleEntityWrapper<T> Unproxy()
        {
            CheckDeferred();
            _unproxy = true;
            return this;
        }

        public ISingleEntityWrapper<T> Deferred()
        {
            CheckDeferred();
            _deferred = true;
            CreateCriteriaOrFutures();
            return this;
        }

        public T Value()
        {
            return GetValueSyncOrAsync(true)
                    .ConfigureAwait(false)
                    .GetAwaiter()
                    .GetResult();
        }

        public async Task<T> ValueAsync(CancellationToken token = default(CancellationToken))
        {
            return await GetValueSyncOrAsync(false, token);
        }
        #endregion

        private async Task<T> GetValueSyncOrAsync(bool sync, CancellationToken token = default(CancellationToken))
        {
            T result;
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
                    result = _mainFuture.Value;
                }
                else
                {
                    result = await _mainFuture.GetValueAsync(token);
                }
            }
            else
            {
                if (sync)
                {
                    result = _mainCriteria.UniqueResult<T>();
                }
                else
                {
                    result = await _mainCriteria.UniqueResultAsync<T>(token);
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
            _mainCriteria = _session.CreateCriteria<T>().Add(Restrictions.IdEq(_id)).SetTimeout(_timeoutInSeconds);
            if (_childNodesInfo != null)
            {
                bool rootHasList = false; //only one list can be joined with the main query
                var childCriterias = new List<ICriteria>();
                PopulateChildrenCriteria(_childNodesInfo, CriteriaTransformer.Clone(_mainCriteria), _mainCriteria, ref rootHasList, childCriterias);
                if (childCriterias.Count > 0 || _deferred)
                {
                    _mainFuture = _mainCriteria.FutureValue<T>();
                    _childFutures = childCriterias.Select(c => c.Future<T>()).ToList();
                }
            }
            else if (_deferred)
            {
                _mainFuture = _mainCriteria.FutureValue<T>();
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
