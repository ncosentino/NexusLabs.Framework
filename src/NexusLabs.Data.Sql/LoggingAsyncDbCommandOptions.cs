using Microsoft.Extensions.Logging;

namespace NexusLabs.Data.Sql;

/// <summary>Options for <see cref="LoggingAsyncDbCommand"/>.</summary>
public sealed class LoggingAsyncDbCommandOptions
{
    /// <summary>
    /// When <c>true</c>, the full <c>CommandText</c> is included in log output. Default
    /// <c>false</c> to avoid accidentally logging inlined parameter values.
    /// </summary>
    public bool IncludeCommandText { get; init; }

    /// <summary>Log level used for command-execution entries. Default <see cref="LogLevel.Debug"/>.</summary>
    public LogLevel LogLevel { get; init; } = LogLevel.Debug;
}
