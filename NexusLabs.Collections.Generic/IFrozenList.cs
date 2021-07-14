using System.Collections.Generic;

namespace NexusLabs.Collections.Generic
{
    public interface IFrozenList<T> :
		IReadOnlyList<T>,
		IFrozenCollection<T>
	{
    }
}
