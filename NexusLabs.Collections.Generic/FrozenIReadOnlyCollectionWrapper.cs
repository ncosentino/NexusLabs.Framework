using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace NexusLabs.Collections.Generic
{
    internal sealed class FrozenIReadOnlyCollectionWrapper<T> : IFrozenCollection<T>
    {
        private readonly IReadOnlyCollection<T> _wrapped;

        internal FrozenIReadOnlyCollectionWrapper(IReadOnlyCollection<T> wrapped)
        {
            _wrapped = wrapped;
        }

        public int Count => _wrapped.Count;

        public IEnumerator<T> GetEnumerator() =>
            _wrapped.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() =>
            GetEnumerator();

        public bool Contains(T item) => _wrapped.Contains(item);
    }
}
