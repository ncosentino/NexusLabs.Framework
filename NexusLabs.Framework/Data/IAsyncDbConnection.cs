using System;
using System.Threading;
using System.Threading.Tasks;

namespace System.Data;

public interface IAsyncDbConnection : 
    IDbConnection,
    IAsyncDisposable
{
    IAsyncDbCommand CreateAsyncCommand();

    Task OpenAsync();

    Task OpenAsync(
        CancellationToken cancellationToken);
}
