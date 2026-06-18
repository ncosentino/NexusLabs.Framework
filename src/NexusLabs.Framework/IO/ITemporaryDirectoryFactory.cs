namespace NexusLabs.Framework.IO;

/// <summary>
/// Creates <see cref="ITemporaryDirectory"/> instances. This is the mockable seam consumers depend
/// on so their code can be unit tested without touching the real file system.
/// </summary>
public interface ITemporaryDirectoryFactory
{
    /// <summary>
    /// Creates a new uniquely-named temporary directory using default options.
    /// </summary>
    /// <returns>
    /// A disposable handle whose disposal deletes the directory and all of its contents.
    /// </returns>
    ITemporaryDirectory Create();

    /// <summary>
    /// Creates a new uniquely-named temporary directory using the supplied options.
    /// </summary>
    /// <param name="options">
    /// Controls the root location, name prefix, cleanup-failure handling, and optional delete
    /// resilience policy.
    /// </param>
    /// <returns>
    /// A disposable handle whose disposal deletes the directory and all of its contents.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is <see langword="null"/>.</exception>
    ITemporaryDirectory Create(TemporaryDirectoryOptions options);
}
