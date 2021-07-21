using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace NexusLabs.Collections.Generic
{
    public sealed class FrozenHashSet<T> : IFrozenHashSet<T>
    {
        private static readonly Lazy<FrozenHashSet<T>> EMPTY = new Lazy<FrozenHashSet<T>>(() =>
          new FrozenHashSet<T>(Enumerable.Empty<T>()));

        private readonly HashSet<T> _wrapped;

        public FrozenHashSet(params T[] items)
           : this((IEnumerable<T>)items)
        {
        }

        public FrozenHashSet(IEnumerable<T> items)
            : this(items, null)
        {
        }

        public FrozenHashSet(
            IEnumerable<T> items,
            IEqualityComparer<T> equalityComparer)
        {
            _wrapped = new HashSet<T>(items, equalityComparer);
        }

        public static IFrozenHashSet<T> Empty => EMPTY.Value;

        public int Count => _wrapped.Count;

        public IEnumerator<T> GetEnumerator() => _wrapped.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public bool Contains(T item) => _wrapped.Contains(item);
    }
}
