namespace NexusLabs.Framework.IO;

/// <summary>
/// A temporary file that deletes itself when disposed. Obtain one from
/// <see cref="ITemporaryFileFactory"/> and bind its lifetime to a scope with <c>using</c> /
/// <c>await using</c>:
/// <code>
/// await using var file = factory.Create();
/// await File.WriteAllTextAsync(file.Path, "...", cancellationToken);
/// // the file is deleted when the scope exits
/// </code>
/// </summary>
/// <remarks>
/// Disposal is idempotent, thread-safe, and never throws. Cleanup failures (for example the file
/// still being open on Windows) are routed to the handler configured at creation time
/// (<see cref="TemporaryFileOptions.OnCleanupError"/>) rather than thrown. On Windows, dispose any
/// stream you opened over this file <em>before</em> disposing the handle, otherwise the delete may
/// fail and surface through that handler. Supply a
/// <see cref="TemporaryFileOptions.DeleteExecutor"/> to retry transient lock failures.
/// </remarks>
public interface ITemporaryFile : IDisposable, IAsyncDisposable
{
    /// <summary>The absolute path to the temporary file.</summary>
    string Path { get; }

    /// <summary>Whether the file currently exists on disk.</summary>
    bool Exists { get; }
}
