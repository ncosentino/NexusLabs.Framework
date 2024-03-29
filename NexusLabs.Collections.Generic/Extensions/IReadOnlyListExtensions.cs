﻿using System.Collections.Generic;

using NexusLabs.Collections.Generic;

namespace System.Linq
{
    public static class IReadOnlyListExtensions
    {
        public static IFrozenList<T> AssumeAsFrozenList<T>(this IReadOnlyList<T> list)
        {
            if (!list.AssumeAsOrCreateFrozenList(out var frozen))
            {
                throw new InvalidOperationException("Could not freeze list.");
            }

            return frozen;
        }
    }
}