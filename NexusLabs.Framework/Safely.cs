using System;
using System.Threading.Tasks;

namespace NexusLabs.Framework
{
    public static class Safely
    {
        public static Tried<T> GetResultOrFalse<T>(Func<T> callback)
        {
            try
            {
                var result = callback();
                return result;
            }
            catch
            {
                return Tried<T>.Failed;
            }
        }

        public static async Task<Tried<T>> GetResultOrFalseAsync<T>(Func<Task<T>> callback)
        {
            try
            {
                var result = await
                    callback()
                    .ConfigureAwait(false);
                return result;
            }
            catch
            {
                return Tried<T>.Failed;
            }
        }

        public static TriedEx<T> GetResultOrException<T>(Func<T> callback)
        {
            try
            {
                var result = callback();
                return result;
            }
            catch (Exception ex)
            {
                return ex;
            }
        }

        public static async Task<TriedEx<T>> GetResultOrExceptionAsync<T>(Func<Task<T>> callback)
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
                return ex;
            }
        }
    }
}
