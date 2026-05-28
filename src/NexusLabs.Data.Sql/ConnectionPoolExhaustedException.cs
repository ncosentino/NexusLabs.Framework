using System;

namespace NexusLabs.Data.Sql;

/// <summary>
/// Thrown when a connection acquire cannot obtain a pool slot within the configured
/// acquisition timeout. Carries the timeout that was budgeted for the acquire so callers
/// (and operators reading logs) can correlate the failure with the configured cap.
/// </summary>
/// <remarks>
/// Derives from <see cref="InvalidOperationException"/> so existing connection-management
/// catch handlers (which conventionally catch <see cref="InvalidOperationException"/> around
/// open operations) remain compatible.
/// </remarks>
public sealed class ConnectionPoolExhaustedException : InvalidOperationException
{
    /// <summary>Creates an empty <see cref="ConnectionPoolExhaustedException"/>.</summary>
    public ConnectionPoolExhaustedException()
        : base()
    {
    }

    /// <summary>Creates a <see cref="ConnectionPoolExhaustedException"/> with a message.</summary>
    public ConnectionPoolExhaustedException(string message)
        : base(message)
    {
    }

    /// <summary>Creates a <see cref="ConnectionPoolExhaustedException"/> with a message and inner exception.</summary>
    public ConnectionPoolExhaustedException(string message, Exception? innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Creates a new <see cref="ConnectionPoolExhaustedException"/> with a structured
    /// acquisition timeout. Preferred over the message-only ctors.
    /// </summary>
    /// <param name="acquisitionTimeout">The acquisition budget that elapsed without a slot.</param>
    /// <param name="innerException">The underlying timeout (typically <see cref="TimeoutException"/>).</param>
    public ConnectionPoolExhaustedException(
        TimeSpan acquisitionTimeout,
        Exception? innerException = null)
        : base(
            BuildMessage(acquisitionTimeout),
            innerException)
    {
        AcquisitionTimeout = acquisitionTimeout;
    }

    /// <summary>
    /// The acquisition timeout that elapsed without a slot becoming available. Returns
    /// <see cref="TimeSpan.Zero"/> when the exception was constructed via one of the
    /// standard message-only constructors.
    /// </summary>
    public TimeSpan AcquisitionTimeout { get; }

    private static string BuildMessage(TimeSpan acquisitionTimeout) =>
        $"Failed to acquire a connection pool slot within {acquisitionTimeout}.";
}
