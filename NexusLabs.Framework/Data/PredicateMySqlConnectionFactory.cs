using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace NexusLabs.Framework.Data;

public sealed class PredicateMySqlConnectionFactory : IDbConnectionFactory
{
    private readonly Func<CancellationToken, Task<IAsyncDbConnection>> _createCallback;
    private readonly Func<CancellationToken, Task<IAsyncDbConnection>> _openCallback;

    public PredicateMySqlConnectionFactory(
        Func<CancellationToken, Task<IAsyncDbConnection>> createCallback,
        Func<CancellationToken, Task<IAsyncDbConnection>>? openCallback = null)
    {
        _createCallback = createCallback;
        _openCallback =
            openCallback
            ?? new Func<CancellationToken, Task<IAsyncDbConnection>>(async ct =>
            {
                var connection = await _createCallback
                    .Invoke(ct)
                    .ConfigureAwait(false);
                await connection
                    .OpenAsync(ct)
                    .ConfigureAwait(false);
                return connection;
            });
    }

    public string ConnectionString
    {
        get
        {
            // FIXME: this is an abomination
            using var connection = _createCallback.Invoke(CancellationToken.None).Result;
            return connection.ConnectionString;
        }
    }

    public async Task<IAsyncDbConnection> CreateNewConnectionAsync(
        CancellationToken cancellationToken = default)
        => await _createCallback.Invoke(cancellationToken).ConfigureAwait(false);

    public async Task<IAsyncDbConnection> OpenNewConnectionAsync(
        CancellationToken cancellationToken = default)
        => await _openCallback.Invoke(cancellationToken).ConfigureAwait(false);
}

