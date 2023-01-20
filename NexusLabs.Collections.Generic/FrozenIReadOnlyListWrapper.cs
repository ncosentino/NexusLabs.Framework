using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace NexusLabs.Collections.Generic
{
    internal sealed class FrozenIReadOnlyListWrapper<T> : IFrozenList<T>
    {
        private readonly IReadOnlyList<T> _wrapped;

        internal FrozenIReadOnlyListWrapper(IReadOnlyList<T> wrapped)
        {
            _wrapped = wrapped;
        }

        public int Count => _wrapped.Count;

        public T this[int index] => _wrapped[index];

        public IEnumerator<T> GetEnumerator() =>
            _wrapped.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() =>
            GetEnumerator();

        public bool Contains(T item) => _wrapped.Contains(item);
    }
}
