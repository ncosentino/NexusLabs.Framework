using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Castle.DynamicProxy;

using NexusLabs.Reflection;

namespace NexusLabs.Dynamo
{
    public sealed class DynamoFactory : IDynamoFactory
    {
        private readonly ProxyGenerator _proxyGenerator;
        private readonly ConcurrentDictionary<Type, IReadOnlyDictionary<string, DynamoGetterDelegate>> _defaultGetters;
        private readonly ConcurrentDictionary<Type, IReadOnlyDictionary<string, DynamoSetterDelegate>> _defaultSetters;
        private readonly ConcurrentDictionary<Type, IReadOnlyDictionary<string, DynamoInvokableDelegate>> _defaultMethods;

        public DynamoFactory()
        {
            _proxyGenerator = new ProxyGenerator();
            _defaultGetters = new ConcurrentDictionary<Type, IReadOnlyDictionary<string, DynamoGetterDelegate>>();
            _defaultSetters = new ConcurrentDictionary<Type, IReadOnlyDictionary<string, DynamoSetterDelegate>>();
            _defaultMethods = new ConcurrentDictionary<Type, IReadOnlyDictionary<string, DynamoInvokableDelegate>>();
        }

        public T Create<T>(IEnumerable<KeyValuePair<string, IDynamoProperty>> properties) =>
            Create<T>(properties, true);

        public T Create<T>(
            IEnumerable<KeyValuePair<string, IDynamoProperty>> properties,
            bool strict) =>
            Create<T>(properties: properties, strict: strict);

        public T Create<T>(
            IEnumerable<KeyValuePair<string, IDynamoProperty>> properties = default,
            IEnumerable<KeyValuePair<string, DynamoGetterDelegate>> getters = default,
            IEnumerable<KeyValuePair<string, DynamoSetterDelegate>> setters = default,
            IEnumerable<KeyValuePair<string, DynamoInvokableDelegate>> methods = default,
            bool strict = true)
        {
            properties = properties ?? new KeyValuePair<string, IDynamoProperty>[0];
            getters = getters ?? new KeyValuePair<string, DynamoGetterDelegate>[0];
            setters = setters ?? new KeyValuePair<string, DynamoSetterDelegate>[0];
            methods = methods ?? new KeyValuePair<string, DynamoInvokableDelegate>[0];

            getters = getters.Concat(properties.Select(x => new KeyValuePair<string, DynamoGetterDelegate>(x.Key, x.Value.Getter)));
            setters = setters.Concat(properties.Select(x => new KeyValuePair<string, DynamoSetterDelegate>(x.Key, x.Value.Setter)));

            if (typeof(T).IsInterface)
            {
                if (!strict)
                {
                    var looseDynamo = new Dynamo(
                        MergeWithDefaults(getters, typeof(T)),
                        MergeWithDefaults(setters, typeof(T)),
                        MergeWithDefaults(methods, typeof(T)));
                    var looseConverted = Impromptu<T>(looseDynamo);
                    return looseConverted;
                }

                var dynamo = new Dynamo(getters, setters, methods);
                var converted = Impromptu<T>(dynamo);
                return converted;
            }

            if (typeof(T).IsSealed)
            {
                if (typeof(T).IsAbstract)
                {
                    throw new NotSupportedException(
                        $"Cannot create instance of static class '{typeof(T)}'.");
                }

                throw new NotSupportedException(
                    $"Cannot create instance of sealed class '{typeof(T)}'.");
            }

            var interceptor = new DynamoMemberInterceptor(
                typeof(T),
                getters,
                setters,
                methods);

            object proxy;
            try
            {
                proxy = _proxyGenerator.CreateClassProxy(
                    typeof(T),
                    new ProxyGenerationOptions(),
                    interceptor);
            }
            catch (Exception ex)
            {
                throw new NotSupportedException(
                    $"Cannot create instance of class '{typeof(T)}'. See inner " +
                    $"exception for details.",
                    ex);
            }

            return (T)proxy;
        }

