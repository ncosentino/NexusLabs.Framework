using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

namespace NexusLabs.Framework.Logging;

/// <summary>
/// <see cref="ILogger"/> extensions that demote cooperative-cancellation exceptions
/// from warning/error severity to debug. Use these at any point where a caller
/// would otherwise log a warning/error for an exception that might be a routine
/// <see cref="OperationCanceledException"/> from a passing cancellation token.
/// </summary>
/// <remarks>
/// Named to avoid colliding with Microsoft.Extensions.Logging's own
/// <c>LoggerExtensions</c> in the same <c>ILogger</c>-extension space.
/// </remarks>
public static class LoggerCancellationExtensions
{
    /// <summary>
    /// Formats and writes a log message as <see cref="LogLevel.Warning"/> if
    /// <paramref name="exception"/> is NOT either <see cref="OperationCanceledException"/>
    /// or <see cref="TaskCanceledException"/>. Otherwise logs at
    /// <see cref="LogLevel.Debug"/> so the information is not entirely lost.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">
    /// Format string of the log message in message template format.
    /// Example: <c>"User {User} logged in from {Address}"</c>.
    /// </param>
    /// <param name="args">An object array that contains zero or more objects to format.</param>
    /// <example>
    /// <code language="csharp">
    /// logger.LogWarningIfNotCancellation(exception, "Error while processing request from {Address}", address)
    /// </code>
    /// </example>
    [SuppressMessage(
        "Usage",
        "CA2254:Template should be a static expression",
        Justification = "This method forwards a caller-supplied template + args to the underlying ILogger. The template cannot be a static expression here because it is the parameter being forwarded.")]
    public static void LogWarningIfNotCancellation(
        this ILogger logger,
        Exception? exception,
        string? message,
        params object?[] args)
    {
        if (exception is OperationCanceledException or TaskCanceledException)
        {
            logger.LogDebug(exception, message, args);
            return;
        }

        logger.LogWarning(exception, message, args);
    }
}
