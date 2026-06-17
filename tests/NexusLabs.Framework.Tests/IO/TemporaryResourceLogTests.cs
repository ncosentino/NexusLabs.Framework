using System;
using System.IO;

using Microsoft.Extensions.Logging;

using Moq;

using NexusLabs.Framework.IO;

using Xunit;

namespace NexusLabs.Framework.Tests.IO;

public sealed class TemporaryResourceLogTests
{
    [Fact]
    public void CleanupFailed_LogsWarningWithExpectedEventId()
    {
        var repo = new MockRepository(MockBehavior.Strict);
        var logger = repo.Create<ILogger>();
        logger.Setup(l => l.IsEnabled(LogLevel.Warning)).Returns(true);
        logger.Setup(l => l.Log(
            LogLevel.Warning,
            It.Is<EventId>(e => e.Id == TemporaryResourceLog.CleanupFailedEventId),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<IOException>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()));

        var log = new TemporaryResourceLog(logger.Object);
        log.CleanupFailed(@"C:\temp\x", new IOException("boom"));

        repo.VerifyAll();
    }
}
