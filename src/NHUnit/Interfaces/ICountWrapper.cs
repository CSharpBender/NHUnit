using System.Threading;
using System.Threading.Tasks;

namespace NHUnit
{
    public interface ICountWrapper<T>
    {
        /// <summary>
        /// Notifies framework that multi queries should be used to fetch data, using a single server roundtrip.
        /// This is just a configuration, the query is not executed until the Value is retrieved, which will execute the other deferred queries as well.
        /// </summary>
        /// <returns></returns>
        ICountWrapper<T> Deferred();

        /// <summary>
        /// Executes Query and returns the number of rows
        /// If Deferred execution was used, the query will be evaluated only once for the whole batch.
        /// </summary>
        /// <returns></returns>
        int Value();

        /// <summary>
        /// Executes Query asynchronously and returns the number of rows
        /// If Deferred execution was used, the query will be evaluated only once for the whole batch.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<int> ValueAsync(CancellationToken cancellationToken = default(CancellationToken));
    }
}
