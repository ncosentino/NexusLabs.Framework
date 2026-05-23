using System;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Xunit;

namespace NexusLabs.Framework.Tests;

public sealed class TryTests
{
    [Fact]
    public async Task Async_NoExceptionThrown_ReturnsNull()
    {
        var error = await Try.Async(async () => { await Task.Yield(); });

        Assert.Null(error);
    }

    [Fact]
    public async Task Async_ExceptionThrown_ReturnsTheException()
    {
        var thrown = new InvalidOperationException("expected");
        var error = await Try.Async(async () => { await Task.Yield(); throw thrown; });

        Assert.Same(thrown, error);
    }

    [Fact]
    public async Task Async_WithLogger_ExceptionThrown_ReturnsTheExceptionAndLogs()
    {
        var thrown = new InvalidOperationException("expected");
        var logger = new RecordingLogger();
        var error = await Try.Async(
            logger,
            async () => { await Task.Yield(); throw thrown; });

        Assert.Same(thrown, error);
        Assert.Contains(logger.Records, r => r.Level == LogLevel.Error && ReferenceEquals(r.Exception, thrown));
    }

    [Fact]
    public async Task Async_WithLogger_CancellationThrown_LogsAtDebugNotError()
    {
        var logger = new RecordingLogger();
        var error = await Try.Async(
            logger,
            async () => { await Task.Yield(); throw new OperationCanceledException("cancel"); });

        Assert.IsType<OperationCanceledException>(error);
        Assert.Contains(logger.Records, r => r.Level == LogLevel.Debug);
        Assert.DoesNotContain(logger.Records, r => r.Level == LogLevel.Error);
    }

    [Fact]
    public void Get_SuccessfulTriedExCallback_PassesThrough()
    {
        TriedEx<int> CallbackReturning() => 42;
        var result = Try.Get(CallbackReturning);

        Assert.True(result.Success);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void Get_CallbackThrows_ReturnsTriedExWithException()
    {
        var thrown = new InvalidOperationException("expected");
        TriedEx<int> CallbackThrowing() => throw thrown;
        var result = Try.Get<int>(CallbackThrowing);

        Assert.False(result.Success);
        Assert.Same(thrown, result.Error);
    }

    [Fact]
    public async Task GetAsync_SuccessfulTriedExCallback_PassesThrough()
    {
        var result = await Try.GetAsync(async () => { await Task.Yield(); return (TriedEx<int>)42; });

        Assert.True(result.Success);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public async Task GetAsync_WithLogger_CallbackThrows_LogsAndReturns()
    {
        var thrown = new InvalidOperationException("expected");
        var logger = new RecordingLogger();
        var result = await Try.GetAsync<int>(
            logger,
            async () => { await Task.Yield(); throw thrown; });

        Assert.False(result.Success);
        Assert.Same(thrown, result.Error);
        Assert.Contains(logger.Records, r => r.Level == LogLevel.Error && ReferenceEquals(r.Exception, thrown));
    }

    [Fact]
    public async Task ToCompletionOrCanceledAsync_CompletesNormally_ReturnsTrue()
    {
        var completed = await Try.ToCompletionOrCanceledAsync(async () => { await Task.Yield(); });

        Assert.True(completed);
    }

    [Fact]
    public async Task ToCompletionOrCanceledAsync_Cancelled_ReturnsFalse()
    {
        var completed = await Try.ToCompletionOrCanceledAsync(
            async () => { await Task.Yield(); throw new OperationCanceledException(); });

        Assert.False(completed);
    }

    [Fact]
    public void CombineErrors_BothTriedExFailed_ReturnsAggregateException()
    {
        var ex1 = new InvalidOperationException("one");
        var ex2 = new InvalidOperationException("two");
        TriedEx<int> a = ex1;
        TriedEx<string> b = ex2;

        var combined = Try.CombineErrors(a, b);

        var agg = Assert.IsType<AggregateException>(combined);
        Assert.Contains(ex1, agg.InnerExceptions);
        Assert.Contains(ex2, agg.InnerExceptions);
    }

    [Fact]
    public void CombineErrors_OneTriedExFailed_ReturnsThatError()
    {
        var ex = new InvalidOperationException("only one");
        TriedEx<int> a = ex;
        TriedEx<string> b = "ok";

        var combined = Try.CombineErrors(a, b);

        Assert.Same(ex, combined);
    }

    [Fact]
    public void CombineErrors_BothSuccessful_Throws()
    {
        TriedEx<int> a = 1;
        TriedEx<string> b = "ok";

        Assert.Throws<ArgumentException>(() => Try.CombineErrors(a, b));
    }

    [Fact]
    public void CombineErrorsIfNeeded_BothSuccessful_ReturnsNull()
    {
        TriedEx<int> a = 1;
        TriedEx<string> b = "ok";

        var combined = Try.CombineErrorsIfNeeded(a, b);

        Assert.Null(combined);
    }

    [Fact]
    public void CombineErrors_ExceptionAndNull_ReturnsTheException()
    {
        var ex = new InvalidOperationException("solo");

        var combined = Try.CombineErrors(ex, null);

        Assert.Same(ex, combined);
    }

    [Fact]
    public void CombineErrors_BothNullExceptions_ReturnsNull()
    {
        Exception? a = null;
        Exception? b = null;

        var combined = Try.CombineErrors(a, b);

        Assert.Null(combined);
    }

    private sealed class RecordingLogger : ILogger
    {
        public sealed record Record(LogLevel Level, Exception? Exception, string Message);

        public System.Collections.Generic.List<Record> Records { get; } = new();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Records.Add(new Record(logLevel, exception, formatter(state, exception)));
        }
    }
}
