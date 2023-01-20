using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NexusLabs.Framework.Testing;

using Xunit;

namespace NexusLabs.Collections.Generic.Tests
{
    public sealed class FrozenEnumerableExtensionTests
    {
        private const int TEST_COLLECTION_LENGTH = 10;

#if NET6_0_OR_GREATER
        [MemberData(nameof(GetEnumerableTestData))]
        [Theory]
        private void AsFrozenSpannableCollection_VariableInputs_CanUseReadOnlySpan(IEnumerable<int> input)
        {
            Assert.Equal(
                TEST_COLLECTION_LENGTH,
                input.AsFrozenSpannableCollection().GetReadOnlySpan().Length);
        }

        [MemberData(nameof(GetEnumerableTestData))]
        [Theory]
        private void AsFrozenSpannableList_VariableInputs_CanUseReadOnlySpan(IEnumerable<int> input)
        {
            Assert.Equal(
                TEST_COLLECTION_LENGTH,
                input.AsFrozenSpannableList().GetReadOnlySpan().Length);
        }

        [MemberData(nameof(GetAssumeFrozenSpannableCollectionTestData))]
        [Theory]
        private void AssumeAsOrCreateFrozenSpannableCollection_VariableInputs_ExpectedCount(
            bool expectedAssumeFrozen,
            IEnumerable<int> input)
        {
            var couldAssume = input.AssumeAsOrCreateFrozenSpannableCollection(out var frozen);
            AssertEx.Equal(
                expectedAssumeFrozen,
                couldAssume,
                $"Unexpected ability to assume frozen for type '{input.GetType()}'.");
            Assert.NotNull(frozen);
            Assert.Equal(10, frozen.Count);
        }

        [MemberData(nameof(GetAssumeFrozenSpannableListTestData))]
        [Theory]
        private void AssumeAsOrCreateFrozenSpannableList_VariableInputs_ExpectedCount(
            bool expectedAssumeFrozen,
            IEnumerable<int> input)
        {
            var couldAssume = input.AssumeAsOrCreateFrozenSpannableList(out var frozen);
            AssertEx.Equal(
                expectedAssumeFrozen,
                couldAssume,
                $"Unexpected ability to assume frozen for type '{input.GetType()}'.");
            Assert.NotNull(frozen);
            Assert.Equal(10, frozen.Count);
        }
#endif

        [MemberData(nameof(GetEnumerableTestData))]
        [Theory]
        private void AsFrozenCollection_VariableInputs_ExpectedCount(IEnumerable<int> input)
        {
            Assert.Equal(
                TEST_COLLECTION_LENGTH,
                input.AsFrozenCollection().Count);
        }

        [MemberData(nameof(GetEnumerableTestData))]
        [Theory]
        private void AsFrozenList_VariableInputs_ExpectedCount(IEnumerable<int> input)
        {
            Assert.Equal(
                TEST_COLLECTION_LENGTH,
                input.AsFrozenList().Count);
        }

        [MemberData(nameof(GetEnumerableTestData))]
        [Theory]
        private void AsFrozenHashSet_VariableInputs_ExpectedCount(IEnumerable<int> input)
        {
            Assert.Equal(
                TEST_COLLECTION_LENGTH,
                input.AsFrozenHashSet().Count);
        }

        [MemberData(nameof(GetAssumeFrozenCollectionTestData))]
        [Theory]
        private void AssumeAsOrCreateFrozenCollection_VariableInputs_ExpectedCount(
            bool expectedAssumeFrozen,
            IEnumerable<int> input)
        {
            var couldAssume = input.AssumeAsOrCreateFrozenCollection(out var frozen);
            AssertEx.Equal(
                expectedAssumeFrozen,
                couldAssume,
                $"Unexpected ability to assume frozen for type '{input.GetType()}'.");
            Assert.NotNull(frozen);
            Assert.Equal(10, frozen.Count);
        }

        [MemberData(nameof(GetAssumeFrozenListTestData))]
        [Theory]
        private void AssumeAsOrCreateFrozenList_VariableInputs_ExpectedCount(
            bool expectedAssumeFrozen,
            IEnumerable<int> input)
        {
            var couldAssume = input.AssumeAsOrCreateFrozenList(out var frozen);
            AssertEx.Equal(
                expectedAssumeFrozen,
                couldAssume,
                $"Unexpected ability to assume frozen for type '{input.GetType()}'.");
            Assert.NotNull(frozen);
            Assert.Equal(10, frozen.Count);
        }

