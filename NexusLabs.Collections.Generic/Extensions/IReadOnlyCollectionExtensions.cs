using System.Collections.Generic;

using NexusLabs.Collections.Generic;

namespace System.Linq
{
    public static class IReadOnlyCollectionExtensions
    {
        public static IFrozenCollection<T> AssumeAsFrozenCollection<T>(this IReadOnlyCollection<T> collection)
        {
            // since it's frozen we can directly return it already
            if (collection is IFrozenCollection<T> frozenCollection)
            {
                return frozenCollection;
            }

            var frozen = new FrozenCollection<T>(collection);
            return frozen;
        }
    }
}
