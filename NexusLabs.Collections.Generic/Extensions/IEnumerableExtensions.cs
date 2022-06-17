using System.Collections.Generic;

using NexusLabs.Collections.Generic;

namespace System.Linq
{
    public static class IEnumerableExtensions
    {
        public static IEnumerable<IReadOnlyList<T>> Batch<T>(
            this IEnumerable<T> source,
            int size)
        {
            T[] bucket = null;
            var count = 0;

            foreach (var item in source)
            {
                if (bucket == null)
                {
                    bucket = new T[size];
                }

                bucket[count++] = item;

                if (count != size)
                {
                    continue;
                }

                yield return bucket;

                bucket = null;
                count = 0;
            }

            // Return the last bucket with all remaining elements
            if (bucket != null && count > 0)
            {
                Array.Resize(ref bucket, count);
                yield return bucket;
            }
        }

        public static T Random<T>(
            this IEnumerable<T> source,
            Random random)
        {
            var current = default(T);
            var count = 0;
            foreach (var element in source)
            {
                count++;
                if (random.Next(count) == 0)
                {
                    current = element;
                }
            }

            if (count == 0)
            {
                throw new InvalidOperationException("Sequence was empty");
            }

            return current;
        }

        public static T RandomOrDefault<T>(
            this IEnumerable<T> source,
            Random random)
        {
            var current = default(T);
            var count = 0;
            foreach (var element in source)
            {
                count++;
                if (random.Next(count) == 0)
                {
                    current = element;
                }
            }

            if (count == 0)
            {
                return default(T);
            }

            return current;
        }

        public static void Foreach<T>(
            this IEnumerable<T> enumerable,
            Action<T> perItemCallback)
        {
            foreach (var item in enumerable)
            {
                perItemCallback(item);
            }
        }

        public static void Consume<T>(this IEnumerable<T> enumerable)
        {
            enumerable.Foreach(_ => { });
        }

        public static IEnumerable<T2> TakeTypes<T2>(this IEnumerable<object> enumerable)
        {
            return enumerable.Where(x => x is T2).Cast<T2>();
        }

        public static HashSet<T> ToHashSet<T>(
#if NETSTANDARD2_1 || NET472 || NET48
            IEnumerable<T> enumerable
#else
            this IEnumerable<T> enumerable
#endif
            )
        {
            return new HashSet<T>(enumerable);
        }

        public static HashSet<T> ToHashSet<T>(
#if NETSTANDARD2_1 || NET472 || NET48
            IEnumerable<T> enumerable,
#else
            this IEnumerable<T> enumerable,
#endif
            IEqualityComparer<T> comparer)
        {
            return new HashSet<T>(enumerable, comparer);
        }

        public static Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> enumerable) =>
            ToDictionary(enumerable, null);

        public static Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(
            this IEnumerable<KeyValuePair<TKey, TValue>> enumerable,
            IEqualityComparer<TKey> equalityComparer)
        {
            return enumerable.ToDictionary(
                x => x.Key,
                x => x.Value,
                equalityComparer);
        }

