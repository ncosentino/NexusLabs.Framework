namespace NexusLabs.Framework.IO;

/// <summary>
/// A temporary directory that deletes itself (and its entire contents) when disposed. Obtain one
/// from <see cref="ITemporaryDirectoryFactory"/> and bind its lifetime to a scope with
/// <c>using</c> / <c>await using</c>:
/// <code>
/// await using var dir = factory.Create();
/// await File.WriteAllTextAsync(Path.Combine(dir.Path, "data.txt"), "...", cancellationToken);
/// // the directory and everything under it is deleted when the scope exits
/// </code>
/// </summary>
/// <remarks>
/// Disposal is idempotent, thread-safe, and never throws. Cleanup failures (for example a file
/// still locked on Windows) are routed to the handler configured at creation time
/// (<see cref="TemporaryDirectoryOptions.OnCleanupError"/>) rather than thrown. On Windows, dispose
/// any streams you opened under this directory <em>before</em> disposing the handle, otherwise the
/// delete may fail and surface through that handler. Supply a
/// <see cref="TemporaryDirectoryOptions.DeleteExecutor"/> to retry transient lock failures.
/// </remarks>
public interface ITemporaryDirectory : IDisposable, IAsyncDisposable
{
    /// <summary>The absolute path to the temporary directory.</summary>
    string Path { get; }

    /// <summary>Whether the directory currently exists on disk.</summary>
    bool Exists { get; }
}
