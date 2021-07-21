using System.Collections.Generic;

namespace NexusLabs.Collections.Generic
{
    public interface IFrozenDictionary<TKey, TValue> : IReadOnlyDictionary<TKey, TValue>
	{
	}
}
