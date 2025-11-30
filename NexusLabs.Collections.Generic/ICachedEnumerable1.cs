using System;
using System.Collections;

namespace NexusLabs.Collections.Generic;

public interface ICachedEnumerable :
    IEnumerable,
    IDisposable,
    IAsyncDisposable
{
    int GetCount();

    object GetAt(int index);
}
