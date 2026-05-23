using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace NexusLabs.Framework.Data;

public interface IAsyncDbConnection :
    IDbConnection,
    IAsyncDisposable
{
    IAsyncDbCommand CreateAsyncCommand();

    Task OpenAsync();

    Task OpenAsync(
        CancellationToken cancellationToken);
}
