using System.Collections.Generic;

using NexusLabs.Collections.Generic;

namespace System.Linq
{
#if NET6_0_OR_GREATER
    public static class ArrayExtensions
    {
        public static IFrozenSpannableList<T> AssumeAsFrozenSpannableList<T>(this T[] array)
        {
            if (!array.AssumeAsOrCreateFrozenSpannableList(out var frozen))
            {
                throw new InvalidOperationException("Could not freeze array.");
            }

            return frozen;
        }

        public static IFrozenSpannableCollection<T> AssumeAsFrozenSpannableCollection<T>(this T[] array)
        {
            if (!array.AssumeAsOrCreateFrozenSpannableCollection(out var frozen))
            {
                throw new InvalidOperationException("Could not freeze array.");
            }

            return frozen;
        }
    }
#endif
}