namespace NexusLabs.Framework.IO;

/// <summary>
/// Options controlling how an <see cref="ITemporaryDirectory"/> is created and cleaned up. All
/// members are optional; an instance with no members set produces a uniquely-named directory under
/// the system temp path with no special cleanup handling.
/// </summary>
public sealed record TemporaryDirectoryOptions
{
    /// <summary>
    /// The directory under which the temporary directory is created. When <see langword="null"/>,
    /// a <c>NexusLabs</c> subfolder of <see cref="System.IO.Path.GetTempPath"/> is used. The root is
    /// created if it does not already exist.
    /// </summary>
    public string? RootPath { get; init; }

    /// <summary>
    /// An optional prefix prepended to the generated unique name. Useful for making leaked
    /// directories easy to identify. When <see langword="null"/>, no prefix is applied.
    /// </summary>
    public string? Prefix { get; init; }

    /// <summary>
    /// Invoked when deletion fails during disposal, with the final exception (after any
    /// <see cref="DeleteExecutor"/> retries are exhausted). Disposal never throws; this handler is
    /// how a caller observes a failed cleanup. When <see langword="null"/>, the creating factory's
    /// logger is used instead.
    /// </summary>
    public Action<Exception>? OnCleanupError { get; init; }

    /// <summary>
    /// An optional resilience policy used to execute the delete during disposal — for example a
    /// retry/backoff pipeline's execute method supplied as a method group. When
    /// <see langword="null"/>, the delete is attempted exactly once.
    /// </summary>
    public ResilientDeleteExecutor? DeleteExecutor { get; init; }
}
