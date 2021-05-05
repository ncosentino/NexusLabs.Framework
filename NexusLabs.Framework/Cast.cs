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

        public Cast()
        {
            _typeMethodInfoLookup = new Dictionary<Type, MethodInfo>();
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
                    var enumerableResult = typeof(Enumerable)
                        .GetMethod("Cast")
                        .MakeGenericMethod(resultType.GenericTypeArguments[0])
                        .Invoke(null, new object[] { obj });

                    if (resultType.FullName.StartsWith("System.Collections.Generic.IReadOnlyCollection`") ||
                        resultType.GetInterfaces().Any(x =>
                        {
                            return x.FullName.StartsWith("System.Collections.Generic.IReadOnlyCollection`");
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
