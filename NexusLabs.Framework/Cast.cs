using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace NexusLabs.Framework
{
    public sealed class Cast : ICast
    {
        private static Lazy<MethodInfo> _lazyCastIntoMethodInfo = new Lazy<MethodInfo>(
            () => typeof(Cast).GetMethod(
                nameof(Cast.CastInto),
                BindingFlags.NonPublic | BindingFlags.Static));

        private static MethodInfo CastIntoMethodInfo => _lazyCastIntoMethodInfo.Value;

        private readonly Dictionary<Type, MethodInfo> _typeMethodInfoLookup;
        private readonly Dictionary<Type, ConstructorInfo> _typeConstructorInfoLookup;
        private readonly Dictionary<Type, PropertyInfo> _kvpKeyPropertyLookup;
        private readonly Dictionary<Type, PropertyInfo> _kvpValuePropertyLookup;

        public Cast()
        {
            _typeMethodInfoLookup = new Dictionary<Type, MethodInfo>();
            _typeConstructorInfoLookup = new Dictionary<Type, ConstructorInfo>();
            _kvpKeyPropertyLookup = new Dictionary<Type, PropertyInfo>();
            _kvpValuePropertyLookup = new Dictionary<Type, PropertyInfo>();
        }

        public T ToType<T>(
            object obj,
            bool useCache) =>
            (T)ToType(obj, typeof(T), useCache);

        public T ToType<T>(object obj) =>
            ToType<T>(obj, true);

        public object ToType(
            object obj,
            Type resultType) =>
            ToType(obj, resultType, true);

        public object ToType(
            object obj,
            Type resultType,
            bool useCache)
        {
            if (obj == null)
            {
                return null;
            }

            if (resultType.IsAssignableFrom(obj.GetType()))
            {
                return obj;
            }

            try
            {
                if (typeof(IEnumerable).IsAssignableFrom(resultType) &&
                    resultType.IsGenericType)
                {
                    var wrapInEnumerable = obj;
                    Type genericType = resultType.GenericTypeArguments[0];

                    if (TryGetEnumerableKvpType(resultType, out var enumerableKvpType) &&
                        obj is IEnumerable &&
                        obj.GetType().GenericTypeArguments.Count() == 2)
                    {
                        var kvpType = enumerableKvpType
                            .GenericTypeArguments
                            .Single();
                        wrapInEnumerable = KeyValuePairConversion(obj, kvpType);
                        genericType = kvpType;
                    }

                    var enumerableResult = typeof(Enumerable)
                        .GetMethod("Cast")
                        .MakeGenericMethod(genericType)
                        .Invoke(null, new object[] { wrapInEnumerable });

                    if (TryHandleDictionary(
                        (IEnumerable)enumerableResult,
                        resultType,
                        enumerableKvpType,
                        out var dictionary))
                    {
                        return dictionary;
                    }

                    const string READONLY_COLLECTION_PREFIX = "System.Collections.Generic.IReadOnlyCollection`";
                    if (resultType.FullName.StartsWith(
                        READONLY_COLLECTION_PREFIX,
                        StringComparison.Ordinal) ||
                        resultType.GetInterfaces().Any(x =>
                        {
                            return x.FullName.StartsWith(
                                READONLY_COLLECTION_PREFIX,
                                StringComparison.Ordinal);
                        }))
                    {
                        enumerableResult = typeof(Enumerable)
                            .GetMethod("ToArray")
                            .MakeGenericMethod(genericType)
                            .Invoke(null, new object[] { enumerableResult });
                    }

                    return enumerableResult;
                }

                if (resultType.IsArray)
                {
                    var enumerableResult = typeof(Enumerable)
                        .GetMethod("Cast")
                        .MakeGenericMethod(resultType.GetElementType())
                        .Invoke(null, new object[] { obj });
                    var arrayResult = typeof(Enumerable)
                        .GetMethod("ToArray")
                        .MakeGenericMethod(resultType.GetElementType())
                        .Invoke(null, new object[] { enumerableResult });
                    return arrayResult;
                }

                if (TryHandleLongValue(
                    obj,
                    resultType,
                    out var longResult))
                {
                    return longResult;
                }

                if (resultType == typeof(TimeSpan) && obj is string rawTimeSpan)
                {
                    var timeSpanResult = TimeSpan.Parse(rawTimeSpan, CultureInfo.InvariantCulture);
                    return timeSpanResult;
                }
            }
            catch (Exception ex)
            {
                throw new InvalidCastException(
                    $"Could not cast '{obj.GetType()}' to '{resultType}'.",
                    ex);
            }

            return CastInto(obj, resultType, useCache);
        }

        private bool TryGetEnumerableKvpType(
            Type resultType,
            out Type kvpType)
        {
            const string KVP_PREFIX = "System.Collections.Generic.KeyValuePair`";
            kvpType = resultType
                .GetInterfaces()
                .Concat(new[] { resultType })
                .FirstOrDefault(x =>
                {
                    return
                        x.GenericTypeArguments.Length == 1 &&
                        x.GenericTypeArguments.Single().FullName.StartsWith(
                            KVP_PREFIX,
                            StringComparison.Ordinal);
                });
            return kvpType != null;
        }

        private bool TryHandleDictionary(
            IEnumerable enumerableToConvert,
            Type resultType,
            Type enumerableKvpType,
            out IDictionary dictionary)
        {
            dictionary = null;

            const string DICTIONARY_PREFIX = "System.Collections.Generic.IDictionary`";
            const string READONLY_DICTIONARY_PREFIX = "System.Collections.Generic.IReadOnlyDictionary`";
            if (!resultType.GetInterfaces().Concat(new[] { resultType }).Any(x =>
                x.FullName.StartsWith(
                    DICTIONARY_PREFIX,
                    StringComparison.Ordinal) ||
                x.FullName.StartsWith(
                    READONLY_DICTIONARY_PREFIX,
                    StringComparison.Ordinal)))
            {
                return false;
            }

            var dictType = typeof(Dictionary<,>);
            var kvpType = enumerableKvpType
                .GenericTypeArguments
                .Single();
            dictType = dictType.MakeGenericType(
                kvpType.GetGenericArguments()[0],
                kvpType.GetGenericArguments()[1]);

            var childKvpType = enumerableToConvert
                .GetType()
                .GenericTypeArguments
                .Single();

            GetKeyValueProperties(
                childKvpType,
                out var keyProperty,
                out var valueProperty);

            dictionary = (IDictionary)Activator.CreateInstance(dictType);
            foreach (var child in enumerableToConvert)
            {
                dictionary[keyProperty.GetValue(child)] = valueProperty.GetValue(child);
            }

            return true;
        }

        private void GetKeyValueProperties(
            Type kvpType,
            out PropertyInfo keyProperty,
            out PropertyInfo valueProperty)
        {
            if (!_kvpKeyPropertyLookup.TryGetValue(
                kvpType,
                out keyProperty))
            {
                keyProperty = kvpType.GetProperty(
                    "Key",
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetProperty);
                _kvpKeyPropertyLookup[kvpType] = keyProperty;
            }

            if (!_kvpValuePropertyLookup.TryGetValue(
                kvpType,
                out valueProperty))
            {
                valueProperty = kvpType.GetProperty(
                    "Value",
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetProperty);
                _kvpValuePropertyLookup[kvpType] = valueProperty;
            }
        }

        private IEnumerable KeyValuePairConversion(object obj, Type kvpType)
        {
            if (!_typeConstructorInfoLookup.TryGetValue(
                kvpType,
                out var kvpConstructor))
            {
                kvpConstructor = kvpType
                    .GetConstructors()
                    .Single();
                _typeConstructorInfoLookup[kvpType] = kvpConstructor;
            }

            var targetKeyType = kvpType.GetGenericArguments()[0];
            var targetValueType = kvpType.GetGenericArguments()[1];
            PropertyInfo keyProperty = null;
            PropertyInfo valueProperty = null;
            foreach (var child in (IEnumerable)obj)
            {
                // we need to get the properties off our SOURCE, not the
                // destination KVP type so we pull it off the first element
                if (keyProperty == null)
                {
                    GetKeyValueProperties(
                        child.GetType(),
                        out keyProperty,
                        out valueProperty);
                }

                var element = kvpConstructor.Invoke(new[] 
                { 
                    ToType(keyProperty.GetValue(child), targetKeyType),
                    ToType(valueProperty.GetValue(child), targetValueType),
                });

                yield return element;
            }
        }

        private bool TryHandleLongValue(
            object obj,
            Type resultType,
            out object result)
        {
            result = null;
            if (!(obj is long))
            {
                return false;
            }

            if (resultType == typeof(ulong))
            {
                result =  Convert.ToUInt64(obj);
                return true;
            }

            if (resultType == typeof(uint))
            {
                result = Convert.ToUInt32(obj);
                return true;
            }

            if (resultType == typeof(ushort))
            {
                result = Convert.ToUInt16(obj);
                return true;
            }

            if (resultType == typeof(byte))
            {
                result = Convert.ToByte(obj);
                return true;
            }

            if (resultType == typeof(long))
            {
                result = obj;
                return true;
            }

            if (resultType == typeof(int))
            {
                result = Convert.ToInt32(obj);
                return true;
            }

            if (resultType == typeof(short))
            {
                result = Convert.ToInt16(obj);
                return true;
            }

            if (resultType == typeof(sbyte))
            {
                result = Convert.ToSByte(obj);
                return true;
            }

            return false;
        }

        private object CastInto<T>(T obj, Type resultType, bool useCache)
        {
            MethodInfo castIntoMethod;
            if (!useCache ||
                !_typeMethodInfoLookup.TryGetValue(
                    resultType,
                    out castIntoMethod))
            {
                castIntoMethod = CastIntoMethodInfo.MakeGenericMethod(resultType);

                if (useCache)
                {
                    _typeMethodInfoLookup[resultType] = castIntoMethod;
                }
            }

            try
            {
                var castedObject = castIntoMethod.Invoke(null, new object[] { obj });
                return castedObject;
            }
            catch (Exception ex)
            {
                throw new InvalidCastException(
                    $"Could not cast '{obj.GetType()}' to '{resultType}'.",
                    ex);
            }
        }

        private static T CastInto<T>(object obj)
        {
            return (T)obj;
            // return obj as T; // if you want to perform safe casting and you are ok with possible null values. Adding type constraint (e.g. where T : class) is needed to use it.
        }
    }
}
