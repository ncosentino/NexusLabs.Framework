using System.Collections.Generic;

using NexusLabs.Collections.Generic;

namespace System.Linq
{
    public static class IReadOnlyCollectionExtensions
    {
        public static IFrozenCollection<T> AssumeAsFrozenCollection<T>(this IReadOnlyCollection<T> collection)
        {
            if (!collection.AssumeAsOrCreateFrozenCollection(out var frozen))
            {
                throw new InvalidOperationException("Could not freeze collection.");
            }

            return frozen;
        }
    }
}
