using System.Collections.Generic;

using NexusLabs.Collections.Generic;

namespace System.Linq
{
    public static class IReadOnlyDictionaryExtensions
    {
        /// <summary>
        /// Provides a mechanism to assume that the current dictionary can be 
        /// treated as frozen. This should be used with care as the intention 
        /// of the <see cref="IFrozenDictionary{TKey, TValue}"/> is that the 
        /// dictionary is unchanging, but using this method improperly can 
        /// circumvent this.
        /// </summary>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <param name="dictionary">The dictionary to use</param>
        /// <param name="equalityComparer">The equality comparer to use. Optional.</param>
        /// <returns>
        /// A new <see cref="IFrozenDictionary{TKey, TValue}"/> reference or 
        /// if the current dictionary was frozen, the original reference.
        /// </returns>
        public static IFrozenDictionary<TKey, TValue> AssumeAsFrozenDictionary<TKey, TValue>(
            this IReadOnlyDictionary<TKey, TValue> dictionary,
            IEqualityComparer<TKey> equalityComparer = default)
        {
            // since it's frozen we can directly return it already
            if (dictionary is IFrozenDictionary<TKey, TValue> frozenDictionary)
            {
                return frozenDictionary;
            }

            var frozen = new FrozenDictionary<TKey, TValue>(
                dictionary,
                equalityComparer);
            return frozen;
        }
    }
}
