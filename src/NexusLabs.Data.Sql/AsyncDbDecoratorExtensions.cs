using System;
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
    /// <param name="connection">The connection to wrap. Disposed by the returned decorator.</param>
    /// <param name="leaseSemaphore">
    /// Externally-owned semaphore controlling pool capacity. The caller is responsible for
    /// its disposal. For a pool-cap pattern where over-release should fail fast, construct
    /// the semaphore as <c>new SemaphoreSlim(limit, limit)</c>.
    /// </param>
    /// <param name="acquisitionTimeout">
    /// Maximum time to wait for a pool slot on <c>OpenAsync</c>. Exceeding the budget throws
    /// <see cref="ConnectionPoolExhaustedException"/>. Use
    /// <see cref="System.Threading.Timeout.InfiniteTimeSpan"/> to wait indefinitely
    /// (cancellation-only).
    /// </param>
    public static IAsyncDbConnection WithLease(
        this IAsyncDbConnection connection,
        SemaphoreSlim leaseSemaphore,
        TimeSpan acquisitionTimeout) =>
        new LeasedAsyncDbConnection(connection, leaseSemaphore, acquisitionTimeout);

    /// <summary>
    /// Wraps <paramref name="connection"/> with an <see cref="OpenTrackingDecorator"/> that
    /// timestamps each open via <paramref name="timeProvider"/>.
    /// </summary>
    public static IAsyncDbConnection WithOpenTracking(
        this IAsyncDbConnection connection,
        OpenConnectionTracker tracker,
        TimeProvider timeProvider) =>
        new OpenTrackingDecorator(connection, tracker, timeProvider);

    /// <summary>Wraps <paramref name="command"/> with a <see cref="LoggingAsyncDbCommand"/>.</summary>
    public static IAsyncDbCommand WithLogging(
        this IAsyncDbCommand command,
        ILogger logger,
        LoggingAsyncDbCommandOptions? options = null) =>
        new LoggingAsyncDbCommand(command, logger, options);
}
