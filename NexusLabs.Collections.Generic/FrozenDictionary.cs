using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace NexusLabs.Collections.Generic
{
    public sealed class FrozenDictionary<TKey, TValue> : IFrozenDictionary<TKey, TValue>
    {
        private static readonly Lazy<FrozenDictionary<TKey, TValue>> EMPTY = new Lazy<FrozenDictionary<TKey, TValue>>(() =>
            new FrozenDictionary<TKey, TValue>(Enumerable.Empty<KeyValuePair<TKey, TValue>>()));

        private readonly IReadOnlyDictionary<TKey, TValue> _wrapped;

        public FrozenDictionary(params KeyValuePair<TKey, TValue>[] items)
            : this((IEnumerable<KeyValuePair<TKey, TValue>>)items)
        {
        }

        public FrozenDictionary(IEnumerable<KeyValuePair<TKey, TValue>> items)
            : this(items, null)
        {
        }

        public FrozenDictionary(
            IEnumerable<KeyValuePair<TKey, TValue>> items,
            IEqualityComparer<TKey> equalityComparer)
            : this(items.ToDictionary(equalityComparer))
        {
        }

        public FrozenDictionary(IFrozenDictionary<TKey, TValue> items)
            : this((IReadOnlyDictionary<TKey, TValue>)items)
        {
        }

        private FrozenDictionary(IReadOnlyDictionary<TKey, TValue> willBeDirectlyAssigned)
        {
            _wrapped = willBeDirectlyAssigned;
        }

        public static IFrozenDictionary<TKey, TValue> Empty => EMPTY.Value;

        public TValue this[TKey key] => _wrapped[key];

        public int Count => _wrapped.Count;

        public IEnumerable<TKey> Keys => _wrapped.Keys;

        public IEnumerable<TValue> Values => _wrapped.Values;

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => _wrapped.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public bool ContainsKey(TKey key) => _wrapped.ContainsKey(key);

        public bool TryGetValue(TKey key, out TValue value) =>
            _wrapped.TryGetValue(key, out value);
    }
}
