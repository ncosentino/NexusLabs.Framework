using NexusLabs.Collections.Generic;

using System.Collections.Generic;

namespace System.Linq;

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
        Random random)
        => EnumerableSelector.RandomOrDefault(
            source,
            random);

    public static T Random<T>(
        this IEnumerable<T> source,
        Random random)
        => EnumerableSelector.Random(
            source,
            random);

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
