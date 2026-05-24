using System.Threading;

using Microsoft.Extensions.Logging;

using NexusLabs.Framework.Data;

namespace NexusLabs.Data.Sql;

/// <summary>
/// Fluent extension methods for composing <see cref="IAsyncDbConnection"/> and
/// <see cref="IAsyncDbCommand"/> decorators.
/// </summary>
/// <remarks>
/// Recommended composition order is <c>inner.WithLease(...).WithOpenTracking(...)</c>: lease
/// innermost so the time spent waiting on a lease is observable by an outer tracker or logger.
/// </remarks>
public static class AsyncDbDecoratorExtensions
{
    /// <summary>
    /// Wraps <paramref name="connection"/> with a <see cref="LeasedAsyncDbConnection"/>.
    /// </summary>
    /// <remarks>
    /// The caller owns the <see cref="SemaphoreSlim"/> and is responsible for its disposal.
    /// For a pool-cap pattern where over-release should fail fast, construct the semaphore
    /// as <c>new SemaphoreSlim(limit, limit)</c>.
    /// </remarks>
    public static IAsyncDbConnection WithLease(
        this IAsyncDbConnection connection,
        SemaphoreSlim leaseSemaphore) =>
        new LeasedAsyncDbConnection(connection, leaseSemaphore);

    /// <summary>Wraps <paramref name="connection"/> with an <see cref="OpenTrackingDecorator"/>.</summary>
    public static IAsyncDbConnection WithOpenTracking(
        this IAsyncDbConnection connection,
        OpenConnectionTracker tracker) =>
        new OpenTrackingDecorator(connection, tracker);

    /// <summary>Wraps <paramref name="command"/> with a <see cref="LoggingAsyncDbCommand"/>.</summary>
    public static IAsyncDbCommand WithLogging(
        this IAsyncDbCommand command,
        ILogger logger,
        LoggingAsyncDbCommandOptions? options = null) =>
        new LoggingAsyncDbCommand(command, logger, options);
}
