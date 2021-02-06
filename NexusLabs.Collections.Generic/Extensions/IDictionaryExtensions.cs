using System.Collections.Generic;

namespace System.Linq
{
    public static class IDictionaryExtensions
    {
        public static void AddRange<TKey, TValue>(
            this IDictionary<TKey, TValue> dictionary,
            IEnumerable<KeyValuePair<TKey, TValue>> items)
        {
            foreach (var kvp in items)
            {
                dictionary.Add(kvp);
            }
        }
    }
}
