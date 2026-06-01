using System;
using System.Threading;
using System.Threading.Tasks;

using MySql.Data.MySqlClient;

using NexusLabs.Framework.Data;

namespace NexusLabs.Data.Sql.MySql;

/// <summary>
/// Connection factory that produces <see cref="IAsyncDbConnection"/> instances backed by
/// <see cref="MySqlConnection"/>. Builds the connection string via
/// <see cref="MySqlConnectionStringBuilder"/>, so password values containing reserved characters
/// such as <c>;</c> or <c>'</c> are safely escaped.
/// </summary>
/// <remarks>
/// This factory does NOT compose lease, tracking, or logging decorators by default. Callers who
/// want pool-cap, debug tracking, or command logging should wrap the returned connection or
/// produced commands using the <see cref="AsyncDbDecoratorExtensions"/> fluent helpers.
/// </remarks>
public sealed class MySqlConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;
    private readonly IMySqlConnectionConfiguration _config;

    /// <summary>Default connection lifetime in seconds.</summary>
    /// <remarks>
    /// Per the MySql.Data documentation, <c>ConnectionLifeTime</c> controls how long a pooled
    /// connection can live before it is destroyed when returned to the pool. 5 minutes is the
    /// historical default carried forward from the BrandGhost implementation.
    /// </remarks>
    public const uint DefaultConnectionLifetimeSeconds = 300;

    /// <summary>Creates a new factory bound to <paramref name="config"/>.</summary>
    /// <exception cref="ArgumentNullException">If <paramref name="config"/> is null.</exception>
    /// <exception cref="ArgumentException">If required fields on <paramref name="config"/> are missing.</exception>
    /// <exception cref="ArgumentOutOfRangeException">If <see cref="IMySqlConnectionConfiguration.Port"/> is not positive.</exception>
    public MySqlConnectionFactory(IMySqlConnectionConfiguration config)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentException.ThrowIfNullOrWhiteSpace(config.Server, $"{nameof(config)}.{nameof(config.Server)}");
        ArgumentException.ThrowIfNullOrWhiteSpace(config.Username, $"{nameof(config)}.{nameof(config.Username)}");
        ArgumentException.ThrowIfNullOrEmpty(config.Password, $"{nameof(config)}.{nameof(config.Password)}");
        if (config.Port <= 0)
        {
            throw new ArgumentOutOfRangeException(
                $"{nameof(config)}.{nameof(config.Port)}",
                config.Port,
                "Port must be a positive integer.");
        }

        _config = config;

        var builder = new MySqlConnectionStringBuilder
        {
            Server = config.Server,
            Port = (uint)config.Port,
            UserID = config.Username,
            Password = config.Password,
            AllowUserVariables = true,
            Pooling = true,
            MinimumPoolSize = (uint)config.MinimumPoolSize,
            MaximumPoolSize = (uint)config.MaximumPoolSize,
            ConnectionLifeTime = DefaultConnectionLifetimeSeconds,
            SslMode = Enum.Parse<MySqlSslMode>(config.SslMode, ignoreCase: true),
        };
        if (!string.IsNullOrWhiteSpace(config.Database))
        {
            builder.Database = config.Database;
        }

        _connectionString = builder.ConnectionString;
    }

    /// <inheritdoc />
    public string ConnectionString => _connectionString;

    /// <inheritdoc />
    public Task<IAsyncDbConnection> CreateNewConnectionAsync(
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var conn = new MySqlConnection(_connectionString);
        return Task.FromResult<IAsyncDbConnection>(new AsyncMySqlConnection(conn));
    }

    /// <inheritdoc />
    public async Task<IAsyncDbConnection> OpenNewConnectionAsync(
        CancellationToken cancellationToken)
    {
        var conn = await CreateNewConnectionAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await conn.OpenAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            await conn.DisposeAsync().ConfigureAwait(false);
            throw;
        }
        catch (Exception ex)
        {
            await conn.DisposeAsync().ConfigureAwait(false);
            throw new InvalidOperationException(
                $"There was an error opening the connection to '{_config.Server}:{_config.Port}' " +
                $"to access database '{_config.Database}' as user '{_config.Username}'. " +
                "See inner exception for more details.",
                ex);
        }
        return conn;
    }
}
