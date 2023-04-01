using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace NexusLabs.Collections.Generic
{
    public sealed class FrozenDictionary<TKey, TValue> : 
        IFrozenDictionary<TKey, TValue>,
        IDictionary
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
            IReadOnlyDictionary<TKey, TValue> items,
            IEqualityComparer<TKey> equalityComparer)
            : this(equalityComparer == null
                ? items
                : items.ToDictionary(equalityComparer))
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

        internal FrozenDictionary(IReadOnlyDictionary<TKey, TValue> willBeDirectlyAssigned)
        {
            _wrapped = willBeDirectlyAssigned;
        }

        public static IFrozenDictionary<TKey, TValue> Empty => EMPTY.Value;

        public TValue this[TKey key] => _wrapped[key];

        public int Count => _wrapped.Count;

        public IEnumerable<TKey> Keys => _wrapped.Keys;

        public IEnumerable<TValue> Values => _wrapped.Values;

        public bool IsFixedSize => true;

        public bool IsReadOnly => true;

        ICollection IDictionary.Keys => Keys.ToArray();

        ICollection IDictionary.Values => Values.ToArray();

        public bool IsSynchronized => true;

        public object SyncRoot => _wrapped;

        public object this[object key] 
        {
            get
            {
                if (key == null)
                {
                    throw new ArgumentNullException(nameof(key));
                }

                if (!(key is TKey castedKey))
                {
                    throw new ArgumentException(
                        $"The type of '{nameof(key)}' was '{key.GetType()}' " +
                        $"but is expected to be '{typeof(TKey)}'.");
                }

                return this[castedKey];
            }
            set => throw new NotSupportedException("This collection is read-only.");
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => _wrapped.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public bool ContainsKey(TKey key) => _wrapped.ContainsKey(key);

        public bool TryGetValue(TKey key, out TValue value) =>
            _wrapped.TryGetValue(key, out value);

        public void Add(object key, object value)
            => throw new NotSupportedException("This collection is read-only.");

        public void Clear()
            => throw new NotSupportedException("This collection is read-only.");

        public bool Contains(object key)
        {
            if (!(key is TKey castedKey))
            {
                throw new ArgumentException(
                    $"The type of '{nameof(key)}' was '{key.GetType()}' " +
                    $"but is expected to be '{typeof(TKey)}'.");
            }

            return Contains(castedKey);
        }

        IDictionaryEnumerator IDictionary.GetEnumerator()
            => new GenericKvpDictionaryEnumerator<TKey, TValue>(GetEnumerator());

        public void Remove(object key)
            => throw new NotSupportedException("This collection is read-only.");

        public void CopyTo(Array array, int index)
        {
            var i = index;
            foreach (var kvp in _wrapped)
            {
                array.SetValue(kvp, i);
                i++;
            }
        }
    }
}
