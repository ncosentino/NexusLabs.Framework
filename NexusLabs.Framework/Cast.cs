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

            var castedObject = castIntoMethod.Invoke(null, new[] { obj });
            return castedObject;
        }

        private static T CastInto<T>(object obj)
        {
            return (T)obj;
            // return obj as T; // if you want to perform safe casting and you are ok with possible null values. Adding type constraint (e.g. where T : class) is needed to use it.
        }
    }
}
