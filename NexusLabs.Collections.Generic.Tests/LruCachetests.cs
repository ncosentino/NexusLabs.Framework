using System;
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
    }
}
