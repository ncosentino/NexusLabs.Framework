using System.Threading.Tasks;

namespace System.Data.Common
{
    public static class DbDataReaderExtensions
    {
        public static async Task<string> GetStringAsync(
            this DbDataReader reader,
            int ordinal,
            Func<string> nullValueCallback)
        {
            var result = await reader.IsDBNullAsync(ordinal)
                ? nullValueCallback.Invoke()
                : reader.GetString(ordinal);
            return result;
        }

        public static async Task<string> GetStringAsync(
            this DbDataReader reader,
            int ordinal) => await GetStringAsync(
                reader,
                ordinal,
                () => null);

        public static async Task<bool> GetBooleanAsync(
            this DbDataReader reader,
            int ordinal,
            Func<bool> nullValueCallback)
        {
            var result = await reader.IsDBNullAsync(ordinal)
                ? nullValueCallback.Invoke()
                : reader.GetBoolean(ordinal);
            return result;
        }

        public static async Task<bool> GetBooleanAsync(
            this DbDataReader reader,
            int ordinal) => await GetBooleanAsync(
                reader,
                ordinal,
                () => false);

        public static async Task<bool?> GetBooleanAsync(
            this DbDataReader reader,
            int ordinal,
            Func<bool?> nullValueCallback)
        {
            var result = await reader.IsDBNullAsync(ordinal)
                ? nullValueCallback.Invoke()
                : reader.GetBoolean(ordinal);
            return result;
        }

        public static async Task<bool?> GetBooleanOrNullAsync(
            this DbDataReader reader,
            int ordinal) => await GetBooleanAsync(
                reader,
                ordinal,
                () => null);

        public static async Task<int> GetInt32Async(
            this DbDataReader reader,
            int ordinal,
            Func<int> nullValueCallback)
        {
            var result = await reader.IsDBNullAsync(ordinal)
                ? nullValueCallback.Invoke()
                : reader.GetInt32(ordinal);
            return result;
        }

        public static async Task<int> GetInt32Async(
            this DbDataReader reader,
            int ordinal) => await GetInt32Async(
                reader,
                ordinal,
                () => 0);

        public static async Task<long> GetInt64Async(
            this DbDataReader reader,
            int ordinal,
            Func<long> nullValueCallback)
        {
            var result = await reader.IsDBNullAsync(ordinal)
                ? nullValueCallback.Invoke()
                : reader.GetInt64(ordinal);
            return result;
        }

        public static async Task<long> GetInt64Async(
            this DbDataReader reader,
            int ordinal) => await GetInt64Async(
                reader,
                ordinal,
                () => 0);

        public static async Task<double> GetDoubleAsync(
            this DbDataReader reader,
            int ordinal,
            Func<double> nullValueCallback)
        {
            var result = await reader.IsDBNullAsync(ordinal)
                ? nullValueCallback.Invoke()
                : reader.GetDouble(ordinal);
            return result;
        }

        public static async Task<double> GetDoubleAsync(
            this DbDataReader reader,
            int ordinal) => await GetDoubleAsync(
                reader,
                ordinal,
                () => 0d);

        public static async Task<T> GetEnumAsync<T>(
            this DbDataReader reader,
            int ordinal,
            Func<T> nullValueCallback)
            where T : struct
        {
            var result = await reader.IsDBNullAsync(ordinal)
                ? nullValueCallback.Invoke()
                : (T)Enum.ToObject(typeof(T), reader.GetInt32(ordinal));
            return result;
        }

        public static async Task<T> GetEnumAsync<T>(
            this DbDataReader reader,
            int ordinal)
            where T : struct => await GetEnumAsync<T>(
                reader,
                ordinal,
                () => default);
    }
}
