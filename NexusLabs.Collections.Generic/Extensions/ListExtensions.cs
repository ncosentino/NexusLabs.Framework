using System.Collections.Generic;

using NexusLabs.Collections.Generic;

namespace System.Linq
{
#if NET6_0_OR_GREATER
    public static class ListExtensions
    {
        public static IFrozenSpannableList<T> AssumeAsFrozenSpannableList<T>(this List<T> list)
        {
            if (!list.AssumeAsOrCreateFrozenSpannableList(out var frozen))
            {
                throw new InvalidOperationException("Could not freeze list.");
            }

            return frozen;
        }
    }
#endif
}