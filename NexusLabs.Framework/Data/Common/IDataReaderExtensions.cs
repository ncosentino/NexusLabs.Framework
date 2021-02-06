namespace System.Data
{
    public static class IDataReaderExtensions
    {
        public static string GetNullableString(
            this IDataReader reader,
            int ordinal,
            Func<string> nullValueCallback)
        {
            var result = reader.IsDBNull(ordinal)
                ? (nullValueCallback == null ? null : nullValueCallback.Invoke())
                : reader.GetString(ordinal);
            return result;
        }

        public static string GetNullableString(
            this IDataReader reader,
            string name,
            Func<string> nullValueCallback) =>
            GetNullableString(reader, reader.GetOrdinal(name), nullValueCallback);

        public static string GetNullableString(
            this IDataReader reader,
            int ordinal) =>
            GetNullableString(reader, ordinal, null);

        public static string GetNullableString(
            this IDataReader reader,
            string name) =>
            GetNullableString(reader, name, null);
    }
}
