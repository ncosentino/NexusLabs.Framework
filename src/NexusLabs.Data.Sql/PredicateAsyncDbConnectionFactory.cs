using System;
using System.Threading;
using System.Threading.Tasks;

using NexusLabs.Framework.Data;

namespace NexusLabs.Data.Sql;

/// <summary>
/// An <see cref="IDbConnectionFactory"/> built from caller-supplied callbacks. Useful in tests
/// and adapter scenarios where you want to compose a factory without writing a new class.
/// </summary>
/// <remarks>
/// Unlike a naive implementation, this factory does NOT call the create callback to derive
/// the <see cref="ConnectionString"/> property. The connection string is captured at
/// construction time, eliminating the synchronous-blocking-on-async anti-pattern.
/// </remarks>
public sealed class PredicateAsyncDbConnectionFactory : IDbConnectionFactory
{
    private readonly Func<CancellationToken, Task<IAsyncDbConnection>> _createCallback;
    private readonly Func<CancellationToken, Task<IAsyncDbConnection>> _openCallback;

    /// <summary>
    /// Creates a factory that delegates to <paramref name="createCallback"/> for both create
    /// and open paths. The open path additionally awaits
    /// <see cref="IAsyncDbConnection.OpenAsync(CancellationToken)"/> on the returned connection.
    /// </summary>
    /// <param name="connectionString">The connection string this factory exposes via <see cref="ConnectionString"/>.</param>
    /// <param name="createCallback">Callback that produces a new (unopened) connection.</param>
    /// <param name="openCallback">
    /// Optional callback that produces a new opened connection. If null, defaults to
    /// <paramref name="createCallback"/> followed by <c>OpenAsync</c>.
    /// </param>
    public PredicateAsyncDbConnectionFactory(
        string connectionString,
        Func<CancellationToken, Task<IAsyncDbConnection>> createCallback,
        Func<CancellationToken, Task<IAsyncDbConnection>>? openCallback = null)
    {
        ArgumentNullException.ThrowIfNull(connectionString);
        ArgumentNullException.ThrowIfNull(createCallback);

        ConnectionString = connectionString;
        _createCallback = createCallback;
        _openCallback =
            openCallback
            ?? (async ct =>
            {
                var connection = await createCallback(ct).ConfigureAwait(false);
                try
                {
                    await connection.OpenAsync(ct).ConfigureAwait(false);
                }
                catch
                {
                    await connection.DisposeAsync().ConfigureAwait(false);
                    throw;
                }
                return connection;
            });
    }

    /// <inheritdoc />
    public string ConnectionString { get; }

    /// <inheritdoc />
    public Task<IAsyncDbConnection> CreateNewConnectionAsync(
        CancellationToken cancellationToken = default) =>
        _createCallback(cancellationToken);

    /// <inheritdoc />
    public Task<IAsyncDbConnection> OpenNewConnectionAsync(
        CancellationToken cancellationToken = default) =>
        _openCallback(cancellationToken);
}
