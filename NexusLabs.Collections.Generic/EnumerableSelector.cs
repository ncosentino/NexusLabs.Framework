using System;
using System.Collections;
using System.Collections.Generic;

using NexusLabs.Framework;

namespace NexusLabs.Collections.Generic
{
    public sealed class EnumerableSelector : IEnumerableSelector
    {
        private Dictionary<Type, TrySelectRandomDelegate> _cacheTypesToConverter;
        private List<Tuple<MatchCollectionDelegate, TrySelectRandomDelegate>> _mapping;

        public EnumerableSelector()
        {
            _cacheTypesToConverter = new Dictionary<Type, TrySelectRandomDelegate>();
            _mapping = new List<Tuple<MatchCollectionDelegate, TrySelectRandomDelegate>>();
            _cacheTypesToConverter[typeof(IEnumerable)] = TrySelectRandomForEnumerable;
        }

        public void RegisterMapping(
            MatchCollectionDelegate matchCollectionDelegate,
            TrySelectRandomDelegate selectRandomOrDefaultDelegate)
        {
            _mapping.Add(Tuple.Create(
                matchCollectionDelegate,
                selectRandomOrDefaultDelegate));
        }

        public T RandomOrDefault<T>(
            IEnumerable<T> source,
            IRandom random)
        {
            return GetMapping(source).Invoke(
                source,
                random,
                out var selected)
                ? (T)selected
                : default;
        }

        public T Random<T>(
            IEnumerable<T> source,
            IRandom random)
        {
            if (!GetMapping(source).Invoke(
                source,
                random,
                out var selected))
            {
                throw new InvalidOperationException(
                    "Could not select a random element from the source.");
            }

            return (T)selected;
        }

        private bool TrySelectRandomForEnumerable(
            object source,
            IRandom random,
            out object selected)
        {
            object current = default;
            var count = 0;
            foreach (var element in (IEnumerable)source)
            {
                count++;

                if (random.Next(0, count) == 0)
                {
                    current = element;
                }
            }

            if (count == 0)
            {
                selected = default;
                return false;
            }

            selected = current;
            return true;
        }

        private TrySelectRandomDelegate GetMapping<T>(T source)
        {
            // check if we have one cached...
            if (_cacheTypesToConverter.TryGetValue(source.GetType(), out var mapped))
            {
                return mapped;
            }

            // nothing cached so check our mappings
            foreach (var entry in _mapping)
            {
                if (entry.Item1(source))
                {
                    _cacheTypesToConverter[source.GetType()] = entry.Item2;
                    return entry.Item2;
                }
            }

            // no mapping so store the default
            if (_cacheTypesToConverter.TryGetValue(typeof(IEnumerable), out mapped))
            {
                _cacheTypesToConverter[source.GetType()] = mapped;
                return mapped;
            }

            throw new NotSupportedException(
                $"No mappers registered to handle type '{source.GetType().FullName}'.");
        }
    }
}
