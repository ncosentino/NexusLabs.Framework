using System.Threading;
using System.Threading.Tasks;

namespace System.Data
{
    public static class IAsyncDbDataReaderExtensions
    {
        public static async Task<string?> GetStringAsync(
            this IAsyncDbDataReader reader,
            int ordinal,
            Func<string?> nullValueCallback,
            CancellationToken cancellationToken = default)
        {
            var result = await reader
                .IsDBNullAsync(
                    ordinal,
                    cancellationToken)
                .ConfigureAwait(false)
                ? nullValueCallback.Invoke()
                : reader.GetString(ordinal);
            return result;
        }

        public static async Task<string?> GetStringAsync(
            this IAsyncDbDataReader reader,
            int ordinal,
            CancellationToken cancellationToken = default) => await GetStringAsync(
                reader,
                ordinal,
                () => null,
                cancellationToken);

        public static async Task<bool> GetBooleanAsync(
            this IAsyncDbDataReader reader,
            int ordinal,
            Func<bool> nullValueCallback,
            CancellationToken cancellationToken = default)
        {
            var result = await reader
                .IsDBNullAsync(
                    ordinal,
                    cancellationToken)
                .ConfigureAwait(false)
                ? nullValueCallback.Invoke()
                : reader.GetBoolean(ordinal);
            return result;
        }

        public static async Task<bool> GetBooleanAsync(
            this IAsyncDbDataReader reader,
            int ordinal,
            CancellationToken cancellationToken = default) => await GetBooleanAsync(
                reader,
                ordinal,
                () => default(bool),
                cancellationToken);

        public static async Task<bool?> GetBooleanAsync(
            this IAsyncDbDataReader reader,
            int ordinal,
            Func<bool?> nullValueCallback,
            CancellationToken cancellationToken = default)
        {
            var result = await reader
                .IsDBNullAsync(
                    ordinal,
                    cancellationToken)
                .ConfigureAwait(false)
                ? nullValueCallback.Invoke()
                : reader.GetBoolean(ordinal);
            return result;
        }

        public static async Task<bool?> GetBooleanOrNullAsync(
            this IAsyncDbDataReader reader,
            int ordinal,
            CancellationToken cancellationToken = default) => await GetBooleanAsync(
                reader,
                ordinal,
                () => null,
                cancellationToken);

        public static async Task<int> GetInt32Async(
            this IAsyncDbDataReader reader,
            int ordinal,
            Func<int> nullValueCallback,
            CancellationToken cancellationToken = default)
        {
            var result = await reader
                .IsDBNullAsync(
                    ordinal,
                    cancellationToken)
                .ConfigureAwait(false)
                ? nullValueCallback.Invoke()
                : reader.GetInt32(ordinal);
            return result;
        }

        public static async Task<int> GetInt32Async(
            this IAsyncDbDataReader reader,
            int ordinal,
            CancellationToken cancellationToken = default) => await GetInt32Async(
                reader,
                ordinal,
                () => default(int),
                cancellationToken);

        public static async Task<int?> GetInt32Async(
            this IAsyncDbDataReader reader,
            int ordinal,
            Func<int?> nullValueCallback,
            CancellationToken cancellationToken = default)
        {
            var result = await reader
                .IsDBNullAsync(
                    ordinal,
                    cancellationToken)
                .ConfigureAwait(false)
                ? nullValueCallback.Invoke()
                : reader.GetInt32(ordinal);
            return result;
        }

        public static async Task<int?> GetInt32OrNullAsync(
            this IAsyncDbDataReader reader,
            int ordinal,
            CancellationToken cancellationToken = default) => await GetInt32Async(
                reader,
                ordinal,
                () => default(int?),
                cancellationToken);

        public static async Task<long> GetInt64Async(
            this IAsyncDbDataReader reader,
            int ordinal,
            Func<long> nullValueCallback,
            CancellationToken cancellationToken = default)
        {
            var result = await reader
                .IsDBNullAsync(
                    ordinal,
                    cancellationToken)
                .ConfigureAwait(false)
                ? nullValueCallback.Invoke()
                : reader.GetInt64(ordinal);
            return result;
        }

        public static async Task<long> GetInt64Async(
            this IAsyncDbDataReader reader,
            int ordinal,
            CancellationToken cancellationToken = default) => await GetInt64Async(
                reader,
                ordinal,
                () => default(long),
                cancellationToken);

        public static async Task<long?> GetInt64Async(
            this IAsyncDbDataReader reader,
            int ordinal,
            Func<long?> nullValueCallback,
            CancellationToken cancellationToken = default)
        {
            var result = await reader
                .IsDBNullAsync(
                    ordinal,
                    cancellationToken)
                .ConfigureAwait(false)
                ? nullValueCallback.Invoke()
                : reader.GetInt64(ordinal);
            return result;
        }

        public static async Task<long?> GetInt64OrNullAsync(
            this IAsyncDbDataReader reader,
            int ordinal, 
            CancellationToken cancellationToken = default) => await GetInt64Async(
                reader,
                ordinal,
                () => default(long?),
                cancellationToken);

