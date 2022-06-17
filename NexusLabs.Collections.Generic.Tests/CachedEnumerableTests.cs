using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Xunit;

namespace NexusLabs.Collections.Generic.Tests
{
    [ExcludeFromCodeCoverage]
    public sealed class CachedEnumerableTests
    {
        [Fact]
        public void Enumerate_WrappedArray_SequenceEqual()
        {
            var wrapped = new int[] { 1, 2, 3, 4, 5 };
            var cachedEnumerable = new CachedEnumerable<int>(wrapped);
            Assert.Equal(wrapped, cachedEnumerable);
        }

        [Fact]
        public void Enumerate_WrappedArray_SubsequentAttemptsSequenceEqual()
        {
            var wrapped = new int[] { 1, 2, 3, 4, 5 };
            var cachedEnumerable = new CachedEnumerable<int>(wrapped);
            
            for (var i = 0; i < 3; i++)
            {
                Assert.Equal(wrapped, cachedEnumerable);
            }
        }

        [Fact]
        public void Enumerate_SubsequentAttempts_UsesCache()
        {
            var expected = new int[] { 1, 2, 3, 4, 5 };
            var cachedEnumerable = new CachedEnumerable<int>(new SingleEnumerate());

            for (var i = 0; i < 3; i++)
            {
                Assert.Equal(expected, cachedEnumerable);
            }
        }

        [Fact]
        public void GetCount_SubsequentAttempts_UsesCache()
        {
            var cachedEnumerable = new CachedEnumerable<int>(new SingleEnumerate());

            for (var i = 0; i < 3; i++)
            {
                Assert.Equal(5, cachedEnumerable.GetCount());
            }
        }

        [Fact]
        public void GetAt_IndexLessThanZero_ThrowsArgumentOutOfRangeException()
        {
            var cachedEnumerable = new CachedEnumerable<int>(new SingleEnumerate());
            Assert.Throws<ArgumentOutOfRangeException>(() => cachedEnumerable.GetAt(-1));
        }

        [Fact]
        public void GetAt_IndexTooLarge_ThrowsArgumentOutOfRangeException()
        {
            var cachedEnumerable = new CachedEnumerable<int>(new SingleEnumerate());
            Assert.Throws<ArgumentOutOfRangeException>(() => cachedEnumerable.GetAt(6));
        }

        [Fact]
        public void GetAt_ValidIndex_ExpectedValue()
        {
            var cachedEnumerable = new CachedEnumerable<int>(new SingleEnumerate());
            Assert.Equal(1, cachedEnumerable.GetAt(0));
            Assert.Equal(2, cachedEnumerable.GetAt(1));
            Assert.Equal(3, cachedEnumerable.GetAt(2));
        }

        [Fact]
        public void GetAt_ValidIndexSubsequentAttempts_UsesCache()
        {
            var cachedEnumerable = new CachedEnumerable<int>(new SingleEnumerate());
            for (var i = 0; i < 20; i++)
            {
                var index = i % 5;
                Assert.Equal(index + 1, cachedEnumerable.GetAt(index));
            }
        }

        private sealed class SingleEnumerate : IEnumerable<int>
        {
            private bool _once;

            public IEnumerator<int> GetEnumerator()
            {
                if (_once)
                {
                    throw new InvalidOperationException("Can only enumerate this once!");
                }

                _once = true;
                yield return 1;
                yield return 2;
                yield return 3;
                yield return 4;
                yield return 5;
            }

            IEnumerator IEnumerable.GetEnumerator()
                => GetEnumerator();
        }
    }
}
