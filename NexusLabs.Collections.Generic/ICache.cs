using System.Collections.Generic;

namespace NexusLabs.Collections.Generic
{
    public interface ICache<TKey, TValue> : IReadOnlyCollection<TValue>
    {
        TValue this[TKey key] { get; }

        int Capacity { get; }

        void Add(TKey key, TValue value);

        bool ContainsKey(TKey key);

        bool TryGet(TKey key, out TValue value);
    }
}