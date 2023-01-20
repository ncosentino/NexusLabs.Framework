using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace NexusLabs.Collections.Generic
{
    internal sealed class FrozenListWrapper<T> :
#if NET6_0_OR_GREATER
        IFrozenSpannableList<T>
#else
        IFrozenList<T>
#endif
    {
        private readonly List<T> _wrapped;

        internal FrozenListWrapper(List<T> wrapped)
        {
            _wrapped = wrapped;
        }

        public int Count => _wrapped.Count;

        public T this[int index] => _wrapped[index];

#if NET6_0_OR_GREATER
        public ReadOnlySpan<T> GetReadOnlySpan() =>
                CollectionsMarshal.AsSpan(_wrapped);
#endif
        public IEnumerator<T> GetEnumerator() =>
            _wrapped.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() =>
            GetEnumerator();

        public bool Contains(T item) => _wrapped.Contains(item);
    }
}
