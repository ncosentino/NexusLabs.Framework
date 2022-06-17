using System.Collections.Generic;

using NexusLabs.Collections.Generic;

namespace System.Linq
{
    public static class IReadOnlyListExtensions
    {
        public static IFrozenList<T> AssumeAsFrozenList<T>(this IReadOnlyList<T> list)
        {
            // since it's frozen we can directly return it already
            if (list is IFrozenList<T> frozenList)
            {
                return frozenList;
            }

            var frozen = new FrozenList<T>(list);
            return frozen;
        }
    }
}
