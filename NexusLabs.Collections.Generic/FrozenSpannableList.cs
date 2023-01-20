using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace NexusLabs.Collections.Generic
{
#if NET6_0_OR_GREATER
    public sealed class FrozenSpannableList<T> : IFrozenSpannableList<T>
    {
        private static readonly Lazy<IFrozenSpannableList<T>> EMPTY = new Lazy<IFrozenSpannableList<T>>(() =>
            new FrozenSpannableList<T>());

        private readonly IFrozenSpannableList<T> _wrapped;

        public FrozenSpannableList()
            : this(Array.Empty<T>())
        {
        }

        public FrozenSpannableList(T item, params T[] otherItems)
            : this(item.Yield().Concat(otherItems ?? Array.Empty<T>()))
        {
        }

        public FrozenSpannableList(IEnumerable<T> items)
        {
            items.GetAsOrCreateFrozenSpannableList(out _wrapped);
        }

        public static IFrozenSpannableList<T> Empty => EMPTY.Value;

        public T this[int index] => _wrapped[index];

        public int Count => _wrapped.Count;

        public IEnumerator<T> GetEnumerator() => _wrapped.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public ReadOnlySpan<T> GetReadOnlySpan() => _wrapped.GetReadOnlySpan();

        public bool Contains(T item) => _wrapped.Contains(item);
    }
#endif
}
