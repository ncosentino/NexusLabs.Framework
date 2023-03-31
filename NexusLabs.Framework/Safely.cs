using System;
using System.Threading.Tasks;

namespace NexusLabs.Framework
{
    public static class Safely
    {
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
    }
}
