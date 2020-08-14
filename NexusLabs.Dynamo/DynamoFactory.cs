using System.Collections.Generic;

namespace NexusLabs.Dynamo
{
    public sealed class DynamoFactory : IDynamoFactory
    {
        public T Create<T>(string memberName, object memberValue) =>
            Create<T>(new[]
            {
                new KeyValuePair<string, object>(memberName, memberValue)
            });

        public T Create<T>(IReadOnlyDictionary<string, object> members) =>
            Create<T>((IEnumerable < KeyValuePair<string, object>>)members);

        public T Create<T>(IEnumerable<KeyValuePair<string, object>> members)
        {
            var dynamo = new Dynamo(members);
            var converted = Impromptu<T>(dynamo);
            return converted;
        }

        public T Create<T>(TryGetDynamoMemberDelegate callback) =>
            Create<T>(new[]
            {
                callback
            });

        public T Create<T>(IEnumerable<TryGetDynamoMemberDelegate> callbacks)
        {
            var dynamo = new Dynamo(callbacks);
            var converted = Impromptu<T>(dynamo);
            return converted;
        }

        private T Impromptu<T>(IReadOnlyDynamo dynamo)
        {
            var converted = (T)FixImpromptuBug.ActLike(dynamo, typeof(T));
            return converted;
        }
    }
}
