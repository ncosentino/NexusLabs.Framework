using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace NexusLabs.Framework
{
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
    }
}
