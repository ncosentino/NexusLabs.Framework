using System.Collections.Generic;

namespace NexusLabs.Collections.Generic
{
    public interface ICachedEnumerable<T> :
        IEnumerable<T>,
        ICachedEnumerable
    {
        new T GetAt(int index);
    }
}
