using System.Threading.Tasks;

namespace System.Data.Common;

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
            () => default(bool));

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
            () => default(int));

    public static async Task<int?> GetInt32Async(
        this DbDataReader reader,
        int ordinal,
        Func<int?> nullValueCallback)
    {
        var result = await reader.IsDBNullAsync(ordinal)
            ? nullValueCallback.Invoke()
            : reader.GetInt32(ordinal);
        return result;
    }

    public static async Task<int?> GetInt32OrNullAsync(
        this DbDataReader reader,
        int ordinal) => await GetInt32Async(
            reader,
            ordinal,
            () => default(int?));

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
            () => default(long));

    public static async Task<long?> GetInt64Async(
        this DbDataReader reader,
        int ordinal,
        Func<long?> nullValueCallback)
    {
        var result = await reader.IsDBNullAsync(ordinal)
            ? nullValueCallback.Invoke()
            : reader.GetInt64(ordinal);
        return result;
    }

    public static async Task<long?> GetInt64OrNullAsync(
        this DbDataReader reader,
        int ordinal) => await GetInt64Async(
            reader,
            ordinal,
            () => default(long?));

    public static async Task<float> GetFloatAsync(
        this DbDataReader reader,
        int ordinal,
        Func<float> nullValueCallback)
    {
        var result = await reader.IsDBNullAsync(ordinal)
            ? nullValueCallback.Invoke()
            : reader.GetFloat(ordinal);
        return result;
    }

    public static async Task<float> GetFloatAsync(
        this DbDataReader reader,
        int ordinal) => await GetFloatAsync(
            reader,
            ordinal,
            () => default(float));

    public static async Task<float?> GetFloatAsync(
        this DbDataReader reader,
        int ordinal,
        Func<float?> nullValueCallback)
    {
        var result = await reader.IsDBNullAsync(ordinal)
            ? nullValueCallback.Invoke()
            : reader.GetFloat(ordinal);
        return result;
    }

    public static async Task<float?> GetFloatOrNullAsync(
        this DbDataReader reader,
        int ordinal) => await GetFloatAsync(
            reader,
            ordinal,
            () => default(float?));

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
            () => default(double));

    public static async Task<double?> GetDoubleAsync(
        this DbDataReader reader,
        int ordinal,
        Func<double?> nullValueCallback)
    {
        var result = await reader.IsDBNullAsync(ordinal)
            ? nullValueCallback.Invoke()
            : reader.GetDouble(ordinal);
        return result;
    }

    public static async Task<double?> GetDoubleOrNullAsync(
        this DbDataReader reader,
        int ordinal) => await GetDoubleAsync(
            reader,
            ordinal,
            () => default(double?));

    public static async Task<DateTime> GetDateTimeAsync(
        this DbDataReader reader,
        int ordinal,
        Func<DateTime> nullValueCallback)
    {
        var result = await reader.IsDBNullAsync(ordinal)
            ? nullValueCallback.Invoke()
            : reader.GetDateTime(ordinal);
        return result;
    }

    public static async Task<DateTime> GetDateTimeAsync(
        this DbDataReader reader,
        int ordinal) => await GetDateTimeAsync(
            reader,
            ordinal,
            () => default(DateTime));

    public static async Task<DateTime?> GetDateTimeAsync(
        this DbDataReader reader,
        int ordinal,
        Func<DateTime?> nullValueCallback)
    {
        var result = await reader.IsDBNullAsync(ordinal)
            ? nullValueCallback.Invoke()
            : reader.GetDateTime(ordinal);
        return result;
    }

    public static async Task<DateTime?> GetDateTimeOrNullAsync(
        this DbDataReader reader,
        int ordinal) => await GetDateTimeAsync(
            reader,
            ordinal,
            () => default(DateTime?));

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
