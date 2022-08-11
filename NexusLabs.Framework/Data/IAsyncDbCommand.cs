using System;
using System.Threading;
using System.Threading.Tasks;

namespace System.Data
{
    public interface IAsyncDbCommand :
        IDbCommand
#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        ,IAsyncDisposable
#endif
    {
        Task<int> ExecuteNonQueryAsync();

        Task<int> ExecuteNonQueryAsync(
            CancellationToken cancellationToken);

        Task<IAsyncDbDataReader> ExecuteReaderAsync();

        Task<IAsyncDbDataReader> ExecuteReaderAsync(
            CommandBehavior commandBehavior);

        Task<IAsyncDbDataReader> ExecuteReaderAsync(
            CommandBehavior commandBehavior,
            CancellationToken cancellationToken);

        Task<IAsyncDbDataReader> ExecuteReaderAsync(
            CancellationToken cancellationToken);

        Task<object> ExecuteScalarAsync();

        Task<object> ExecuteScalarAsync(
            CancellationToken cancellationToken);
    }
}