        public static IReadOnlyDictionary<TKey, TValue> ToReadOnlyDictionary<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> enumerable)
        {
            return enumerable.ToDictionary();
        }

        public static IReadOnlyDictionary<TKey, TValue> ToReadOnlyDictionary<TKey, TValue>(
            this IEnumerable<KeyValuePair<TKey, TValue>> enumerable,
            IEqualityComparer<TKey> equalityComparer)
        {
            return enumerable.ToDictionary(equalityComparer);
        }

        public static IEnumerable<T> Yield<T>(this T obj)
        {
            yield return obj;
        }

        public static T[] AsArray<T>(this T obj)
        {
            return obj.Yield().ToArray();
        }

        public static IEnumerable<T> Repeat<T>(this T obj, int times)
        {
            for (var i = 0; i < times; ++i)
            {
                yield return obj;
            }
        }

        public static IEnumerable<T> AppendSingle<T>(this IEnumerable<T> enumerable, T obj)
        {
            return enumerable.Concat(obj.Yield());
        }

        public static IReadOnlyCollection<T> ToReadOnlyCollection<T>(this IEnumerable<T> enumerable)
        {
            return enumerable.ToArray();
        }

        public static IReadOnlyList<T> ToReadOnlyList<T>(this IEnumerable<T> enumerable)
        {
            return enumerable.ToArray();
        }

        public static IEnumerable<TProperty> SingleOrDefault<T, TProperty>(
            this IEnumerable<T> enumerable,
            Func<T, IEnumerable<TProperty>> selector)
        {
            return SingleOrDefault(
                enumerable,
                _ => true,
                selector);
            ;
        }

        public static IEnumerable<TProperty> SingleOrDefault<T, TProperty>(
            this IEnumerable<T> enumerable,
            Func<T, bool> predicate,
            Func<T, IEnumerable<TProperty>> selector)
        {
            var item = enumerable.SingleOrDefault(predicate);
            return item == null
                ? Enumerable.Empty<TProperty>()
                : selector(item);
        }

        public static TResult Single<TResult>(this IEnumerable<object> enumerable)
        {
            return enumerable.TakeTypes<TResult>().Single();
        }

        public static IFrozenCollection<T> AsFrozenCollection<T>(this IEnumerable<T> enumerable)
        {
            // since it's frozen we can directly return it already
            if (enumerable is IFrozenCollection<T>)
            {
                return (IFrozenCollection<T>)enumerable;
            }

            var frozen = new FrozenCollection<T>(enumerable);
            return frozen;
        }

        public static IFrozenList<T> AsFrozenList<T>(this IEnumerable<T> enumerable)
        {
            // since it's frozen we can directly return it already
            if (enumerable is IFrozenList<T>)
            {
                return (IFrozenList<T>)enumerable;
            }

            var frozen = new FrozenList<T>(enumerable);
            return frozen;
        }

        public static IFrozenHashSet<T> AsFrozenHashSet<T>(this IEnumerable<T> enumerable) =>
            AsFrozenHashSet(enumerable, null);

        public static IFrozenHashSet<T> AsFrozenHashSet<T>(
            this IEnumerable<T> enumerable,
            IEqualityComparer<T> equalityComparer)
        {
            var frozen = new FrozenHashSet<T>(enumerable, equalityComparer);
            Enumerable.Empty<KeyValuePair<string, int>>().ToDictionary(x => x.Key, x => x.Value);
            return frozen;
        }

        public static IFrozenDictionary<TResultKey, TResultValue> AsFrozenDictionary<TSourceKey, TSourceValue, TResultKey, TResultValue>(
            this IEnumerable<KeyValuePair<TSourceKey, TSourceValue>> enumerable,
            Func<KeyValuePair<TSourceKey, TSourceValue>, TResultKey> keySelector,
            Func<KeyValuePair<TSourceKey, TSourceValue>, TResultValue> valueSelector) => AsFrozenDictionary(
                enumerable,
                keySelector,
                valueSelector,
                null);

        public static IFrozenDictionary<TResultKey, TResultValue> AsFrozenDictionary<TSourceKey, TSourceValue, TResultKey, TResultValue>(
            this IEnumerable<KeyValuePair<TSourceKey, TSourceValue>> enumerable,
            Func<KeyValuePair<TSourceKey, TSourceValue>, TResultKey> keySelector,
            Func<KeyValuePair<TSourceKey, TSourceValue>, TResultValue> valueSelector,
            IEqualityComparer<TResultKey> equalityComparer) => AsFrozenDictionary(
                enumerable.Select(kvp => new KeyValuePair<TResultKey, TResultValue>(
                    keySelector(kvp),
                    valueSelector(kvp))),
                equalityComparer);

        public static IFrozenDictionary<TKey, TValue> AsFrozenDictionary<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> enumerable) =>
            AsFrozenDictionary(enumerable, null);

        public static IFrozenDictionary<TKey, TValue> AsFrozenDictionary<TKey, TValue>(
            this IEnumerable<KeyValuePair<TKey, TValue>> enumerable,
            IEqualityComparer<TKey> equalityComparer)
        {
            var frozen = new FrozenDictionary<TKey, TValue>(enumerable, equalityComparer);
            return frozen;
        }
    }
}
