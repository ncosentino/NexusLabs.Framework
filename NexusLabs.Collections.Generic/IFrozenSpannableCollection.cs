using System;

namespace NexusLabs.Collections.Generic
{
#if NET6_0_OR_GREATER
    public interface IFrozenSpannableCollection<T> : IFrozenCollection<T>
    {
        ReadOnlySpan<T> GetReadOnlySpan();
    }
#endif
}
