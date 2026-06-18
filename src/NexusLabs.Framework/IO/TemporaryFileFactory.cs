using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace NexusLabs.Framework.IO;

/// <summary>
/// Default <see cref="ITemporaryFileFactory"/> implementation. Creates uniquely-named files under a
/// <c>NexusLabs</c> subfolder of the system temp path (or a caller-supplied root) and hands back
/// self-deleting <see cref="ITemporaryFile"/> handles.
/// </summary>
public sealed class TemporaryFileFactory : ITemporaryFileFactory
{
    private static readonly string DefaultRoot =
        System.IO.Path.Combine(System.IO.Path.GetTempPath(), "NexusLabs");

    private readonly TemporaryResourceLog _log;
    private readonly Func<string, Func<CancellationToken, ValueTask>> _deleteOnceFactory;

    /// <summary>
    /// Creates a factory that does not log cleanup failures. Without a logger, supply a
    /// <see cref="TemporaryFileOptions.OnCleanupError"/> handler per creation to observe failed
    /// cleanups.
    /// </summary>
    public TemporaryFileFactory()
        : this(NullLogger.Instance, TemporaryResourceDeleter.CreateFileDeleteOnce)
    {
    }

    /// <summary>
    /// Creates a factory that logs cleanup failures via <paramref name="logger"/> whenever a
    /// creation does not supply its own <see cref="TemporaryFileOptions.OnCleanupError"/> handler.
    /// </summary>
    /// <param name="logger">The logger used to report cleanup failures.</param>
    public TemporaryFileFactory(ILogger<TemporaryFileFactory> logger)
        : this(logger, TemporaryResourceDeleter.CreateFileDeleteOnce)
    {
    }

    internal TemporaryFileFactory(
        ILogger logger,
        Func<string, Func<CancellationToken, ValueTask>> deleteOnceFactory)
    {
        _log = new TemporaryResourceLog(logger);
        _deleteOnceFactory = deleteOnceFactory;
    }

    /// <inheritdoc />
    public ITemporaryFile Create() => Create(new TemporaryFileOptions());

    /// <inheritdoc />
    public ITemporaryFile Create(TemporaryFileOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var root = options.RootPath ?? DefaultRoot;
        Directory.CreateDirectory(root);

        var name = (options.Prefix ?? string.Empty)
            + Guid.NewGuid().ToString("N")
            + (options.Extension ?? string.Empty);
        var path = System.IO.Path.Combine(root, name);

        if (options.CreateEmptyFile)
        {
            using var stream = new FileStream(
                path,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None);
        }

        var onCleanupError = options.OnCleanupError ?? (error => _log.CleanupFailed(path, error));
        return new TemporaryFile(
            path,
            _deleteOnceFactory(path),
            options.DeleteExecutor,
            onCleanupError);
    }
}
