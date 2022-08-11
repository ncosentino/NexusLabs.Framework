using System;
using System.Threading;
using System.Threading.Tasks;

namespace System.Data
{
    public interface IAsyncDbConnection : 
        IDbConnection
#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        ,IAsyncDisposable
#endif
    {
        IAsyncDbCommand CreateAsyncCommand();

        Task OpenAsync();

        Task OpenAsync(
            CancellationToken cancellationToken);
    }
}
