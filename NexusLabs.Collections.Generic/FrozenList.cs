using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace NexusLabs.Collections.Generic
{
    public sealed class FrozenList<T> : IFrozenList<T>    
    {
        private static readonly Lazy<FrozenList<T>> EMPTY = new Lazy<FrozenList<T>>(() =>
            new FrozenList<T>(Enumerable.Empty<T>()));

        private readonly IFrozenList<T> _wrapped;

        public FrozenList()
            : this(Array.Empty<T>())
        {
        }

        public FrozenList(T item, params T[] otherItems)
            : this(item.Yield().Concat(otherItems ?? Array.Empty<T>()))
        {
        }

        public FrozenList(IEnumerable<T> items)
        {
            items.GetAsOrCreateFrozenList(out _wrapped);
        }

        public static IFrozenList<T> Empty => EMPTY.Value;

        public T this[int index] => _wrapped[index];

		public int Count => _wrapped.Count;

		public IEnumerator<T> GetEnumerator() => _wrapped.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public bool Contains(T item) => _wrapped.Contains(item);
    }
}
