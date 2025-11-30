using System;
using System.Collections.Generic;

namespace NexusLabs.Collections.Generic;

public delegate bool MatchCollectionDelegate(object collection);

public delegate bool TrySelectRandomDelegate(
    object collection,
    Random random,
    out object selected);

public interface IEnumerableSelector
{
    T RandomOrDefault<T>(
        IEnumerable<T> source,
        Random random);

    T Random<T>(
        IEnumerable<T> source,
        Random random);

    void RegisterMapping(
        MatchCollectionDelegate matchCollectionDelegate,
        TrySelectRandomDelegate selectRandomOrDefaultDelegate);
}
