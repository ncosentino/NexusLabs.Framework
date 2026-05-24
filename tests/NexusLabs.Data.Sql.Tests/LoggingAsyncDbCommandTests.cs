using System;
using System.Collections.Generic;
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

    public void Dispose() => _mocks.VerifyAll();

    [Fact]
    public async Task ExecuteNonQueryAsync_LogsAndDelegates()
    {
        var inner = _mocks.Create<IAsyncDbCommand>();
        inner.Setup(c => c.CommandText).Returns("SELECT 1");
        inner.Setup(c => c.ExecuteNonQueryAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        var log = new CapturingLogger();
        var sut = new LoggingAsyncDbCommand(inner.Object, log);

        var result = await sut.ExecuteNonQueryAsync();

        Assert.Equal(1, result);
        inner.Verify(c => c.ExecuteNonQueryAsync(It.IsAny<CancellationToken>()), Times.Once);
        Assert.Single(log.Entries);
        Assert.Contains("ExecuteNonQueryAsync", log.Entries[0].Message);
    }

    [Fact]
    public async Task DefaultOptions_DoNotLogCommandText()
    {
        var inner = _mocks.Create<IAsyncDbCommand>();
        inner.Setup(c => c.CommandText).Returns("SELECT SecretValue FROM Vault");
        inner.Setup(c => c.ExecuteScalarAsync(It.IsAny<CancellationToken>())).ReturnsAsync(42);
        var log = new CapturingLogger();
        var sut = new LoggingAsyncDbCommand(inner.Object, log);

        await sut.ExecuteScalarAsync();

        Assert.Single(log.Entries);
        Assert.DoesNotContain("SecretValue", log.Entries[0].Message);
        Assert.DoesNotContain("Vault", log.Entries[0].Message);
        Assert.Contains("CommandTextLength=29", log.Entries[0].Message);
    }

    [Fact]
    public async Task IncludeCommandText_LogsFullText()
    {
        var inner = _mocks.Create<IAsyncDbCommand>();
        inner.Setup(c => c.CommandText).Returns("SELECT 1");
        inner.Setup(c => c.ExecuteNonQueryAsync(It.IsAny<CancellationToken>())).ReturnsAsync(0);
        var log = new CapturingLogger();
        var sut = new LoggingAsyncDbCommand(
            inner.Object,
            log,
            new LoggingAsyncDbCommandOptions { IncludeCommandText = true });

        await sut.ExecuteNonQueryAsync();

        Assert.Single(log.Entries);
        Assert.Contains("SELECT 1", log.Entries[0].Message);
    }

    [Fact]
    public async Task LogLevelOption_RespectsRequestedLevel()
    {
        var inner = _mocks.Create<IAsyncDbCommand>();
        inner.Setup(c => c.CommandText).Returns("SELECT 1");
        inner.Setup(c => c.ExecuteReaderAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<IAsyncDbDataReader>());
        var log = new CapturingLogger();
        var sut = new LoggingAsyncDbCommand(
            inner.Object,
            log,
            new LoggingAsyncDbCommandOptions { LogLevel = LogLevel.Information });

        await sut.ExecuteReaderAsync();

        Assert.Single(log.Entries);
        Assert.Equal(LogLevel.Information, log.Entries[0].Level);
    }

    [Fact]
    public async Task DisabledLogLevel_SkipsLogging_ButStillDelegates()
    {
        var inner = _mocks.Create<IAsyncDbCommand>();
        inner.Setup(c => c.ExecuteScalarAsync(It.IsAny<CancellationToken>())).ReturnsAsync(7);
        var log = new CapturingLogger { MinimumLevel = LogLevel.Warning };
        var sut = new LoggingAsyncDbCommand(inner.Object, log);

        await sut.ExecuteScalarAsync();

        Assert.Empty(log.Entries);
        inner.Verify(c => c.ExecuteScalarAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private sealed class CapturingLogger : ILogger
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = new();

        public LogLevel MinimumLevel { get; set; } = LogLevel.Trace;

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;
        public bool IsEnabled(LogLevel logLevel) => logLevel >= MinimumLevel;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }
            Entries.Add((logLevel, formatter(state, exception)));
        }

        private sealed class NullScope : IDisposable
        {
            public static NullScope Instance { get; } = new();
            public void Dispose() { }
        }
    }
}
