using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace NexusLabs.Collections.Generic
{
    internal sealed class FrozenArrayWrapper<T> :
#if NET6_0_OR_GREATER
        IFrozenSpannableList<T>
#else
        IFrozenList<T>
#endif
    {
        private readonly T[] _wrapped;

        internal FrozenArrayWrapper(T[] wrapped)
        {
            _wrapped = wrapped;
        }

        public T this[int index] => _wrapped[index];

        public int Count => _wrapped.Length;

#if NET6_0_OR_GREATER
            public ReadOnlySpan<T> GetReadOnlySpan() => _wrapped;
#endif

        public IEnumerator<T> GetEnumerator() =>
            _wrapped.Select(x => x).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() =>
            GetEnumerator();

        public bool Contains(T item) => _wrapped.Contains(item);
    }
}
