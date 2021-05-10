using System;
using System.Collections;
using System.Collections.Generic;
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

        public Cast()
        {
            _typeMethodInfoLookup = new Dictionary<Type, MethodInfo>();
            _typeConstructorInfoLookup = new Dictionary<Type, ConstructorInfo>();
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

                    const string KVP_PREFIX = "System.Collections.Generic.KeyValuePair`";
                    if (resultType.GenericTypeArguments.Single().FullName.StartsWith(
                        KVP_PREFIX,
                        StringComparison.Ordinal) &&
                        obj is IEnumerable &&
                        obj.GetType().GenericTypeArguments.Count() == 2)
                    {
                        wrapInEnumerable = KeyValuePairConversion(obj, resultType);
                    }

                    var enumerableResult = typeof(Enumerable)
                        .GetMethod("Cast")
                        .MakeGenericMethod(resultType.GenericTypeArguments[0])
                        .Invoke(null, new object[] { wrapInEnumerable });

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
                            .MakeGenericMethod(resultType.GenericTypeArguments[0])
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
            }
            catch (Exception ex)
            {
                throw new InvalidCastException(
                    $"Could not cast '{obj.GetType()}' to '{resultType}'.",
                    ex);
            }

            return CastInto(obj, resultType, useCache);
        }

        private IEnumerable KeyValuePairConversion(object obj, Type resultType)
        {
            var resultElementGenericType = resultType
                .GenericTypeArguments
                .Single();
            if (!_typeConstructorInfoLookup.TryGetValue(
                resultElementGenericType,
                out var kvpConstructor))
            {
                kvpConstructor = resultElementGenericType
                    .GetConstructors()
                    .Single();
                _typeConstructorInfoLookup[resultElementGenericType] = kvpConstructor;
            }

            foreach (var child in (IEnumerable)obj)
            {
                // FIXME: cache this because it's ridiculous
                var keyProperty = child
                    .GetType()
                    .GetProperty("Key", BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetProperty);
                var valueProperty = child
                    .GetType()
                    .GetProperty("Value", BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetProperty);

                var element = kvpConstructor.Invoke(new[] { keyProperty.GetValue(child), valueProperty.GetValue(child) });
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
