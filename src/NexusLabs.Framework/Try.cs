using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

namespace NexusLabs.Framework;

/// <summary>
/// Higher-level wrappers around <see cref="Safely"/> that combine the result-pattern
/// callback execution with logging and telemetry forwarding. Use the
/// <see cref="ILogger"/>-accepting overloads to log captured exceptions; use the
/// no-logger overloads when only the result is interesting.
/// </summary>
/// <remarks>
/// Cancellation exceptions (<see cref="OperationCanceledException"/>,
/// <see cref="TaskCanceledException"/>) are logged at Debug level instead of Error
/// to avoid noise in cooperative-cancellation scenarios. See
/// <see cref="LoggerCancellationExtensions.LogWarningIfNotCancellation"/> for the
/// equivalent guarantee on caller-side warning logs.
/// </remarks>
public static class Try
{
    /// <summary>
    /// Executes the callback and logs any exception that is thrown.
    /// </summary>
    /// <param name="logger">The logger to use.</param>
    /// <param name="callback">The callback to execute.</param>
    /// <param name="caller">
    /// The name of the calling method. Leave as null to automatically use the name
    /// of the calling method via <see cref="CallerMemberNameAttribute"/>.
    /// </param>
    /// <returns>The captured exception if one was thrown; otherwise <c>null</c>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerStepThrough]
    public static async Task<Exception?> Async(
        ILogger logger,
        Func<Task> callback,
        [CallerMemberName] string? caller = null)
    {
        try
        {
            await callback
                .Invoke()
                .ConfigureAwait(false);
            return null;
        }
        catch (Exception ex)
        {
            LoggingAndTelemetry(logger, caller, ex);
            return ex;
        }
    }

