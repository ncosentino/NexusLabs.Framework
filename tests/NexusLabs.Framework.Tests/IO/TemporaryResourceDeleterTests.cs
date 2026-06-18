using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging.Abstractions;

using NexusLabs.Framework.IO;

using Xunit;

namespace NexusLabs.Framework.Tests.IO;

public sealed class TemporaryResourceDeleterTests
{
    private readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    [Fact]
    public async Task DeleteAsync_NoExecutor_RunsDeleteOnceExactlyOnce()
    {
        var attempts = 0;
        Func<CancellationToken, ValueTask> deleteOnce = _ =>
        {
            attempts++;
            return ValueTask.CompletedTask;
        };

        var error = await TemporaryResourceDeleter.DeleteAsync(deleteOnce, executor: null, _ct);

        Assert.Null(error);
        Assert.Equal(1, attempts);
    }

    [Fact]
    public async Task DeleteAsync_NoExecutor_Failure_ReturnsException()
    {
        var boom = new IOException("nope");
        Func<CancellationToken, ValueTask> deleteOnce = _ => throw boom;

        var error = await TemporaryResourceDeleter.DeleteAsync(deleteOnce, executor: null, _ct);

        Assert.Same(boom, error);
    }

    [Fact]
    public async Task DeleteAsync_AlreadyGone_IsNormalizedToSuccess()
    {
        Func<CancellationToken, ValueTask> deleteOnce = _ => throw new DirectoryNotFoundException();

        var error = await TemporaryResourceDeleter.DeleteAsync(deleteOnce, executor: null, _ct);

        Assert.Null(error);
    }

    [Fact]
    public async Task DeleteAsync_RetryExecutor_RetriesTransientFailureThenSucceeds()
    {
        var attempts = 0;
        Func<CancellationToken, ValueTask> deleteOnce = _ =>
        {
            attempts++;
            if (attempts < 3)
            {
                throw new IOException("transient lock");
            }

            return ValueTask.CompletedTask;
        };

        var error = await TemporaryResourceDeleter.DeleteAsync(deleteOnce, RetryExecutor(5), _ct);

        Assert.Null(error);
        Assert.Equal(3, attempts);
    }

    [Fact]
    public async Task DeleteAsync_RetryExecutor_Exhausted_ReturnsFinalException()
    {
        var attempts = 0;
        var boom = new IOException("always");
        Func<CancellationToken, ValueTask> deleteOnce = _ =>
        {
            attempts++;
            throw boom;
        };

        var error = await TemporaryResourceDeleter.DeleteAsync(deleteOnce, RetryExecutor(3), _ct);

        Assert.Same(boom, error);
        Assert.Equal(3, attempts);
    }

    [Fact]
    public async Task DeleteAsync_CustomExecutorLambda_IsHonored()
    {
        var executorCalls = 0;
        ResilientDeleteExecutor executor = async (operation, ct) =>
        {
            executorCalls++;
            await operation(ct);
        };

        var deleteCalls = 0;
        Func<CancellationToken, ValueTask> deleteOnce = _ =>
        {
            deleteCalls++;
            return ValueTask.CompletedTask;
        };

        var error = await TemporaryResourceDeleter.DeleteAsync(deleteOnce, executor, _ct);

        Assert.Null(error);
        Assert.Equal(1, executorCalls);
        Assert.Equal(1, deleteCalls);
    }

    private static ResilientDeleteExecutor RetryExecutor(int maxAttempts) =>
        async (operation, cancellationToken) =>
        {
            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                var error = await Try.Async(
                    NullLogger.Instance,
                    () => operation(cancellationToken).AsTask());
                if (error is null)
                {
                    return;
                }

                if (attempt == maxAttempts)
                {
                    throw error;
                }
            }
        };
}
