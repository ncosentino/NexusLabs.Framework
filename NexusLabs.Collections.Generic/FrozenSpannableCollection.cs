using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace NexusLabs.Collections.Generic
{
#if NET6_0_OR_GREATER
    public sealed class FrozenSpannableCollection<T> : IFrozenSpannableCollection<T>
    {
        private static readonly Lazy<IFrozenSpannableCollection<T>> EMPTY = new Lazy<IFrozenSpannableCollection<T>>(() =>
            new FrozenSpannableCollection<T>());

        private readonly IFrozenSpannableCollection<T> _wrapped;

        public FrozenSpannableCollection()
            : this(Array.Empty<T>())
        {
        }

        public FrozenSpannableCollection(T item, params T[] otherItems)
            : this(item.Yield().Concat(otherItems ?? Array.Empty<T>()))
        {
        }

        public FrozenSpannableCollection(IEnumerable<T> items)
        {
            items.GetAsOrCreateFrozenSpannableCollection(out _wrapped);
        }

        public static IFrozenSpannableCollection<T> Empty => EMPTY.Value;

        public int Count => _wrapped.Count;

        public IEnumerator<T> GetEnumerator() => _wrapped.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public ReadOnlySpan<T> GetReadOnlySpan() => _wrapped.GetReadOnlySpan();

        public bool Contains(T item) => _wrapped.Contains(item);
    }
#endif
}
