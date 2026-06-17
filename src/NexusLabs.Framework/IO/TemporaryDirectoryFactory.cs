using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace NexusLabs.Framework.IO;

/// <summary>
/// Default <see cref="ITemporaryDirectoryFactory"/> implementation. Creates uniquely-named
/// directories under a <c>NexusLabs</c> subfolder of the system temp path (or a caller-supplied
/// root) and hands back self-deleting <see cref="ITemporaryDirectory"/> handles.
/// </summary>
public sealed class TemporaryDirectoryFactory : ITemporaryDirectoryFactory
{
    private static readonly string DefaultRoot =
        System.IO.Path.Combine(System.IO.Path.GetTempPath(), "NexusLabs");

    private readonly TemporaryResourceLog _log;
    private readonly Func<string, Func<CancellationToken, ValueTask>> _deleteOnceFactory;

    /// <summary>
    /// Creates a factory that does not log cleanup failures. Without a logger, supply a
    /// <see cref="TemporaryDirectoryOptions.OnCleanupError"/> handler per creation to observe
    /// failed cleanups.
    /// </summary>
    public TemporaryDirectoryFactory()
        : this(NullLogger.Instance, TemporaryResourceDeleter.CreateDirectoryDeleteOnce)
    {
    }

    /// <summary>
    /// Creates a factory that logs cleanup failures via <paramref name="logger"/> whenever a
    /// creation does not supply its own <see cref="TemporaryDirectoryOptions.OnCleanupError"/>
    /// handler.
    /// </summary>
    /// <param name="logger">The logger used to report cleanup failures.</param>
    public TemporaryDirectoryFactory(ILogger<TemporaryDirectoryFactory> logger)
        : this(logger, TemporaryResourceDeleter.CreateDirectoryDeleteOnce)
    {
    }

    internal TemporaryDirectoryFactory(
        ILogger logger,
        Func<string, Func<CancellationToken, ValueTask>> deleteOnceFactory)
    {
        _log = new TemporaryResourceLog(logger);
        _deleteOnceFactory = deleteOnceFactory;
    }

    /// <inheritdoc />
    public ITemporaryDirectory Create() => Create(new TemporaryDirectoryOptions());

    /// <inheritdoc />
    public ITemporaryDirectory Create(TemporaryDirectoryOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var root = options.RootPath ?? DefaultRoot;
        Directory.CreateDirectory(root);

        var name = (options.Prefix ?? string.Empty) + Guid.NewGuid().ToString("N");
        var path = System.IO.Path.Combine(root, name);
        Directory.CreateDirectory(path);

        var onCleanupError = options.OnCleanupError ?? (error => _log.CleanupFailed(path, error));
        return new TemporaryDirectory(
            path,
            _deleteOnceFactory(path),
            options.DeleteExecutor,
            onCleanupError);
    }
}
