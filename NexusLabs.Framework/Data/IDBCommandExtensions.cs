namespace System.Data
{
    public static class IDBCommandExtensions
    {
        public static IDbDataParameter CreateParameter<T>(this IDbCommand command, string name, T value)
        {
            var param = command.CreateParameter();
            param.ParameterName = name;
            param.Value = value;
            return param;
        }

        public static IDbDataParameter AddParameter<T>(this IDbCommand command, string name, T value)
        {
            var param = CreateParameter(command, name, value);
            command.Parameters.Add(param);
            return param;
        }
    }
}
