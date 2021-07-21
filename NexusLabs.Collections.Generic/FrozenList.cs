using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace NexusLabs.Collections.Generic
{
    public sealed class FrozenList<T> : IFrozenList<T>
    {
        private static readonly Lazy<FrozenList<T>> EMPTY = new Lazy<FrozenList<T>>(() =>
            new FrozenList<T>(Enumerable.Empty<T>()));

		private readonly IReadOnlyList<T> _wrapped;

        public FrozenList(params T[] items)
            : this((IEnumerable<T>)items)
        {
        }

        public FrozenList(IEnumerable<T> items)
			: this(items.ToList())
		{
        }

        public FrozenList(IFrozenList<T> items)
			: this((IReadOnlyList<T>)items)
        {
        }

		private FrozenList(IReadOnlyList<T> willBeDirectlyAssigned)
        {
			_wrapped = willBeDirectlyAssigned;
        }

        public static IFrozenList<T> Empty => EMPTY.Value;

        public T this[int index] => _wrapped[index];

		public int Count => _wrapped.Count;

		public IEnumerator<T> GetEnumerator() => _wrapped.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public bool Contains(T item) => _wrapped.Contains(item);
    }
}
