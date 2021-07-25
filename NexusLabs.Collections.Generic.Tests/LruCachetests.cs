using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Xunit;

namespace NexusLabs.Collections.Generic.Tests
{
    [ExcludeFromCodeCoverage]
    public sealed class LruCacheTests
    {
        [Fact]
        public void Constructor_CapacityTooSmall_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new LruCache<int, int>(0));
        }

        [Fact]
        public void ContainsKey_EntryExists_True()
        {
            var cache = new LruCache<int, int>(1);
            cache.Add(0, 1);
            var actual = cache.ContainsKey(0);
            Assert.True(
                actual,
                $"Unexpected result for '{nameof(LruCache<int, int>.ContainsKey)}'.");
        }

        [Fact]
        public void Add_Limit1Add1_NothingEvictedItemPresent()
        {
            var cache = new LruCache<string, int>(1);
            cache.Add("first", 1);

            Assert.True(
                cache.ContainsKey("first"),
                $"Expecting to contain 'first'.");
            Assert.Equal(1, cache["first"]);
        }

        [Fact]
        public void Add_Limit1Add2_FirstEntryEvicted()
        {
            var cache = new LruCache<string, int>(1);
            cache.Add("first", 1);
            cache.Add("second", 2);

            Assert.False(
                cache.ContainsKey("first"),
                $"Not expecting to contain 'first'.");
            Assert.True(
                cache.ContainsKey("second"),
                $"Expecting to contain 'second'.");
            Assert.Equal(2, cache["second"]);
        }

        [Fact]
        public void Indexer_ItemDoesNotExist_KeyNotFoundExceptionWithKeyName()
        {
            var cache = new LruCache<string, int>(1);
            var guidKey = Guid.NewGuid().ToString();
            var exception = Assert.Throws<KeyNotFoundException>(() => cache[guidKey]);
            Assert.Contains(guidKey, exception.Message);
        }
    }
}
