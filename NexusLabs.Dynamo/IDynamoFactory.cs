using System.Collections.Generic;
using System.Dynamic;

namespace NexusLabs.Dynamo
{
    public delegate bool TryGetDynamoMemberDelegate(
        GetMemberBinder binder,
        out object result);

    public interface IDynamoFactory
    {
        T Create<T>(IEnumerable<KeyValuePair<string, object>> members);

        T Create<T>(IReadOnlyDictionary<string, object> members);

        T Create<T>(IEnumerable<TryGetDynamoMemberDelegate> callbacks);

        T Create<T>(
            string memberName,
            object memberValue);

        T Create<T>(TryGetDynamoMemberDelegate callback);
    }
}