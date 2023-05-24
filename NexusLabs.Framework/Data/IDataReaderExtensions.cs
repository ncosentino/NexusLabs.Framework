using System.Collections.Generic;
using System.Linq;

namespace System.Data
{
    public static class IDataReaderExtensions
    {
        public static string? GetNullableString(
            this IDataReader reader,
            int ordinal,
            Func<string?>? nullValueCallback)
        {
            var result = reader.IsDBNull(ordinal)
                ? nullValueCallback == null ? null : nullValueCallback.Invoke()
                : reader.GetString(ordinal);
            return result;
        }

        public static string? GetNullableString(
            this IDataReader reader,
            string name,
            Func<string?>? nullValueCallback) =>
            reader.GetNullableString(reader.GetOrdinal(name), nullValueCallback);

        public static string? GetNullableString(
            this IDataReader reader,
            int ordinal) =>
            reader.GetNullableString(ordinal, null);

        public static string? GetNullableString(
            this IDataReader reader,
            string name) =>
            reader.GetNullableString(name, null);

        public static IReadOnlyDictionary<string, int> GetOrdinalColumnMapping(
            this IDataReader reader,
            IEqualityComparer<string>? columnNameComparer = null) =>
            Enumerable
                .Range(0, reader.FieldCount)
                .ToDictionary(
                    reader.GetName,
                    x => x,
                    columnNameComparer ?? StringComparer.Ordinal);
    }
}
