using System.Collections.Generic;

namespace NexusLabs.Dynamo
{
    public sealed class DynamoFactory : IDynamoFactory
    {
        public T Create<T>(
            IEnumerable<KeyValuePair<string, DynamoGetterDelegate>> getters,
            IEnumerable<KeyValuePair<string, DynamoSetterDelegate>> setters)
        {
            var dynamo = new Dynamo(getters, setters);
            var converted = Impromptu<T>(dynamo);
            return converted;
        }

        public T Create<T>(
            IReadOnlyDictionary<string, DynamoGetterDelegate> getters,
            IReadOnlyDictionary<string, DynamoSetterDelegate> setters) =>
            Create<T>(
                (IEnumerable<KeyValuePair<string, DynamoGetterDelegate>>)getters,
                (IEnumerable<KeyValuePair<string, DynamoSetterDelegate>>)setters);

        public T Create<T>(
            TryGetDynamoMemberDelegate getter,
            TrySetDynamoMemberDelegate setter) =>
            Create<T>(
                new[] { getter },
                new[] { setter });

        public T Create<T>(
            IEnumerable<TryGetDynamoMemberDelegate> getters,
            IEnumerable<TrySetDynamoMemberDelegate> setters)
        {
            var dynamo = new Dynamo(getters, setters);
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
