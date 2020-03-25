using System;
using System.Diagnostics.CodeAnalysis;
using Xunit;

namespace NexusLabs.Collections.Generic.Tests
{
    [ExcludeFromCodeCoverage]
    public sealed class TrieTests
    {
        private readonly Trie _trie;

        public TrieTests()
        {
            _trie = new Trie();
        }

        [InlineData("apple")]
        [InlineData("")]
        [Theory]
        public void Insert_Valid_NoException(string word)
        {
            _trie.Insert(word);
        }

        [Fact]
        public void Insert_Invalid_Exception()
        {
            Assert.Throws<ArgumentNullException>(() => _trie.Insert(null));
        }

        [Fact]
        public void Search_Empty_False()
        {
            var result = _trie.Search("apple");
            Assert.False(
                result,
                $"Unexpected result for '{nameof(Trie.Search)}'.");
        }

        [Fact]
        public void Search_SingleInsertExactMatch_True()
        {
            const string WORD = "apple";
            _trie.Insert(WORD);
            var result = _trie.Search(WORD);
            Assert.True(
                result,
                $"Unexpected result for '{nameof(Trie.Search)}'.");
        }

        [Fact]
        public void Search_SingleInsertIsSubstring_False()
        {
            _trie.Insert("apples");
            var result = _trie.Search("apple");
            Assert.False(
                result,
                $"Unexpected result for '{nameof(Trie.Search)}'.");
        }

        [Fact]
        public void Search_InsertFullInsertSubstringSearchSubstring_True()
        {
            _trie.Insert("apples");
            _trie.Insert("apple");
            var result = _trie.Search("apple");
            Assert.True(
                result,
                $"Unexpected result for '{nameof(Trie.Search)}'.");
        }

        [Fact]
        public void StartsWith_SingleInsertExactMatch_True()
        {
            const string WORD = "apple";
            _trie.Insert(WORD);
            var result = _trie.StartsWith(WORD);
            Assert.True(
                result,
                $"Unexpected result for '{nameof(Trie.StartsWith)}'.");
        }

        [Fact]
        public void StartsWith_SingleInsertIsSubstring_False()
        {
            _trie.Insert("apples");
            var result = _trie.StartsWith("apple");
            Assert.True(
                result,
                $"Unexpected result for '{nameof(Trie.StartsWith)}'.");
        }

        [Fact]
        public void StartsWith_InsertFullInsertSubstringSearchSubstring_True()
        {
            _trie.Insert("apples");
            _trie.Insert("apple");
            var result = _trie.StartsWith("app");
            Assert.True(
                result,
                $"Unexpected result for '{nameof(Trie.StartsWith)}'.");
        }
    }
}
