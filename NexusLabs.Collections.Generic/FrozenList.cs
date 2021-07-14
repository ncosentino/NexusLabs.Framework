using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace NexusLabs.Collections.Generic
{
    public sealed class FrozenList<T> : IFrozenList<T>
    {
		private readonly IReadOnlyList<T> _wrapped;

		public FrozenList(IEnumerable<T> collection)
			: this(collection.ToArray())
		{
        }

        public FrozenList(IFrozenList<T> wrapped)
			: this((IReadOnlyList<T>)wrapped)
        {
        }

		private FrozenList(IReadOnlyList<T> willBeDirectlyAssigned)
        {
			_wrapped = willBeDirectlyAssigned;
        }

        public T this[int index] => _wrapped[index];

		public int Count => _wrapped.Count;

		public IEnumerator<T> GetEnumerator() => _wrapped.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
