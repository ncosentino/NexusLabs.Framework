namespace NexusLabs.Framework.IO;

internal sealed class TemporaryFile : ITemporaryFile
{
    private readonly Func<CancellationToken, ValueTask> _deleteOnce;
    private readonly ResilientDeleteExecutor? _executor;
    private readonly Action<Exception>? _onCleanupError;
    private int _disposed;

    internal TemporaryFile(
        string path,
        Func<CancellationToken, ValueTask> deleteOnce,
        ResilientDeleteExecutor? executor,
        Action<Exception>? onCleanupError)
    {
        Path = path;
        _deleteOnce = deleteOnce;
        _executor = executor;
        _onCleanupError = onCleanupError;
    }

    public string Path { get; }

    public bool Exists => File.Exists(Path);

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        var error = TemporaryResourceDeleter.DeleteBlocking(_deleteOnce, _executor);
        if (error is not null)
        {
            _onCleanupError?.Invoke(error);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        var error = await TemporaryResourceDeleter
            .DeleteAsync(_deleteOnce, _executor, CancellationToken.None)
            .ConfigureAwait(false);
        if (error is not null)
        {
            _onCleanupError?.Invoke(error);
        }
    }
}
