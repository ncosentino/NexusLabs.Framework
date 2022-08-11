using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks; // for value task

namespace NexusLabs.Collections.Generic
{
    public sealed class CachedEnumerable<T> :
        ICachedEnumerable<T>
    {
        private readonly List<T> _cache;

        // NOTE: always acquire enumerator THEN cache lock
        private readonly object _cacheLock;
        private readonly object _enumeratorLock;

        private IEnumerator<T> _enumerator;       

        public CachedEnumerable(IEnumerable<T> enumerable)
            : this(enumerable.GetEnumerator())
        {
        }

        public CachedEnumerable(IEnumerator<T> enumerator)
        {
            _enumerator = enumerator;
            _cache = new List<T>();
            _cacheLock = new object();
            _enumeratorLock = new object();
        }

        public int GetCount()
        {
            // NOTE: we don't need to lock the cache here because the
            // enumerator needs to exist in order for us to be modifying the
            // cache. So the non-existence of the enumerator should mean we're
            // safe to read it without a lock on it
            if (_enumerator == null)
            {
                return _cache.Count;
            }

            lock (_enumeratorLock)
            {
                // double check after acquiring lock
                if (_enumerator == null)
                {
                    return _cache.Count;
                }

                using (var enumerator = GetEnumerator())
                {
                    while (enumerator.MoveNext()) { }
                }

                return _cache.Count;
            }
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


            lock (_enumeratorLock)
            {
                lock (_cacheLock)
                {
                    if (_cache.Count <= index && _enumerator != null)
                    {
                        using (var enumerator = GetEnumerator())
                        {
                            while (_cache.Count <= index && enumerator.MoveNext()) { }
                        }
                    }
                }
            }

            lock (_cacheLock)
            {
                if (index >= _cache.Count)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(index),
                        $"{nameof(index)} was {index} but must be less than the " +
                        $"size of the collection, {_cache.Count}.");
                }

                return _cache[index];
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            // The index of the current item in the cache.
            int index = 0;

            // Enumerate the _cache first
            lock (_cacheLock)
            {
                for (; index < _cache.Count; index++)
                {
                    yield return _cache[index];
                }
            }

            // Continue enumeration of the original _enumerator, 
            // until it is finished. 
            // This adds items to the cache and increment 
            lock (_enumeratorLock)
            {
                for (; _enumerator != null && _enumerator.MoveNext(); index++)
                {
                    var current = _enumerator.Current;
                    lock (_cacheLock)
                    {
                        _cache.Add(current);
                    }

                    yield return current;
                }

                if (_enumerator != null)
                {
                    _enumerator.Dispose();
                    _enumerator = null;
                }
            }

            // Some other users of the same instance of CachedEnumerable
            // can add more items to the cache, 
            // so we need to enumerate them as well
            lock (_cacheLock)
            {
                for (; index < _cache.Count; index++)
                {
                    yield return _cache[index];
                }
            }
        }

#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public ValueTask DisposeAsync()
        {
            Dispose();
            return new ValueTask(Task.CompletedTask);
        }
#endif

        public void Dispose()
        {
            if (_enumerator == null)
            {
                return;
            }

            lock (_enumeratorLock)
            {
                // double check with lock since we can only go from having one to not (not the reverse)
                if (_enumerator == null)
                {
                    return;
                }

                _enumerator.Dispose();
                _enumerator = null;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();
    }
}