    /// <summary>
    /// Executes the callback and returns the captured exception, if any.
    /// </summary>
    /// <param name="callback">The callback to execute.</param>
    /// <returns>The captured exception if one was thrown; otherwise <c>null</c>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerStepThrough]
    public static async Task<Exception?> Async(
        Func<Task> callback)
    {
        try
        {
            await callback
                .Invoke()
                .ConfigureAwait(false);
            return null;
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    /// <summary>
    /// Executes a callback that itself returns an <see cref="Exception"/>? (already-handled
    /// failure) and additionally logs any exception thrown during the callback execution.
    /// </summary>
    /// <param name="logger">The logger to use.</param>
    /// <param name="callback">The callback to execute.</param>
    /// <param name="caller">
    /// The name of the calling method. Leave as null to automatically use the name
    /// of the calling method via <see cref="CallerMemberNameAttribute"/>.
    /// </param>
    /// <returns>
    /// The exception the callback returned, OR the exception thrown by the callback,
    /// OR <c>null</c> if the callback completed successfully and reported no error.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerStepThrough]
    public static async Task<Exception?> Async(
        ILogger logger,
        Func<Task<Exception?>> callback,
        [CallerMemberName] string? caller = null)
    {
        try
        {
            var maybeError = await callback
                .Invoke()
                .ConfigureAwait(false);
            return maybeError;
        }
        catch (Exception ex)
        {
            LoggingAndTelemetry(logger, caller, ex);
            return ex;
        }
    }

    /// <summary>
    /// Executes a callback that itself returns an <see cref="Exception"/>? and returns
    /// either that error, the catch-captured exception, or <c>null</c>.
    /// </summary>
    /// <param name="callback">The callback to execute.</param>
    /// <returns>
    /// The exception the callback returned, OR the exception thrown by the callback,
    /// OR <c>null</c> if the callback completed successfully and reported no error.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerStepThrough]
    public static async Task<Exception?> Async(
        Func<Task<Exception?>> callback)
    {
        try
        {
            var maybeError = await callback
                .Invoke()
                .ConfigureAwait(false);
            return maybeError;
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    /// <summary>
    /// Executes a callback that returns <see cref="TriedEx{T}"/> and forwards any
    /// thrown exception through the logger. Built on <see cref="Safely.GetResultOrExceptionAsync{T}(System.Func{System.Threading.Tasks.Task{NexusLabs.Framework.TriedEx{T}}}, System.Func{System.Exception, System.Threading.Tasks.Task}?)"/>.
    /// </summary>
    /// <typeparam name="T">The result type.</typeparam>
    /// <param name="logger">The logger to use.</param>
    /// <param name="callback">The callback to execute.</param>
    /// <param name="caller">
    /// The name of the calling method. Leave as null to automatically use the name
    /// of the calling method via <see cref="CallerMemberNameAttribute"/>.
    /// </param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerStepThrough]
    public static async Task<TriedEx<T>> GetAsync<T>(
        ILogger logger,
        Func<Task<TriedEx<T>>> callback,
        [CallerMemberName] string? caller = null)
    {
        var result = await Safely
            .GetResultOrExceptionAsync<T>(
                [DebuggerStepThrough] async () =>
                {
                    var inner = await callback
                        .Invoke()
                        .ConfigureAwait(false);
                    return inner;
                },
                ex =>
                {
                    LoggingAndTelemetry(logger, caller, ex);

                    return Task.CompletedTask;
                })
            .ConfigureAwait(false);
        return result;
    }

    /// <summary>
    /// Executes a callback that returns <see cref="TriedEx{T}"/> without logging.
    /// </summary>
    /// <typeparam name="T">The result type.</typeparam>
    /// <param name="callback">The callback to execute.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerStepThrough]
    public static async Task<TriedEx<T>> GetAsync<T>(
        Func<Task<TriedEx<T>>> callback)
    {
        var result = await Safely
            .GetResultOrExceptionAsync<T>(
                [DebuggerStepThrough] async () =>
                {
                    var inner = await callback
                        .Invoke()
                        .ConfigureAwait(false);
                    return inner;
                })
            .ConfigureAwait(false);
        return result;
    }

    /// <summary>
    /// Executes a synchronous callback that returns <see cref="TriedEx{T}"/> without logging.
    /// </summary>
    /// <typeparam name="T">The result type.</typeparam>
    /// <param name="callback">The callback to execute.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerStepThrough]
    public static TriedEx<T> Get<T>(
        Func<TriedEx<T>> callback)
    {
        var result = Safely.GetResultOrException(
            [DebuggerStepThrough] () =>
            {
                var inner = callback.Invoke();
                return inner;
            });
        return result;
    }

    /// <summary>
    /// Executes a synchronous callback that returns <see cref="TriedEx{T}"/> and logs
    /// any thrown exception via <paramref name="logger"/>.
    /// </summary>
    /// <typeparam name="T">The result type.</typeparam>
    /// <param name="logger">The logger to use.</param>
    /// <param name="callback">The callback to execute.</param>
    /// <param name="caller">
    /// The name of the calling method. Leave as null to automatically use the name
    /// of the calling method via <see cref="CallerMemberNameAttribute"/>.
    /// </param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerStepThrough]
    public static TriedEx<T> Get<T>(
        ILogger logger,
        Func<TriedEx<T>> callback,
        [CallerMemberName] string? caller = null)
    {
        var result = Safely.GetResultOrException(
            [DebuggerStepThrough] () =>
            {
                var inner = callback.Invoke();
                return inner;
            },
            ex =>
            {
                LoggingAndTelemetry(logger, caller, ex);
            });
        return result;
    }

    /// <summary>
    /// Executes a callback that returns <see cref="TriedNullEx{T}"/> and forwards any
    /// thrown exception through the logger.
    /// </summary>
    /// <typeparam name="T">The (nullable) result type.</typeparam>
    /// <param name="logger">The logger to use.</param>
    /// <param name="callback">The callback to execute.</param>
    /// <param name="caller">
    /// The name of the calling method. Leave as null to automatically use the name
    /// of the calling method via <see cref="CallerMemberNameAttribute"/>.
    /// </param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerStepThrough]
    public static async Task<TriedNullEx<T?>> GetOrNullAsync<T>(
        ILogger logger,
        Func<Task<TriedNullEx<T?>>> callback,
        [CallerMemberName] string? caller = null)
    {
        var result = await Safely
            .GetResultNullOrExceptionAsync<T>(
                [DebuggerStepThrough] async () =>
                {
                    var inner = await callback
                        .Invoke()
                        .ConfigureAwait(false);
                    return inner;
                },
                ex =>
                {
                    LoggingAndTelemetry(logger, caller, ex);
                    return Task.CompletedTask;
                })
            .ConfigureAwait(false);
        return result;
    }

    /// <summary>
    /// Executes a callback that returns <see cref="TriedNullEx{T}"/> without logging.
    /// </summary>
    /// <typeparam name="T">The (nullable) result type.</typeparam>
    /// <param name="callback">The callback to execute.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerStepThrough]
    public static async Task<TriedNullEx<T?>> GetOrNullAsync<T>(
        Func<Task<TriedNullEx<T?>>> callback)
    {
        var result = await Safely
            .GetResultNullOrExceptionAsync<T>(
                [DebuggerStepThrough] async () =>
                {
                    var inner = await callback
                        .Invoke()
                        .ConfigureAwait(false);
                    return inner;
                })
            .ConfigureAwait(false);
        return result;
    }

    /// <summary>
    /// Awaits the callback to completion, treating cooperative cancellation as a
    /// successful "did not complete" result rather than a failure.
    /// </summary>
    /// <param name="asyncCallback">The callback to await.</param>
    /// <returns><c>true</c> if the callback completed; <c>false</c> if cancelled.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [DebuggerStepThrough]
    public static async Task<bool> ToCompletionOrCanceledAsync(
        Func<Task> asyncCallback)
    {
        try
        {
            await asyncCallback
                .Invoke()
                .ConfigureAwait(false);
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    /// <summary>
    /// Combines two <see cref="TriedEx{T}"/> error states into a single exception. Throws
    /// if both inputs are successful (no error to combine). When one is successful, returns
    /// the other's error; when both failed, returns an <see cref="AggregateException"/>.
    /// </summary>
    public static Exception CombineErrors<T1, T2>(
        TriedEx<T1> triedEx1,
        TriedEx<T2> triedEx2)
    {
        if (triedEx1.Success && triedEx2.Success)
        {
            throw new ArgumentException(
                "Both TriedEx<T> results were successful. Cannot combine errors.");
        }

        if (!triedEx1.Success && !triedEx2.Success)
        {
            return new AggregateException(triedEx1.Error!, triedEx2.Error!);
        }

        return !triedEx1.Success
            ? triedEx1.Error
            : triedEx2.Error;
    }

    /// <summary>
    /// Combines a <see cref="TriedEx{T}"/> error with an optional secondary
    /// <see cref="Exception"/>. Throws if there is no error in either input.
    /// </summary>
    public static Exception CombineErrors<T1>(
        TriedEx<T1> triedEx1,
        Exception? maybeOther)
    {
        if (triedEx1.Success && maybeOther is null)
        {
            throw new ArgumentException(
                "Both TriedEx<T> results were successful. Cannot combine errors.");
        }

        if (!triedEx1.Success && maybeOther is not null)
        {
            return new AggregateException(triedEx1.Error!, maybeOther);
        }

        return !triedEx1.Success
            ? triedEx1.Error!
            : maybeOther!;
    }

    /// <summary>
    /// Combines two optional <see cref="Exception"/>s into one, returning <c>null</c>
    /// if both are null, the non-null one if exactly one is provided, or an
    /// <see cref="AggregateException"/> wrapping both when both are non-null.
    /// </summary>
    public static Exception? CombineErrors(
        Exception? exception,
        Exception? maybeOther)
    {
        if (exception is null && maybeOther is null)
        {
            return null;
        }

        if (exception is not null && maybeOther is not null)
        {
            return new AggregateException(exception, maybeOther);
        }

        return exception ?? maybeOther;
    }

    /// <summary>
    /// Combines two <see cref="TriedEx{T}"/> error states without throwing when both
    /// are successful. Returns <c>null</c> when there is nothing to combine.
    /// </summary>
    public static Exception? CombineErrorsIfNeeded<T1, T2>(
        TriedEx<T1> triedEx1,
        TriedEx<T2> triedEx2)
    {
        if (triedEx1.Success && triedEx2.Success)
        {
            return null;
        }

        if (!triedEx1.Success && !triedEx2.Success)
        {
            return new AggregateException(triedEx1.Error!, triedEx2.Error!);
        }

        return !triedEx1.Success
            ? triedEx1.Error
            : triedEx2.Error;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void LoggingAndTelemetry(
        ILogger logger,
        string? caller,
        Exception ex)
    {
        if (ex is OperationCanceledException)
        {
            logger.LogDebug(
                ex,
                "Operation cancelled while calling '{Caller}'.",
                caller);
        }
        else
        {
            logger.LogError(
                ex,
                "An exception was thrown calling '{Caller}'.",
                caller);
        }
    }
}
