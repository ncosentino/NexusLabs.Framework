using System.Collections.Generic;

using NexusLabs.Collections.Generic;
using NexusLabs.Framework;

namespace System.Linq
{
    public static class IEnumerableExtensions
    {
        static IEnumerableExtensions()
        {
            EnumerableSelector = new EnumerableSelector();
        }

        public static EnumerableSelector EnumerableSelector { get; set; }

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

        public static T RandomOrDefault<T>(
            this IEnumerable<T> source,
            IRandom random)
            => EnumerableSelector.RandomOrDefault(
                source,
                random);

        public static T Random<T>(
            this IEnumerable<T> source,
            IRandom random)
            => EnumerableSelector.Random(
                source,
                random);

        public static T Random<T>(
            this IEnumerable<T> source,
            Random random)
            => Random(
                source,
                new NexusLabs.Framework.Random(random));

        public static T RandomOrDefault<T>(
            this IEnumerable<T> source,
            Random random)
            => RandomOrDefault(
                source,
                new NexusLabs.Framework.Random(random));

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
#if NETSTANDARD2_1_OR_GREATER || NET472_OR_GREATER || NETCOREAPP2_0_OR_GREATER
            IEnumerable<T> enumerable
#else
            this IEnumerable<T> enumerable
#endif
            )
        {
            return new HashSet<T>(enumerable);
        }

        public static HashSet<T> ToHashSet<T>(
#if NETSTANDARD2_1_OR_GREATER || NET472_OR_GREATER || NETCOREAPP2_0_OR_GREATER
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
    }
}
