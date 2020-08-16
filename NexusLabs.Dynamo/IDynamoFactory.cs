using System.Collections.Generic;
using System.Dynamic;

namespace NexusLabs.Dynamo
{
    public delegate void DynamoSetterDelegate(
        string memberName,
        object value);

    public delegate object DynamoGetterDelegate(string memberName);

    public delegate bool TryGetDynamoMemberDelegate(
        GetMemberBinder binder,
        out DynamoGetterDelegate getter);

    public delegate bool TrySetDynamoMemberDelegate(
        SetMemberBinder binder,
        out DynamoSetterDelegate setter);

    public interface IDynamoFactory
    {
        T Create<T>(
            IEnumerable<KeyValuePair<string, DynamoGetterDelegate>> getters,
            IEnumerable<KeyValuePair<string, DynamoSetterDelegate>> setters);

        T Create<T>(
            IReadOnlyDictionary<string, DynamoGetterDelegate> getters,
            IReadOnlyDictionary<string, DynamoSetterDelegate> setters);

        T Create<T>(
            IEnumerable<TryGetDynamoMemberDelegate> getters,
            IEnumerable<TrySetDynamoMemberDelegate> setters);
    }
}