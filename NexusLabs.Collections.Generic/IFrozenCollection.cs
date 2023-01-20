using System.Collections.Generic;

namespace NexusLabs.Collections.Generic
{
    public interface IFrozenCollection<T> : IReadOnlyCollection<T>
    {
        bool Contains(T item);
    }

#if NET6_0_OR_GREATER
#endif
}
