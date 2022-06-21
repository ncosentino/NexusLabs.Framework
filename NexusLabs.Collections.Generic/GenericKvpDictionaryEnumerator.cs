using System.Collections;
using System.Collections.Generic;

namespace NexusLabs.Collections.Generic
{
    public sealed class GenericKvpDictionaryEnumerator<TKey, TValue> : IDictionaryEnumerator
    {
        private IEnumerator<KeyValuePair<TKey, TValue>> _enumerator;

        public GenericKvpDictionaryEnumerator(IEnumerator<KeyValuePair<TKey, TValue>> enumerator)
        {
            _enumerator = enumerator;
        }

        public DictionaryEntry Entry
        {
            get { return new DictionaryEntry(_enumerator.Current.Key, _enumerator.Current.Value); }
        }

        public object Key
        {
            get { return _enumerator.Current.Key; }
        }

        public object Value
        {
            get { return _enumerator.Current.Value; }
        }

        public object Current
        {
            get { return Entry; }
        }

        public bool MoveNext()
        {
            return _enumerator.MoveNext();
        }

        public void Reset()
        {
            _enumerator.Reset();
        }
    }
}
