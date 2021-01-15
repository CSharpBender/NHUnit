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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NHUnit.Wrapper
{
    public class CountWrapper<T> : ICountWrapper<T>
    {
        private readonly IQueryable<T> _query;
        private bool _isDeferred;
        private readonly ISession _session;
        private IFutureValue<int> _mainFuture;

        public CountWrapper(IQueryable<T> query, ISession session)
        {
            _query = query;
            _session = session;
        }

        public ICountWrapper<T> Deferred()
        {
            _isDeferred = true;
            _mainFuture = _query.ToFutureValue(q => q.Count());
            return this;
        }

        public int Value()
        {
            return GetValueSyncOrAsync(true)
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();
        }

        public async Task<int> ValueAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return await GetValueSyncOrAsync(false, cancellationToken);
        }

        public async Task<int> GetValueSyncOrAsync(bool sync, CancellationToken token = default(CancellationToken))
        {
            int result;
            if (_isDeferred)
            {
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
                    result = await _query.CountAsync(token);
                }
                else
                {
                    result = _query.Count();
                }
            }

            return result;
        }
    }
}