        public static IEnumerable<object[]> GetEnumerableTestData()
        {
            // built in types
            yield return new object[] { Enumerable.Range(0, TEST_COLLECTION_LENGTH) };
            yield return new object[] { Enumerable.Range(0, TEST_COLLECTION_LENGTH).ToArray() };
            yield return new object[] { Enumerable.Range(0, TEST_COLLECTION_LENGTH).ToList() };
            yield return new object[] { Enumerable.Range(0, TEST_COLLECTION_LENGTH).ToHashSet() };

            // offered types in package
            yield return new object[] { new FrozenCollection<int>(Enumerable.Range(0, TEST_COLLECTION_LENGTH)) };
            yield return new object[] { new FrozenList<int>(Enumerable.Range(0, TEST_COLLECTION_LENGTH)) };
            yield return new object[] { new FrozenHashSet<int>(Enumerable.Range(0, TEST_COLLECTION_LENGTH)) };
#if NET6_0_OR_GREATER
            yield return new object[] { new FrozenSpannableCollection<int>(Enumerable.Range(0, TEST_COLLECTION_LENGTH)) };
            yield return new object[] { new FrozenSpannableList<int>(Enumerable.Range(0, TEST_COLLECTION_LENGTH)) };
#endif

            // internal wrappers
            yield return new object[] { new FrozenArrayWrapper<int>(Enumerable.Range(0, TEST_COLLECTION_LENGTH).ToArray()) };
            yield return new object[] { new FrozenListWrapper<int>(Enumerable.Range(0, TEST_COLLECTION_LENGTH).ToList()) };
            yield return new object[] { new FrozenIReadOnlyCollectionWrapper<int>(Enumerable.Range(0, TEST_COLLECTION_LENGTH).ToArray()) };
            yield return new object[] { new FrozenIReadOnlyListWrapper<int>(Enumerable.Range(0, TEST_COLLECTION_LENGTH).ToArray()) };
        }

        public static IEnumerable<object[]> GetAssumeFrozenCollectionTestData()
        {
            foreach (var entry in GetEnumerableTestData())
            {
                var shouldAssumeFrozen = false;
                var collection = entry[0];
                if (typeof(IFrozenCollection<int>).IsAssignableFrom(collection.GetType()) ||
                    typeof(HashSet<int>).IsAssignableFrom(collection.GetType()) ||
                    typeof(List<int>).IsAssignableFrom(collection.GetType()) ||
                    typeof(int[]).IsAssignableFrom(collection.GetType()))
                {
                    shouldAssumeFrozen = true;
                }

                yield return new object[] { shouldAssumeFrozen, collection };
            }
        }

        public static IEnumerable<object[]> GetAssumeFrozenListTestData()
        {
            foreach (var entry in GetEnumerableTestData())
            {
                var shouldAssumeFrozen = false;
                var collection = entry[0];
                if (typeof(IFrozenList<int>).IsAssignableFrom(collection.GetType()) ||
                    typeof(List<int>).IsAssignableFrom(collection.GetType()) ||
                    typeof(int[]).IsAssignableFrom(collection.GetType()))
                {
                    shouldAssumeFrozen = true;
                }

                yield return new object[] { shouldAssumeFrozen, collection };
            }
        }

#if NET6_0_OR_GREATER
        public static IEnumerable<object[]> GetAssumeFrozenSpannableCollectionTestData()
        {
            foreach (var entry in GetEnumerableTestData())
            {
                var shouldAssumeFrozen = false;
                var collection = entry[0];
                if (typeof(IFrozenSpannableCollection<int>).IsAssignableFrom(collection.GetType()) ||
                    typeof(List<int>).IsAssignableFrom(collection.GetType()) ||
                    typeof(int[]).IsAssignableFrom(collection.GetType()))
                {
                    shouldAssumeFrozen = true;
                }

                yield return new object[] { shouldAssumeFrozen, collection };
            }
        }

        public static IEnumerable<object[]> GetAssumeFrozenSpannableListTestData()
        {
            foreach (var entry in GetEnumerableTestData())
            {
                var shouldAssumeFrozen = false;
                var collection = entry[0];
                if (typeof(IFrozenSpannableList<int>).IsAssignableFrom(collection.GetType()) ||
                    typeof(List<int>).IsAssignableFrom(collection.GetType()) ||
                    typeof(int[]).IsAssignableFrom(collection.GetType()))
                {
                    shouldAssumeFrozen = true;
                }

                yield return new object[] { shouldAssumeFrozen, collection };
            }
        }
#endif
    }
}
