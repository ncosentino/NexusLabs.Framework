using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace NexusLabs.Collections.Generic
{
    public static class EnumerableFrozenExtensions
    {
        /// <summary>
        /// Gets the provided <see cref="IEnumerable{T}"/> as an
        /// <see cref="IFrozenCollection{T}"/> either by providing the same 
        /// instance if it is already frozen or creating a new
        /// <see cref="IFrozenCollection{T}"/> instance in order to provide 
        /// frozen state.
        /// </summary>
        /// <typeparam name="T">
        /// The type of items in <paramref name="enumerable"/>.
        /// </typeparam>
        /// <param name="enumerable">
        /// The <see cref="IEnumerable{T}"/> to get as frozen or wrap as frozen.
        /// </param>
        /// <returns>
        /// The provided <paramref name="enumerable"/> as an
        /// <see cref="IFrozenCollection{T}"/> instance if it is already frozen;
        /// Otherwise, a new <see cref="IFrozenCollection{T}"/> that wraps the 
        /// input.
        /// </returns>
        /// <seealso cref="GetAsOrCreateFrozenCollection{T}(IEnumerable{T}, out IFrozenCollection{T})"/>
        public static IFrozenCollection<T> AsFrozenCollection<T>(this IEnumerable<T> enumerable)
        {
            enumerable.GetAsOrCreateFrozenCollection(out var frozen);
            return frozen;
        }

#if NET6_0_OR_GREATER
        /// <summary>
        /// Gets the provided <see cref="IEnumerable{T}"/> as an
        /// <see cref="IFrozenSpannableCollection{T}"/> either by providing the same 
        /// instance if it is already frozen and spannable or creating a new
        /// <see cref="IFrozenSpannableCollection{T}"/> instance in order to provide 
        /// frozen state.
        /// </summary>
        /// <typeparam name="T">
        /// The type of items in <paramref name="enumerable"/>.
        /// </typeparam>
        /// <param name="enumerable">
        /// The <see cref="IEnumerable{T}"/> to get as frozen or wrap as frozen.
        /// </param>
        /// <returns>
        /// The provided <paramref name="enumerable"/> as an
        /// <see cref="IFrozenSpannableCollection{T}"/> instance if it is already frozen and spannable;
        /// Otherwise, a new <see cref="IFrozenSpannableCollection{T}"/> that wraps the 
        /// input.
        /// </returns>
        /// <seealso cref="GetAsOrCreateFrozenCollection{T}(IEnumerable{T}, out IFrozenCollection{T})"/>
        public static IFrozenSpannableCollection<T> AsFrozenSpannableCollection<T>(this IEnumerable<T> enumerable)
        {
            enumerable.GetAsOrCreateFrozenSpannableCollection(out var frozen);
            return frozen;
        }
#endif

        /// <summary>
        /// Gets an <see cref="IEnumerable{T}"/> as an 
        /// <see cref="IFrozenCollection{T}"/> if it is already frozen; 
        /// Otherwise, create a new instance of 
        /// <see cref="IFrozenCollection{T}"/>.
        /// </summary>
        /// <typeparam name="T">
        /// The type of items in the <see cref="IEnumerable{T}"/>.
        /// </typeparam>
        /// <param name="items">
        /// The items to get as or create a <see cref="IFrozenCollection{T}"/>
        /// for.
        /// </param>
        /// <param name="frozen">
        /// The resulting <see cref="IFrozenCollection{T}"/> instance.
        /// </param>
        /// <returns>
        /// <c>true</c> if the input was already frozen; Otherwise,
        /// <c>false</c>.
        /// </returns>
        public static bool GetAsOrCreateFrozenCollection<T>(
            this IEnumerable<T> items,
            out IFrozenCollection<T> frozen)
        {
            if (items.TryGetAsFrozenCollection(out frozen))
            {
                return true;
            }

            frozen = new FrozenArrayWrapper<T>(items.ToArray());
            return false;
        }

#if NET6_0_OR_GREATER
        /// <summary>
        /// Gets an <see cref="IEnumerable{T}"/> as an 
        /// <see cref="IFrozenCollection{T}"/> if it is already frozen and spannable; 
        /// Otherwise, create a new instance of 
        /// <see cref="IFrozenCollection{T}"/>.
        /// </summary>
        /// <typeparam name="T">
        /// The type of items in the <see cref="IEnumerable{T}"/>.
        /// </typeparam>
        /// <param name="items">
        /// The items to get as or create a <see cref="IFrozenCollection{T}"/>
        /// for.
        /// </param>
        /// <param name="frozen">
        /// The resulting <see cref="IFrozenCollection{T}"/> instance.
        /// </param>
        /// <returns>
        /// <c>true</c> if the input was already frozen; Otherwise,
        /// <c>false</c>.
        /// </returns>
        public static bool GetAsOrCreateFrozenSpannableCollection<T>(
            this IEnumerable<T> items,
            out IFrozenSpannableCollection<T> frozen)
        {
            if (items.TryGetAsFrozenSpannableCollection(out frozen))
            {
                return true;
            }

            frozen = new FrozenArrayWrapper<T>(items.ToArray());
            return false;
        }
#endif

        /// <summary>
        /// Assumes the <see cref="IEnumerable{T}"/> as frozen if the method is
        /// able to; Otherwise, creates a new instance of 
        /// <see cref="IFrozenCollection{T}"/>.
        /// </summary>
        /// <typeparam name="T">
        /// The type of items in the <see cref="IEnumerable{T}"/>.
        /// </typeparam>
        /// <param name="items">
        /// The items to get as or create a <see cref="IFrozenCollection{T}"/>
        /// for.
        /// </param>
        /// <param name="frozen">
        /// The resulting <see cref="IFrozenCollection{T}"/> instance.
        /// </param>
        /// <returns>
        /// <c>true</c> if the input was already frozen; Otherwise,
        /// <c>false</c>.
        /// </returns>
        public static bool AssumeAsOrCreateFrozenCollection<T>(
            this IEnumerable<T> items,
            out IFrozenCollection<T> frozen)
        {
            if (items.TryAssumeAsFrozenCollection(out frozen))
            {
                return true;
            }

            frozen = new FrozenCollection<T>(items);
            return false;
        }

#if NET6_0_OR_GREATER
        /// <summary>
        /// Assumes the <see cref="IEnumerable{T}"/> as frozen if the method is
        /// able to; Otherwise, creates a new instance of 
        /// <see cref="IFrozenSpannableCollection{T}"/>.
        /// </summary>
        /// <typeparam name="T">
        /// The type of items in the <see cref="IEnumerable{T}"/>.
        /// </typeparam>
        /// <param name="items">
        /// The items to get as or create a <see cref="IFrozenCollection{T}"/>
        /// for.
        /// </param>
        /// <param name="frozen">
        /// The resulting <see cref="IFrozenCollection{T}"/> instance.
        /// </param>
        /// <returns>
        /// <c>true</c> if the input was already frozen; Otherwise,
        /// <c>false</c>.
        /// </returns>
        public static bool AssumeAsOrCreateFrozenSpannableCollection<T>(
            this IEnumerable<T> items,
            out IFrozenSpannableCollection<T> frozen)
        {
            if (items.TryAssumeAsFrozenSpannableCollection(out frozen))
            {
                return true;
            }

            frozen = new FrozenSpannableCollection<T>(items);
            return false;
        }
#endif
        /// <summary>
        /// Gets the provided <see cref="IEnumerable{T}"/> as an
        /// <see cref="IFrozenList{T}"/> either by providing the same 
        /// instance if it is already frozen or creating a new
        /// <see cref="IFrozenList{T}"/> instance in order to provide 
        /// frozen state.
        /// </summary>
        /// <typeparam name="T">
        /// The type of items in <paramref name="enumerable"/>.
        /// </typeparam>
        /// <param name="enumerable">
        /// The <see cref="IEnumerable{T}"/> to get as frozen or wrap as frozen.
        /// </param>
        /// <returns>
        /// The provided <paramref name="enumerable"/> as an
        /// <see cref="IFrozenList{T}"/> instance if it is already frozen;
        /// Otherwise, a new <see cref="IFrozenList{T}"/> that wraps the 
        /// input.
        /// </returns>
        /// <seealso cref="GetAsOrCreateFrozenList{T}(IEnumerable{T}, out IFrozenList{T})"/>
        public static IFrozenList<T> AsFrozenList<T>(this IEnumerable<T> enumerable)
        {
            enumerable.GetAsOrCreateFrozenList(out var frozen);
            return frozen;
        }

#if NET6_0_OR_GREATER
        /// <summary>
        /// Gets the provided <see cref="IEnumerable{T}"/> as an
        /// <see cref="IFrozenSpannableList{T}"/> either by providing the same 
        /// instance if it is already frozen and spannable or creating a new
        /// <see cref="IFrozenSpannableList{T}"/> instance in order to provide 
        /// frozen state.
        /// </summary>
        /// <typeparam name="T">
        /// The type of items in <paramref name="enumerable"/>.
        /// </typeparam>
        /// <param name="enumerable">
        /// The <see cref="IEnumerable{T}"/> to get as frozen or wrap as frozen.
        /// </param>
        /// <returns>
        /// The provided <paramref name="enumerable"/> as an
        /// <see cref="IFrozenSpannableList{T}"/> instance if it is already frozen;
        /// Otherwise, a new <see cref="IFrozenSpannableList{T}"/> that wraps the 
        /// input.
        /// </returns>
        /// <seealso cref="GetAsOrCreateFrozenSpannableList{T}(IEnumerable{T}, out IFrozenSpannableList{T})"/>
        public static IFrozenSpannableList<T> AsFrozenSpannableList<T>(this IEnumerable<T> enumerable)
        {
            enumerable.GetAsOrCreateFrozenSpannableList(out var frozen);
            return frozen;
        }
#endif

        /// <summary>
        /// Gets an <see cref="IEnumerable{T}"/> as an 
        /// <see cref="IFrozenList{T}"/> if it is already frozen; 
        /// Otherwise, create a new instance of 
        /// <see cref="IFrozenList{T}"/>.
        /// </summary>
        /// <typeparam name="T">
        /// The type of items in the <see cref="IEnumerable{T}"/>.
        /// </typeparam>
        /// <param name="items">
        /// The items to get as or create a <see cref="IFrozenList{T}"/>
        /// for.
        /// </param>
        /// <param name="frozen">
        /// The resulting <see cref="IFrozenList{T}"/> instance.
        /// </param>
        /// <returns>
        /// <c>true</c> if the input was already frozen; Otherwise,
        /// <c>false</c>.
        /// </returns>
        public static bool GetAsOrCreateFrozenList<T>(
            this IEnumerable<T> items,
            out IFrozenList<T> frozen)
        {
            if (items.TryGetAsFrozenList(out frozen))
            {
                return true;
            }

            frozen = new FrozenArrayWrapper<T>(items.ToArray());
            return false;
        }

#if NET6_0_OR_GREATER
        /// <summary>
        /// Gets an <see cref="IEnumerable{T}"/> as an 
        /// <see cref="IFrozenList{T}"/> if it is already frozen and spannable; 
        /// Otherwise, create a new instance of 
        /// <see cref="IFrozenList{T}"/>.
        /// </summary>
        /// <typeparam name="T">
        /// The type of items in the <see cref="IEnumerable{T}"/>.
        /// </typeparam>
        /// <param name="items">
        /// The items to get as or create a <see cref="IFrozenList{T}"/>
        /// for.
        /// </param>
        /// <param name="frozen">
        /// The resulting <see cref="IFrozenList{T}"/> instance.
        /// </param>
        /// <returns>
        /// <c>true</c> if the input was already frozen; Otherwise,
        /// <c>false</c>.
        /// </returns>
        public static bool GetAsOrCreateFrozenSpannableList<T>(
            this IEnumerable<T> items,
            out IFrozenSpannableList<T> frozen)
        {
            if (items.TryGetAsFrozenSpannableList(out frozen))
            {
                return true;
            }

            frozen = new FrozenArrayWrapper<T>(items.ToArray());
            return false;
        }
#endif

        /// <summary>
        /// Assumes the <see cref="IEnumerable{T}"/> as frozen if the method is
        /// able to; Otherwise, creates a new instance of 
        /// <see cref="IFrozenList{T}"/>.
        /// </summary>
        /// <typeparam name="T">
        /// The type of items in the <see cref="IEnumerable{T}"/>.
        /// </typeparam>
        /// <param name="items">
        /// The items to get as or create a <see cref="IFrozenList{T}"/>
        /// for.
        /// </param>
        /// <param name="frozen">
        /// The resulting <see cref="IFrozenList{T}"/> instance.
        /// </param>
        /// <returns>
        /// <c>true</c> if the input was already frozen; Otherwise,
        /// <c>false</c>.
        /// </returns>
        public static bool AssumeAsOrCreateFrozenList<T>(
            this IEnumerable<T> items,
            out IFrozenList<T> frozen)
        {
            if (items.TryAssumeAsFrozenList(out frozen))
            {
                return true;
            }

            frozen = new FrozenList<T>(items);
            return false;
        }

#if NET6_0_OR_GREATER
        /// <summary>
        /// Assumes the <see cref="IEnumerable{T}"/> as frozen if the method is
        /// able to; Otherwise, creates a new instance of 
        /// <see cref="IFrozenSpannableList{T}"/>.
        /// </summary>
        /// <typeparam name="T">
        /// The type of items in the <see cref="IEnumerable{T}"/>.
        /// </typeparam>
        /// <param name="items">
        /// The items to get as or create a <see cref="IFrozenSpannableList{T}"/>
        /// for.
        /// </param>
        /// <param name="frozen">
        /// The resulting <see cref="IFrozenList{T}"/> instance.
        /// </param>
        /// <returns>
        /// <c>true</c> if the input was already frozen; Otherwise,
        /// <c>false</c>.
        /// </returns>
        public static bool AssumeAsOrCreateFrozenSpannableList<T>(
            this IEnumerable<T> items,
            out IFrozenSpannableList<T> frozen)
        {
            if (items.TryAssumeAsFrozenSpannableList(out frozen))
            {
                return true;
            }

            frozen = new FrozenSpannableList<T>(items);
            return false;
        }
#endif

        /// <summary>
        /// Tries to assume the provided <see cref="IEnumerable{T}"/> as frozen.
        /// </summary>
        /// <typeparam name="T">
        /// The type of items in <paramref name="items"/>.
        /// </typeparam>
        /// <param name="items">
        /// The items to try and assume as frozen.
        /// </param>
        /// <param name="frozen">
        /// The output <see cref="IFrozenCollection{T}"/> when successfully 
        /// assumed as frozen.
        /// </param>
        /// <returns>
        /// <c>true</c> if <paramref name="items"/> could be assumed as frozen;
        /// Otherwise, <c>false</c>.
        /// </returns>
        public static bool TryAssumeAsFrozenCollection<T>(
            this IEnumerable<T> items,
            out IFrozenCollection<T> frozen)
        {
            if (items is List<T> itemList)
            {
                frozen = new FrozenListWrapper<T>(itemList);
                return true;
            }

            if (items is T[] itemArray)
            {
                frozen = new FrozenArrayWrapper<T>(itemArray);
                return true;
            }

            if (items is IFrozenCollection<T> frozenCollection)
            {
                frozen = frozenCollection;
                return true;
            }

            if (items is IReadOnlyCollection<T> readOnlyCollection)
            {
                frozen = new FrozenIReadOnlyCollectionWrapper<T>(readOnlyCollection);
                return true;
            }

            frozen = null;
            return false;
        }

#if NET6_0_OR_GREATER
        /// <summary>
        /// Tries to assume the provided <see cref="IEnumerable{T}"/> as frozen
        /// and spannable.
        /// </summary>
        /// <typeparam name="T">
        /// The type of items in <paramref name="items"/>.
        /// </typeparam>
        /// <param name="items">
        /// The items to try and assume as frozen and spannable.
        /// </param>
        /// <param name="frozen">
        /// The output <see cref="IFrozenSpannableCollection{T}"/> when successfully 
        /// assumed as frozen.
        /// </param>
        /// <returns>
        /// <c>true</c> if <paramref name="items"/> could be assumed as frozen
        /// and spannable; Otherwise, <c>false</c>.
        /// </returns>
        public static bool TryAssumeAsFrozenSpannableCollection<T>(
            this IEnumerable<T> items,
            out IFrozenSpannableCollection<T> frozen)
        {
            if (items is List<T> itemList)
            {
                frozen = new FrozenListWrapper<T>(itemList);
                return true;
            }

            if (items is T[] itemArray)
            {
                frozen = new FrozenArrayWrapper<T>(itemArray);
                return true;
            }

            if (items is IFrozenSpannableCollection<T> frozenCollection)
            {
                frozen = frozenCollection;
                return true;
            }

            frozen = null;
            return false;
        }
#endif

        /// <summary>
        /// Tries to get the provided <see cref="IEnumerable{T}"/> as frozen.
        /// </summary>
        /// <typeparam name="T">
        /// The type of items in <paramref name="items"/>.
        /// </typeparam>
        /// <param name="items">
        /// The items to try and get as frozen.
        /// </param>
        /// <param name="frozen">
        /// The output <see cref="IFrozenCollection{T}"/> when the input 
        /// <paramref name="items"/> are already frozen.
        /// </param>
        /// <returns>
        /// <c>true</c> if <paramref name="items"/> is already a <see cref="IFrozenCollection{T}"/>;
        /// Otherwise, <c>false</c>.
        /// </returns>
        /// <remarks>
        /// This is very much just like using the <c>as</c> keyword.
        /// </remarks>
        public static bool TryGetAsFrozenCollection<T>(
            this IEnumerable<T> items,
            out IFrozenCollection<T> frozen)
        {
            if (items is IFrozenCollection<T> frozenCollection)
            {
                frozen = frozenCollection;
                return true;
            }

            frozen = null;
            return false;
        }

#if NET6_0_OR_GREATER
        public static bool TryGetAsFrozenSpannableCollection<T>(
            this IEnumerable<T> items,
            out IFrozenSpannableCollection<T> frozen)
        {
            if (items is IFrozenSpannableCollection<T> frozenCollection)
            {
                frozen = frozenCollection;
                return true;
            }

            frozen = null;
            return false;
        }
#endif
        /// <summary>
        /// Tries to assume the provided <see cref="IEnumerable{T}"/> as frozen.
        /// </summary>
        /// <typeparam name="T">
        /// The type of items in <paramref name="items"/>.
        /// </typeparam>
        /// <param name="items">
        /// The items to try and assume as frozen.
        /// </param>
        /// <param name="frozen">
        /// The output <see cref="IFrozenList{T}"/> when successfully 
        /// assumed as frozen.
        /// </param>
        /// <returns>
        /// <c>true</c> if <paramref name="items"/> could be assumed as frozen;
        /// Otherwise, <c>false</c>.
        /// </returns>
        public static bool TryAssumeAsFrozenList<T>(
            this IEnumerable<T> items,
            out IFrozenList<T> frozen)
        {
            if (items is List<T> itemList)
            {
                frozen = new FrozenListWrapper<T>(itemList);
                return true;
            }

            if (items is T[] itemArray)
            {
                frozen = new FrozenArrayWrapper<T>(itemArray);
                return true;
            }

            if (items is IFrozenList<T> frozenCollection)
            {
                frozen = frozenCollection;
                return true;
            }

            if (items is IReadOnlyList<T> readOnlyList)
            {
                frozen = new FrozenIReadOnlyListWrapper<T>(readOnlyList);
                return true;
            }

            frozen = null;
            return false;
        }

#if NET6_0_OR_GREATER
        /// <summary>
        /// Tries to assume the provided <see cref="IEnumerable{T}"/> as frozen
        /// and spannable.
        /// </summary>
        /// <typeparam name="T">
        /// The type of items in <paramref name="items"/>.
        /// </typeparam>
        /// <param name="items">
        /// The items to try and assume as frozen and spannable.
        /// </param>
        /// <param name="frozen">
        /// The output <see cref="IFrozenSpannableList{T}{T}"/> when successfully 
        /// assumed as frozen.
        /// </param>
        /// <returns>
        /// <c>true</c> if <paramref name="items"/> could be assumed as frozen
        /// and spannable; Otherwise, <c>false</c>.
        /// </returns>
        public static bool TryAssumeAsFrozenSpannableList<T>(
            this IEnumerable<T> items,
            out IFrozenSpannableList<T> frozen)
        {
            if (items is List<T> itemList)
            {
                frozen = new FrozenListWrapper<T>(itemList);
                return true;
            }

            if (items is T[] itemArray)
            {
                frozen = new FrozenArrayWrapper<T>(itemArray);
                return true;
            }

            if (items is IFrozenSpannableList<T> frozenList)
            {
                frozen = frozenList;
                return true;
            }

            frozen = null;
            return false;
        }
#endif
        /// <summary>
        /// Tries to get the provided <see cref="IEnumerable{T}"/> as frozen.
        /// </summary>
        /// <typeparam name="T">
        /// The type of items in <paramref name="items"/>.
        /// </typeparam>
        /// <param name="items">
        /// The items to try and get as frozen.
        /// </param>
        /// <param name="frozen">
        /// The output <see cref="IFrozenList{T}"/> when the input 
        /// <paramref name="items"/> are already frozen.
        /// </param>
        /// <returns>
        /// <c>true</c> if <paramref name="items"/> is already a <see cref="IFrozenList{T}"/>;
        /// Otherwise, <c>false</c>.
        /// </returns>
        /// <remarks>
        /// This is very much just like using the <c>as</c> keyword.
        /// </remarks>
        public static bool TryGetAsFrozenList<T>(
            this IEnumerable<T> items,
            out IFrozenList<T> frozen)
        {
            if (items is IFrozenList<T> frozenList)
            {
                frozen = frozenList;
                return true;
            }

            frozen = null;
            return false;
        }

#if NET6_0_OR_GREATER
        public static bool TryGetAsFrozenSpannableList<T>(
            this IEnumerable<T> items,
            out IFrozenSpannableList<T> frozen)
        {
            if (items is IFrozenSpannableList<T> frozenList)
            {
                frozen = frozenList;
                return true;
            }

            frozen = null;
            return false;
        }
#endif

        public static IFrozenHashSet<T> AsFrozenHashSet<T>(this IEnumerable<T> enumerable) =>
            AsFrozenHashSet(enumerable, null);

        public static IFrozenHashSet<T> AsFrozenHashSet<T>(
            this IEnumerable<T> enumerable,
            IEqualityComparer<T> equalityComparer)
        {
            var frozen = new FrozenHashSet<T>(enumerable, equalityComparer);
            Enumerable.Empty<KeyValuePair<string, int>>().ToDictionary(x => x.Key, x => x.Value);
            return frozen;
        }

        public static IFrozenDictionary<TResultKey, TResultValue> AsFrozenDictionary<TSourceKey, TSourceValue, TResultKey, TResultValue>(
            this IEnumerable<KeyValuePair<TSourceKey, TSourceValue>> enumerable,
            Func<KeyValuePair<TSourceKey, TSourceValue>, TResultKey> keySelector,
            Func<KeyValuePair<TSourceKey, TSourceValue>, TResultValue> valueSelector) => AsFrozenDictionary(
                enumerable,
                keySelector,
                valueSelector,
                null);

        public static IFrozenDictionary<TResultKey, TResultValue> AsFrozenDictionary<TSourceKey, TSourceValue, TResultKey, TResultValue>(
            this IEnumerable<KeyValuePair<TSourceKey, TSourceValue>> enumerable,
            Func<KeyValuePair<TSourceKey, TSourceValue>, TResultKey> keySelector,
            Func<KeyValuePair<TSourceKey, TSourceValue>, TResultValue> valueSelector,
            IEqualityComparer<TResultKey> equalityComparer) => AsFrozenDictionary(
                enumerable.Select(kvp => new KeyValuePair<TResultKey, TResultValue>(
                    keySelector(kvp),
                    valueSelector(kvp))),
                equalityComparer);

        public static IFrozenDictionary<TKey, TValue> AsFrozenDictionary<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> enumerable) =>
            AsFrozenDictionary(enumerable, null);

        public static IFrozenDictionary<TKey, TValue> AsFrozenDictionary<TKey, TValue>(
            this IEnumerable<KeyValuePair<TKey, TValue>> enumerable,
            IEqualityComparer<TKey> equalityComparer)
        {
            var frozen = new FrozenDictionary<TKey, TValue>(enumerable, equalityComparer);
            return frozen;
        }
    }
}
