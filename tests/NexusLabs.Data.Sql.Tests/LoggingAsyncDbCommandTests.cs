using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Moq;

using NexusLabs.Framework.Data;

using Xunit;

namespace NexusLabs.Data.Sql.Tests;

public sealed class LoggingAsyncDbCommandTests : IDisposable
{
    private readonly MockRepository _mocks = new(MockBehavior.Strict);
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    public void Dispose() => _mocks.VerifyAll();

    [Fact]
    public async Task ExecuteNonQueryAsync_LogsAndDelegates()
    {
        var inner = _mocks.Create<IAsyncDbCommand>();
        inner.Setup(c => c.CommandText).Returns("SELECT 1");
        inner.Setup(c => c.ExecuteNonQueryAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        var logger = _mocks.Create<ILogger>();
        logger.Setup(l => l.IsEnabled(LogLevel.Debug)).Returns(true);
        ExpectLog(logger, LogLevel.Debug, "ExecuteNonQueryAsync");
        var sut = new LoggingAsyncDbCommand(inner.Object, logger.Object);

        var result = await sut.ExecuteNonQueryAsync(_ct);

        Assert.Equal(1, result);
        inner.Verify(c => c.ExecuteNonQueryAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DefaultOptions_DoNotLogCommandText()
    {
        var inner = _mocks.Create<IAsyncDbCommand>();
        inner.Setup(c => c.CommandText).Returns("SELECT SecretValue FROM Vault");
        inner.Setup(c => c.ExecuteScalarAsync(It.IsAny<CancellationToken>())).ReturnsAsync(42);
        var logger = _mocks.Create<ILogger>();
        logger.Setup(l => l.IsEnabled(LogLevel.Debug)).Returns(true);
        ExpectLog(logger, LogLevel.Debug, "CommandTextLength=29");
        ExpectNotLogged(logger, LogLevel.Debug, "SecretValue");
        ExpectNotLogged(logger, LogLevel.Debug, "Vault");
        var sut = new LoggingAsyncDbCommand(inner.Object, logger.Object);

        await sut.ExecuteScalarAsync(_ct);
    }

    [Fact]
    public async Task IncludeCommandText_LogsFullText()
    {
        var inner = _mocks.Create<IAsyncDbCommand>();
        inner.Setup(c => c.CommandText).Returns("SELECT 1");
        inner.Setup(c => c.ExecuteNonQueryAsync(It.IsAny<CancellationToken>())).ReturnsAsync(0);
        var logger = _mocks.Create<ILogger>();
        logger.Setup(l => l.IsEnabled(LogLevel.Debug)).Returns(true);
        ExpectLog(logger, LogLevel.Debug, "SELECT 1");
        var sut = new LoggingAsyncDbCommand(
            inner.Object,
            logger.Object,
            new LoggingAsyncDbCommandOptions { IncludeCommandText = true });

        await sut.ExecuteNonQueryAsync(_ct);
    }

    [Fact]
    public async Task LogLevelOption_RespectsRequestedLevel()
    {
        var reader = _mocks.Create<IAsyncDbDataReader>();
        var inner = _mocks.Create<IAsyncDbCommand>();
        inner.Setup(c => c.CommandText).Returns("SELECT 1");
        inner.Setup(c => c.ExecuteReaderAsync(It.IsAny<CancellationToken>())).ReturnsAsync(reader.Object);
        var logger = _mocks.Create<ILogger>();
        logger.Setup(l => l.IsEnabled(LogLevel.Information)).Returns(true);
        ExpectLog(logger, LogLevel.Information, "ExecuteReaderAsync");
        var sut = new LoggingAsyncDbCommand(
            inner.Object,
            logger.Object,
            new LoggingAsyncDbCommandOptions { LogLevel = LogLevel.Information });

        await sut.ExecuteReaderAsync(_ct);
    }

    [Fact]
    public async Task DisabledLogLevel_SkipsLogging_ButStillDelegates()
    {
        var inner = _mocks.Create<IAsyncDbCommand>();
        inner.Setup(c => c.ExecuteScalarAsync(It.IsAny<CancellationToken>())).ReturnsAsync(7);
        var logger = _mocks.Create<ILogger>();
        logger.Setup(l => l.IsEnabled(LogLevel.Debug)).Returns(false);
        var sut = new LoggingAsyncDbCommand(inner.Object, logger.Object);

        await sut.ExecuteScalarAsync(_ct);

        inner.Verify(c => c.ExecuteScalarAsync(It.IsAny<CancellationToken>()), Times.Once);

        // Strict mock: an unconfigured Log call throws, so omitting any Log setup
        // already asserts logging was skipped when the level is disabled.
    }

    /// <summary>
    /// Asserts the SUT calls <see cref="ILogger.Log"/> once at <paramref name="level"/> with a
    /// formatted message containing <paramref name="messageSubstring"/>. Implemented as a
    /// Moq setup so VerifyAll picks up the assertion at test teardown.
    /// </summary>
    private static void ExpectLog(
        Mock<ILogger> logger,
        LogLevel level,
        string messageSubstring)
    {
        logger
            .Setup(l => l.Log(
                level,
                new EventId(0),
                It.Is<It.IsAnyType>((state, _) => state.ToString()!.Contains(messageSubstring, StringComparison.Ordinal)),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()))
            .Verifiable(Times.Once);
    }

    /// <summary>
    /// Asserts the SUT NEVER calls <see cref="ILogger.Log"/> with a formatted message
    /// containing <paramref name="messageSubstring"/>. Uses Times.Never; no setup needed
    /// because the absence is verified directly.
    /// </summary>
    private static void ExpectNotLogged(
        Mock<ILogger> logger,
        LogLevel level,
        string messageSubstring)
    {
        logger
            .Setup(l => l.Log(
                level,
                new EventId(0),
                It.Is<It.IsAnyType>((state, _) => state.ToString()!.Contains(messageSubstring, StringComparison.Ordinal)),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()))
            .Verifiable(Times.Never);
    }
}
