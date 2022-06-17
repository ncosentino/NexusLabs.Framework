using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace NexusLabs.Collections.Generic
{
    public sealed class FrozenCollection<T> : IFrozenCollection<T>
    {
        private static readonly Lazy<FrozenCollection<T>> EMPTY = new Lazy<FrozenCollection<T>>(() =>
            new FrozenCollection<T>(Enumerable.Empty<T>()));

        private readonly IReadOnlyCollection<T> _wrapped;

        public FrozenCollection(params T[] items)
            : this((IEnumerable<T>)items)
        {
        }

        public FrozenCollection(IEnumerable<T> items)
            : this(items.ToList())
        {
        }

        public FrozenCollection(IFrozenCollection<T> items)
            : this((IReadOnlyCollection<T>)items)
        {
        }

        internal FrozenCollection(IReadOnlyCollection<T> willBeDirectlyAssigned)
        {
            _wrapped = willBeDirectlyAssigned;
        }

        public static IFrozenCollection<T> Empty => EMPTY.Value;

        public int Count => _wrapped.Count;

        public IEnumerator<T> GetEnumerator() => _wrapped.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public bool Contains(T item) => _wrapped.Contains(item);
    }
}
