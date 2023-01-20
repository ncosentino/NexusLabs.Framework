using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace NexusLabs.Collections.Generic
{
    public sealed class FrozenCollection<T> : IFrozenCollection<T>
    {
        private static readonly Lazy<IFrozenCollection<T>> EMPTY = new Lazy<IFrozenCollection<T>>(() =>
            new FrozenCollection<T>());

        private readonly IFrozenCollection<T> _wrapped;

        public FrozenCollection()
            : this(Array.Empty<T>())
        {
        }

        public FrozenCollection(T item, params T[] otherItems)
            : this(item.Yield().Concat(otherItems ?? Array.Empty<T>()))
        {
        }

        public FrozenCollection(IEnumerable<T> items)
        {
            items.GetAsOrCreateFrozenCollection(out _wrapped);
        }

        public static IFrozenCollection<T> Empty => EMPTY.Value;

        public int Count => _wrapped.Count;

        public IEnumerator<T> GetEnumerator() => _wrapped.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public bool Contains(T item) => _wrapped.Contains(item);
    }
}
