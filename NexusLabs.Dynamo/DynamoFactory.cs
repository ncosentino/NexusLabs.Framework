﻿using System.Collections.Generic;
using System.Linq;

namespace NexusLabs.Dynamo
{
    public sealed class DynamoFactory : IDynamoFactory
    {
        public T Create<T>(IEnumerable<KeyValuePair<string, IDynamoProperty>> properties) =>
            Create<T>(properties: properties);

        public T Create<T>(
            IEnumerable<KeyValuePair<string, IDynamoProperty>> properties = default,
            IEnumerable<KeyValuePair<string, DynamoGetterDelegate>> getters = default,
            IEnumerable<KeyValuePair<string, DynamoSetterDelegate>> setters = default)
        {
            properties = properties ?? new KeyValuePair<string, IDynamoProperty>[0];
            getters = getters ?? new KeyValuePair<string, DynamoGetterDelegate>[0];
            setters = setters ?? new KeyValuePair<string, DynamoSetterDelegate>[0];

            getters = getters.Concat(properties.Select(x => new KeyValuePair<string, DynamoGetterDelegate>(x.Key, x.Value.Getter)));
            setters = setters.Concat(properties.Select(x => new KeyValuePair<string, DynamoSetterDelegate>(x.Key, x.Value.Setter)));

            var dynamo = new Dynamo(getters, setters);
            var converted = Impromptu<T>(dynamo);
            return converted;
        }

        public T Create<T>(
            IReadOnlyDictionary<string, IDynamoProperty> properties = default,
            IReadOnlyDictionary<string, DynamoGetterDelegate> getters = default,
            IReadOnlyDictionary<string, DynamoSetterDelegate> setters = default) =>
            Create<T>(
                (IEnumerable<KeyValuePair<string, IDynamoProperty>>)properties,
                (IEnumerable<KeyValuePair<string, DynamoGetterDelegate>>)getters,
                (IEnumerable<KeyValuePair<string, DynamoSetterDelegate>>)setters);

        private T Impromptu<T>(IReadOnlyDynamo dynamo)
        {
            var converted = (T)FixImpromptuBug.ActLike(dynamo, typeof(T));
            return converted;
        }
    }
}
