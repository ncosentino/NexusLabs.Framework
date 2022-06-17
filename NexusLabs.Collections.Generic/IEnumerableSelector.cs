using System.Collections.Generic;

using NexusLabs.Framework;

namespace NexusLabs.Collections.Generic
{
    public delegate bool MatchCollectionDelegate(object collection);

    public delegate bool TrySelectRandomDelegate(
        object collection,
        IRandom random,
        out object selected);

    public interface IEnumerableSelector
    {
        T RandomOrDefault<T>(
            IEnumerable<T> source,
            IRandom random);

        T Random<T>(
            IEnumerable<T> source,
            IRandom random);

        void RegisterMapping(
            MatchCollectionDelegate matchCollectionDelegate,
            TrySelectRandomDelegate selectRandomOrDefaultDelegate);
    }
}
