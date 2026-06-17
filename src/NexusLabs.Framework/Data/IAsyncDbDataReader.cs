using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace NexusLabs.Framework.Data;

public interface IAsyncDbDataReader : 
    IDataReader
#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER || NETCOREAPP3_0_OR_GREATER
    ,IAsyncDisposable
#endif
{
    Task<bool> IsDBNullAsync(
        int ordinal);

    Task<bool> IsDBNullAsync(
        int ordinal,
        CancellationToken cancellationToken);

    Task CloseAsync();

    Task CloseAsync(CancellationToken cancellationToken);

    Task<bool> ReadAsync(
        CancellationToken cancellationToken);
}
