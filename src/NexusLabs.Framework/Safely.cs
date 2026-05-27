using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace NexusLabs.Framework;

// NLF0006 / NLF0009 protect consumers from hand-rolling try/catch wrappers
// instead of using the Try/Safely helpers. This file IS one of those helpers —
// every method here is the canonical try-catch shape the analyzer recommends
// callers replace with Safely/Try. The consumer-facing protection still
// applies everywhere else.
#pragma warning disable NLF0006 // Method wraps body in try-catch (use Safely/Try helpers)
#pragma warning disable NLF0009 // Method returns TriedEx/TriedNullEx without Try.GetAsync wrapper

public static class Safely
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Tried<T> GetResultOrFalse<T>(
        Func<T> callback,
        Action<Exception>? errorCallback = null)
    {
        try
        {
            var result = callback();
            if (result is null)
            {
                errorCallback?.Invoke(CreateNullCallbackException());
                return Tried<T>.Failed;
            }
            return result;
        }
        catch (Exception ex)
        {
            errorCallback?.Invoke(ex);
            return Tried<T>.Failed;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Tried<T> GetResultOrFalse<T>(
        Func<Tried<T>> callback,
        Action<Exception>? errorCallback = null)
    {
        try
        {
            var result = callback();
            return result;
        }
        catch (Exception ex)
        {
            errorCallback?.Invoke(ex);
            return Tried<T>.Failed;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static async Task<Tried<T>> GetResultOrFalseAsync<T>(
        Func<Task<T>> callback,
        Func<Exception, Task>? errorCallback = null)
    {
        try
        {
            var result = await
                callback()
                .ConfigureAwait(false);
            if (result is null)
            {
                if (errorCallback != null)
                {
                    await errorCallback
                        .Invoke(CreateNullCallbackException())
                        .ConfigureAwait(false);
                }
                return Tried<T>.Failed;
            }
            return result;
        }
        catch (Exception ex)
        {
            if (errorCallback != null)
            {
                await errorCallback
                    .Invoke(ex)
                    .ConfigureAwait(false);
            }

            return Tried<T>.Failed;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static async Task<Tried<T>> GetResultOrFalseAsync<T>(
        Func<Task<Tried<T>>> callback,
        Func<Exception, Task>? errorCallback = null)
    {
        try
        {
            var result = await
                callback()
                .ConfigureAwait(false);
            return result;
        }
        catch (Exception ex)
        {
            if (errorCallback != null)
            {
                await errorCallback
                    .Invoke(ex)
                    .ConfigureAwait(false);
            }

            return Tried<T>.Failed;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TriedEx<T> GetResultOrException<T>(
        Func<T> callback,
        Action<Exception>? errorCallback = null)
    {
        try
        {
            var result = callback();
            if (result is null)
            {
                var nullEx = CreateNullCallbackException();
                errorCallback?.Invoke(nullEx);
                return nullEx;
            }
            return result;
        }
        catch (Exception ex)
        {
            errorCallback?.Invoke(ex);
            return ex;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TriedEx<T> GetResultOrException<T>(
        Func<TriedEx<T>> callback,
        Action<Exception>? errorCallback = null)
    {
        try
        {
            var result = callback();
            return result;
        }
        catch (Exception ex)
        {
            errorCallback?.Invoke(ex);
            return ex;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static async Task<TriedEx<T>> GetResultOrExceptionAsync<T>(
        Func<Task<T>> callback,
        Func<Exception, Task>? errorCallback = null)
    {
        try
        {
            var result = await
                callback()
                .ConfigureAwait(false);
            if (result is null)
            {
                var nullEx = CreateNullCallbackException();
                if (errorCallback != null)
                {
                    await errorCallback
                        .Invoke(nullEx)
                        .ConfigureAwait(false);
                }
                return nullEx;
            }
            return result;
        }
        catch (Exception ex)
        {
            if (errorCallback != null)
            {
                await errorCallback
                    .Invoke(ex)
                    .ConfigureAwait(false);
            }

            return ex;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static async Task<TriedEx<T>> GetResultOrExceptionAsync<T>(
        Func<Task<TriedEx<T>>> callback,
        Func<Exception, Task>? errorCallback = null)
    {
        try
        {
            var result = await
                callback()
                .ConfigureAwait(false);
            return result;
        }
        catch (Exception ex)
        {
            if (errorCallback != null)
            {
                await errorCallback
                    .Invoke(ex)
                    .ConfigureAwait(false);
            }

            return ex;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static async Task<TriedNullEx<T?>> GetResultNullOrExceptionAsync<T>(
        Func<Task<T?>> callback,
        Func<Exception, Task>? errorCallback = null)
    {
        try
        {
            var result = await
                callback()
                .ConfigureAwait(false);
            return result;
        }
        catch (Exception ex)
        {
            if (errorCallback != null)
            {
                await errorCallback
                    .Invoke(ex)
                    .ConfigureAwait(false);
            }

            return ex;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static async Task<TriedNullEx<T?>> GetResultNullOrExceptionAsync<T>(
        Func<Task<TriedNullEx<T?>>> callback,
        Func<Exception, Task>? errorCallback = null)
    {
        try
        {
            var result = await
                callback()
                .ConfigureAwait(false);
            return result;
        }
        catch (Exception ex)
        {
            if (errorCallback != null)
            {
                await errorCallback
                    .Invoke(ex)
                    .ConfigureAwait(false);
            }

            return ex;
        }
    }

    private static InvalidOperationException CreateNullCallbackException() =>
        new(
            "Callback returned null. The non-nullable Safely.GetResult* overloads do not " +
            "permit null return values - use Safely.GetResultNullOrExceptionAsync if null " +
            "is a valid result for your callback.");
}
