using System.Collections.Generic;

namespace NexusLabs.Dynamo
{
    public delegate void DynamoSetterDelegate(
        string memberName,
        object value);

    public delegate object DynamoGetterDelegate(string memberName);

    public interface IDynamoFactory
    {
        T Create<T>(IEnumerable<KeyValuePair<string, IDynamoProperty>> properties);

        T Create<T>(
            IEnumerable<KeyValuePair<string, IDynamoProperty>> properties = default,
            IEnumerable<KeyValuePair<string, DynamoGetterDelegate>> getters = default,
            IEnumerable<KeyValuePair<string, DynamoSetterDelegate>> setters = default);

        T Create<T>(
            IReadOnlyDictionary<string, IDynamoProperty> properties = default,
            IReadOnlyDictionary<string, DynamoGetterDelegate> getters = default,
            IReadOnlyDictionary<string, DynamoSetterDelegate> setters = default);
    }
}