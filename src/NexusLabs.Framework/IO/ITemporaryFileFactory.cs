namespace NexusLabs.Framework.IO;

/// <summary>
/// Creates <see cref="ITemporaryFile"/> instances. This is the mockable seam consumers depend on so
/// their code can be unit tested without touching the real file system.
/// </summary>
public interface ITemporaryFileFactory
{
    /// <summary>
    /// Creates a new uniquely-named temporary file using default options.
    /// </summary>
    /// <returns>A disposable handle whose disposal deletes the file.</returns>
    ITemporaryFile Create();

    /// <summary>
    /// Creates a new uniquely-named temporary file using the supplied options.
    /// </summary>
    /// <param name="options">
    /// Controls the root location, name prefix, extension, whether the file is pre-created,
    /// cleanup-failure handling, and optional delete resilience policy.
    /// </param>
    /// <returns>A disposable handle whose disposal deletes the file.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is <see langword="null"/>.</exception>
    ITemporaryFile Create(TemporaryFileOptions options);
}