        public T Create<T>(
            IReadOnlyDictionary<string, IDynamoProperty> properties = default,
            IReadOnlyDictionary<string, DynamoGetterDelegate> getters = default,
            IReadOnlyDictionary<string, DynamoSetterDelegate> setters = default,
            IReadOnlyDictionary<string, DynamoInvokableDelegate> methods = default,
            bool strict = true) =>
            Create<T>(
                (IEnumerable<KeyValuePair<string, IDynamoProperty>>)properties,
                (IEnumerable<KeyValuePair<string, DynamoGetterDelegate>>)getters,
                (IEnumerable<KeyValuePair<string, DynamoSetterDelegate>>)setters,
                (IEnumerable<KeyValuePair<string, DynamoInvokableDelegate>>)methods,
                strict);

        private T Impromptu<T>(IReadOnlyDynamo dynamo)
        {
            var converted = (T)FixImpromptuBug.ActLike(dynamo, typeof(T));
            return converted;
        }

        private IReadOnlyDictionary<string, DynamoGetterDelegate> GetOrCreateDefaultGetters(Type type)
        {
            if (_defaultGetters.TryGetValue(type, out var cachedGetters))
            {
                return cachedGetters;
            }

            var defaultGetters = new Dictionary<string, DynamoGetterDelegate>();
            foreach (var property in type.GetPublicProperties())
            {
                if (!property.CanRead)
                {
                    continue;
                }

                defaultGetters[property.Name] = _ => default;
            }

            _defaultGetters[type] = defaultGetters;
            return defaultGetters;
        }

        private IReadOnlyDictionary<string, DynamoInvokableDelegate> GetOrCreateDefaultMethods(Type type)
        {
            if (_defaultMethods.TryGetValue(type, out var cachedMethods))
            {
                return cachedMethods;
            }

            var defaultMethods = new Dictionary<string, DynamoInvokableDelegate>();
            foreach (var method in type.GetPublicMethods())
            {
                defaultMethods[method.Name] = (_, __) => default;
            }

            _defaultMethods[type] = defaultMethods;
            return defaultMethods;
        }

        private IReadOnlyDictionary<string, DynamoSetterDelegate> GetOrCreateDefaultSetters(Type type)
        {
            if (_defaultSetters.TryGetValue(type, out var cachedSetters))
            {
                return cachedSetters;
            }

            var defaultSetters = new Dictionary<string, DynamoSetterDelegate>();
            foreach (var property in type.GetPublicProperties())
            {
                if (!property.CanWrite)
                {
                    continue;
                }

                defaultSetters[property.Name] = (_, __) => { };
            }

            _defaultSetters[type] = defaultSetters;
            return defaultSetters;
        }

        private IEnumerable<KeyValuePair<string, DynamoGetterDelegate>> MergeWithDefaults(
            IEnumerable<KeyValuePair<string, DynamoGetterDelegate>> getters,
            Type type)
        {
            var lookup = getters.ToDictionary(x => x.Key, x => x.Value);
            var final = GetOrCreateDefaultGetters(type)
                .Select(x => new KeyValuePair<string, DynamoGetterDelegate>(
                    x.Key,
                    lookup.TryGetValue(x.Key, out var val)
                        ? val
                        : x.Value));
            return final;
        }

        private IEnumerable<KeyValuePair<string, DynamoSetterDelegate>> MergeWithDefaults(
            IEnumerable<KeyValuePair<string, DynamoSetterDelegate>> setters,
            Type type)
        {
            var lookup = setters.ToDictionary(x => x.Key, x => x.Value);
            var final = GetOrCreateDefaultSetters(type)
                .Select(x => new KeyValuePair<string, DynamoSetterDelegate>(
                    x.Key,
                    lookup.TryGetValue(x.Key, out var val)
                        ? val
                        : x.Value));
            return final;
        }

        private IEnumerable<KeyValuePair<string, DynamoInvokableDelegate>> MergeWithDefaults(
            IEnumerable<KeyValuePair<string, DynamoInvokableDelegate>> methods,
            Type type)
        {
            var lookup = methods.ToDictionary(x => x.Key, x => x.Value);
            var final = GetOrCreateDefaultMethods(type)
                .Select(x => new KeyValuePair<string, DynamoInvokableDelegate>(
                    x.Key,
                    lookup.TryGetValue(x.Key, out var val)
                        ? val
                        : x.Value));
            return final;
        }
    }
}
