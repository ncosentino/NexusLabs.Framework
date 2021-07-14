using System.Collections;
using System.Collections.Generic;

namespace NexusLabs.Collections.Generic
{
    public sealed class FrozenHashSet<T> : IFrozenHashSet<T>
    {
        private readonly HashSet<T> _wrapped;

        public FrozenHashSet(IEnumerable<T> collection)
            : this (collection, null)
        {
        }

        public FrozenHashSet(
            IEnumerable<T> collection,
            IEqualityComparer<T> equalityComparer)
        {
            _wrapped = new HashSet<T>(collection, equalityComparer);
        }

        public int Count => _wrapped.Count;

        public IEnumerator<T> GetEnumerator() => _wrapped.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public bool Contains(T item) => _wrapped.Contains(item);
    }
}
