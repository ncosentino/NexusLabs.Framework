using Microsoft.Extensions.Logging;

namespace NexusLabs.Framework.IO;

/// <summary>
/// Source-generated log messages emitted by the temporary file/directory factories when cleanup
/// fails and no caller-supplied <c>OnCleanupError</c> handler is configured.
/// </summary>
internal sealed partial class TemporaryResourceLog(ILogger logger)
{
    /// <summary>
    /// The <see cref="EventId"/> identifier used by <see cref="CleanupFailed"/>.
    /// </summary>
    internal const int CleanupFailedEventId = 1001;

    [LoggerMessage(
        EventId = CleanupFailedEventId,
        Level = LogLevel.Warning,
        Message = "Failed to delete temporary resource at {Path}")]
    public partial void CleanupFailed(string path, Exception exception);
}
