using System;
using System.Collections;
using System.Collections.Generic;

namespace NexusLabs.Collections.Generic
{
    public sealed class CachedEnumerable<T> :
        ICachedEnumerable<T>
    {
        private IEnumerator<T> _enumerator;
        private readonly List<T> _cache;

        public CachedEnumerable(IEnumerable<T> enumerable)
            : this(enumerable.GetEnumerator())
        {
        }

        public CachedEnumerable(IEnumerator<T> enumerator)
        {
            _enumerator = enumerator;
            _cache = new List<T>();
        }

        public int GetCount()
        {
            if (_enumerator != null)
            {
                using (var enumerator = GetEnumerator())
                {
                    while (enumerator.MoveNext()) { }
                }
            }

            return _cache.Count;
        }

        object ICachedEnumerable.GetAt(int index)
            => GetAt(index);

        public T GetAt(int index)
        {
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(index),
                    $"{nameof(index)} must be greater than or equal to 0.");
            }

            if (_cache.Count <= index)
            {
                if (_enumerator != null)
                {
                    using (var enumerator = GetEnumerator())
                    {
                        while (_cache.Count <= index && enumerator.MoveNext()) { }
                    }
                }
            }

            if (index >= _cache.Count)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(index),
                    $"{nameof(index)} was {index} but must be less than the " +
                    $"size of the collection, {_cache.Count}.");
            }

            return _cache[index];
        }

        public IEnumerator<T> GetEnumerator()
        {
            // The index of the current item in the cache.
            int index = 0;

            // Enumerate the _cache first
            for (; index < _cache.Count; index++)
            {
                yield return _cache[index];
            }

            // Continue enumeration of the original _enumerator, 
            // until it is finished. 
            // This adds items to the cache and increment 
            for (; _enumerator != null && _enumerator.MoveNext(); index++)
            {
                var current = _enumerator.Current;
                _cache.Add(current);
                yield return current;
            }

            if (_enumerator != null)
            {
                _enumerator.Dispose();
                _enumerator = null;
            }

            // Some other users of the same instance of CachedEnumerable
            // can add more items to the cache, 
            // so we need to enumerate them as well
            for (; index < _cache.Count; index++)
            {
                yield return _cache[index];
            }
        }

        public void Dispose()
        {
            if (_enumerator != null)
            {
                _enumerator.Dispose();
                _enumerator = null;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();
    }
}
