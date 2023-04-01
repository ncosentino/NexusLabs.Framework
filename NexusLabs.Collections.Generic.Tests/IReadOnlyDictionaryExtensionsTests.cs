using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

using Xunit;

namespace NexusLabs.Collections.Generic.Tests
{
    [ExcludeFromCodeCoverage]
    public sealed class IReadOnlyDictionaryExtensionsTests
    {
        [Fact]
        public void AssumeAsFrozenDictionary_CaseSensitiveStrings_UseOriginalComparer()
        {
            Dictionary<string, string> dict = new()
            {
                ["Key1"] = "Val1",
                ["Key2"] = "Val2",
            };
            var frozen = dict.AssumeAsFrozenDictionary();
            Assert.Equal(2, frozen.Count);
            Assert.Contains("Key1", frozen);
            Assert.Contains("Key2", frozen);
            Assert.DoesNotContain("KEY1", frozen);
            Assert.DoesNotContain("KEY2", frozen);
        }

        [Fact]
        public void AssumeAsFrozenDictionary_CaseInsensitiveStringsBeforeConversion_InheritsComparer()
        {
            Dictionary<string, string> dict = new(StringComparer.OrdinalIgnoreCase)
            {
                ["Key1"] = "Val1",
                ["Key2"] = "Val2",
            };
            var frozen = dict.AssumeAsFrozenDictionary();
            Assert.Equal(2, frozen.Count);
            Assert.Contains("Key1", frozen);
            Assert.Contains("Key2", frozen);
            Assert.Contains("KEY1", frozen);
            Assert.Contains("KEY2", frozen);
        }

        [Fact]
        public void AssumeAsFrozenDictionary_CaseSensitiveBeforeInsensitiveAfterConversion_OverridesComparer()
        {
            Dictionary<string, string> dict = new(StringComparer.Ordinal)
            {
                ["Key1"] = "Val1",
                ["Key2"] = "Val2",
            };
            var frozen = dict.AssumeAsFrozenDictionary(StringComparer.OrdinalIgnoreCase);
            Assert.Equal(2, frozen.Count);
            Assert.Contains("Key1", frozen);
            Assert.Contains("Key2", frozen);
            Assert.Contains("KEY1", frozen);
            Assert.Contains("KEY2", frozen);
        }

        [Fact]
        public void AssumeAsFrozenDictionary_CaseInsensitiveStringsAfterConversion_UseNewComparer()
        {
            Dictionary<string, string> dict = new()
            {
                ["Key1"] = "Val1",
                ["Key2"] = "Val2",
            };
            var frozen = dict.AssumeAsFrozenDictionary(StringComparer.OrdinalIgnoreCase);
            Assert.Equal(2, frozen.Count);
            Assert.Contains("Key1", frozen);
            Assert.Contains("Key2", frozen);
            Assert.Contains("KEY1", frozen);
            Assert.Contains("KEY2", frozen);
        }
    }
}
