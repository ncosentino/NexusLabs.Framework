namespace NexusLabs.Framework.IO;

/// <summary>
/// Shared deletion engine for temporary files and directories. Splits cleanup into a
/// read-only-clearing, reparse-point-safe <em>delete-once</em> operation and a resilience wrapper
/// that runs it through an optional <see cref="ResilientDeleteExecutor"/>. The final outcome is
/// returned as an <see cref="Exception"/> (never thrown) so callers can surface it however they
/// choose; an already-deleted resource is treated as success.
/// </summary>
internal static class TemporaryResourceDeleter
{
    internal static Func<CancellationToken, ValueTask> CreateDirectoryDeleteOnce(string path) =>
        _ =>
        {
            DeleteDirectory(path);
            return ValueTask.CompletedTask;
        };

    internal static Func<CancellationToken, ValueTask> CreateFileDeleteOnce(string path) =>
        _ =>
        {
            DeleteFile(path);
            return ValueTask.CompletedTask;
        };

    internal static async ValueTask<Exception?> DeleteAsync(
        Func<CancellationToken, ValueTask> deleteOnce,
        ResilientDeleteExecutor? executor,
        CancellationToken cancellationToken)
    {
        var error = await Try
            .Async(async () =>
            {
                if (executor is null)
                {
                    await deleteOnce(cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    await executor(deleteOnce, cancellationToken).ConfigureAwait(false);
                }
            })
            .ConfigureAwait(false);

        return Normalize(error);
    }

    internal static Exception? DeleteBlocking(
        Func<CancellationToken, ValueTask> deleteOnce,
        ResilientDeleteExecutor? executor)
    {
        var pending = DeleteAsync(deleteOnce, executor, CancellationToken.None);
        return pending.IsCompleted
            ? pending.Result
            : pending.AsTask().GetAwaiter().GetResult();
    }

    private static Exception? Normalize(Exception? error) =>
        error is DirectoryNotFoundException or FileNotFoundException
            ? null
            : error;

    private static void DeleteDirectory(string path)
    {
        if (!Directory.Exists(path))
        {
            return;
        }

        var directory = new DirectoryInfo(path);
        foreach (var child in directory.GetFileSystemInfos())
        {
            if (child.Attributes.HasFlag(FileAttributes.ReparsePoint))
            {
                ClearReadOnly(child);
                child.Delete();
            }
            else if (child is DirectoryInfo childDirectory)
            {
                DeleteDirectory(childDirectory.FullName);
            }
            else
            {
                ClearReadOnly(child);
                child.Delete();
            }
        }

        ClearReadOnly(directory);
        directory.Delete(recursive: false);
    }

    private static void DeleteFile(string path)
    {
        var file = new FileInfo(path);
        if (!file.Exists)
        {
            return;
        }

        ClearReadOnly(file);
        file.Delete();
    }

    private static void ClearReadOnly(FileSystemInfo info)
    {
        if (info.Attributes.HasFlag(FileAttributes.ReadOnly))
        {
            info.Attributes &= ~FileAttributes.ReadOnly;
        }
    }
}
