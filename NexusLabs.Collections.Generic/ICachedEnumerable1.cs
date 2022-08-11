using System;
using System.Collections;

namespace NexusLabs.Collections.Generic
{
    public interface ICachedEnumerable :
        IEnumerable,
        IDisposable
#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        ,IAsyncDisposable
#endif
    {
        int GetCount();

        object GetAt(int index);
    }
}
