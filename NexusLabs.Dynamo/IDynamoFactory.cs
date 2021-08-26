using System.Collections.Generic;

namespace NexusLabs.Dynamo
{
    public delegate void DynamoSetterDelegate(
        string memberName,
        object value);

    public delegate object DynamoGetterDelegate(string memberName);

    public delegate object DynamoInvokableDelegate(string memberName, object[] args);

    public interface IDynamoFactory
    {
        T Create<T>(IEnumerable<KeyValuePair<string, IDynamoProperty>> properties);

        T Create<T>(
            IEnumerable<KeyValuePair<string, IDynamoProperty>> properties,
            bool strict);

        T Create<T>(
            IEnumerable<KeyValuePair<string, IDynamoProperty>> properties = default,
            IEnumerable<KeyValuePair<string, DynamoGetterDelegate>> getters = default,
            IEnumerable<KeyValuePair<string, DynamoSetterDelegate>> setters = default,
            IEnumerable<KeyValuePair<string, DynamoInvokableDelegate>> methods = default,
            bool strict = true);

        T Create<T>(
            IReadOnlyDictionary<string, IDynamoProperty> properties = default,
            IReadOnlyDictionary<string, DynamoGetterDelegate> getters = default,
            IReadOnlyDictionary<string, DynamoSetterDelegate> setters = default,
            IReadOnlyDictionary<string, DynamoInvokableDelegate> methods = default,
            bool strict = true);
    }
}