        public static async Task<float> GetFloatAsync(
            this IAsyncDbDataReader reader,
            int ordinal,
            Func<float> nullValueCallback,
            CancellationToken cancellationToken = default)
        {
            var result = await reader
                .IsDBNullAsync(
                    ordinal,
                    cancellationToken)
                .ConfigureAwait(false)
                ? nullValueCallback.Invoke()
                : reader.GetFloat(ordinal);
            return result;
        }

        public static async Task<float> GetFloatAsync(
            this IAsyncDbDataReader reader,
            int ordinal, 
            CancellationToken cancellationToken = default) => await GetFloatAsync(
                reader,
                ordinal,
                () => default(float),
                cancellationToken);

        public static async Task<float?> GetFloatAsync(
            this IAsyncDbDataReader reader,
            int ordinal,
            Func<float?> nullValueCallback,
            CancellationToken cancellationToken = default)
        {
            var result = await reader.IsDBNullAsync(ordinal, cancellationToken)
                ? nullValueCallback.Invoke()
                : reader.GetFloat(ordinal);
            return result;
        }

        public static async Task<float?> GetFloatOrNullAsync(
            this IAsyncDbDataReader reader,
            int ordinal,
            CancellationToken cancellationToken = default) => await GetFloatAsync(
                reader,
                ordinal,
                () => default(float?),
                cancellationToken);

        public static async Task<double> GetDoubleAsync(
            this IAsyncDbDataReader reader,
            int ordinal,
            Func<double> nullValueCallback,
            CancellationToken cancellationToken = default)
        {
            var result = await reader
                .IsDBNullAsync(
                    ordinal, 
                    cancellationToken)
                .ConfigureAwait(false)
                ? nullValueCallback.Invoke()
                : reader.GetDouble(ordinal);
            return result;
        }

        public static async Task<double> GetDoubleAsync(
            this IAsyncDbDataReader reader,
            int ordinal,
            CancellationToken cancellationToken = default) => await GetDoubleAsync(
                reader,
                ordinal,
                () => default(double), 
                cancellationToken);

        public static async Task<double?> GetDoubleAsync(
            this IAsyncDbDataReader reader,
            int ordinal,
            Func<double?> nullValueCallback,
            CancellationToken cancellationToken = default)
        {
            var result = await reader
                .IsDBNullAsync(
                    ordinal,
                    cancellationToken)
                .ConfigureAwait(false)
                ? nullValueCallback.Invoke()
                : reader.GetDouble(ordinal);
            return result;
        }

        public static async Task<double?> GetDoubleOrNullAsync(
            this IAsyncDbDataReader reader,
            int ordinal,
            CancellationToken cancellationToken = default) => await GetDoubleAsync(
                reader,
                ordinal,
                () => default(double?),
                cancellationToken);

        public static async Task<DateTime> GetDateTimeAsync(
            this IAsyncDbDataReader reader,
            int ordinal,
            Func<DateTime> nullValueCallback,
            CancellationToken cancellationToken = default)
        {
            var result = await reader
                .IsDBNullAsync(
                    ordinal,
                    cancellationToken)
                .ConfigureAwait(false)
                ? nullValueCallback.Invoke()
                : reader.GetDateTime(ordinal);
            return result;
        }

        public static async Task<DateTime> GetDateTimeAsync(
            this IAsyncDbDataReader reader,
            int ordinal,
            CancellationToken cancellationToken = default) => await GetDateTimeAsync(
                reader,
                ordinal,
                () => default(DateTime),
                cancellationToken);

        public static async Task<DateTime?> GetDateTimeAsync(
            this IAsyncDbDataReader reader,
            int ordinal,
            Func<DateTime?> nullValueCallback,
            CancellationToken cancellationToken = default)
        {
            var result = await reader
                .IsDBNullAsync(
                    ordinal,
                    cancellationToken)
                .ConfigureAwait(false)
                ? nullValueCallback.Invoke()
                : reader.GetDateTime(ordinal);
            return result;
        }

        public static async Task<DateTime?> GetDateTimeOrNullAsync(
            this IAsyncDbDataReader reader,
            int ordinal, 
            CancellationToken cancellationToken) => await GetDateTimeAsync(
                reader,
                ordinal,
                () => default(DateTime?), 
                cancellationToken);

        public static async Task<T> GetEnumAsync<T>(
            this IAsyncDbDataReader reader,
            int ordinal,
            Func<T> nullValueCallback,
            CancellationToken cancellationToken = default)
            where T : struct
        {
            var result = await reader
                .IsDBNullAsync(
                    ordinal,
                    cancellationToken)
                .ConfigureAwait(false)
                ? nullValueCallback.Invoke()
                : (T)Enum.ToObject(typeof(T), reader.GetInt32(ordinal));
            return result;
        }

        public static async Task<T> GetEnumAsync<T>(
            this IAsyncDbDataReader reader,
            int ordinal,
            CancellationToken cancellationToken = default)
            where T : struct => await GetEnumAsync<T>(
                reader,
                ordinal,
                () => default,
                cancellationToken);
    }
}
