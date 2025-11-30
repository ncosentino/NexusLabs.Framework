using System;
using System.Collections.Generic;

namespace NexusLabs.Collections.Generic;

public sealed class Trie : ITrie
{
    private readonly Node _root;

    public Trie()
    {
        _root = new Node(null);
    }

    public void Insert(string word)
    {
        ArgumentNullException.ThrowIfNull(word);

        var currentNode = _root;
        foreach (var c in word)
        {
            if (!currentNode
                .Children
                .TryGetValue(c, out var childNode))
            {
                childNode = new Node(c);
                currentNode.Children[c] = childNode;
            }

            currentNode = childNode;
        }

        currentNode.EndMarker = true;
    }

    public bool Search(string word)
    {
        ArgumentNullException.ThrowIfNull(word);

        var currentNode = _root;
        foreach (var c in word)
        {
            if (!currentNode
                .Children
                .TryGetValue(c, out var childNode))
            {
                return false;
            }

            currentNode = childNode;
        }

        return currentNode.EndMarker;
    }

    public bool StartsWith(string prefix)
    {
        ArgumentNullException.ThrowIfNull(prefix);

        var currentNode = _root;
        foreach (var c in prefix)
        {
            if (!currentNode
                .Children
                .TryGetValue(c, out var childNode))
            {
                return false;
            }

            currentNode = childNode;
        }

        return true;
    }

    /// <summary>
    /// The node structure for the <see cref="Trie"/>.
    /// </summary>
    /// <remarks>
    /// This node implementation uses a dictionary because typically in 
    /// Trie discussions the English alphabet comes up, which is 26
    /// characters. Using an array to be able to point to 26 characters is
    /// only 26 pointers, so it's a bit expensive per node for space, but
    /// array access/assignment is instance. A unicode string can have far
    /// more than 26 characters, so each node representing its children by
    /// an array is irresponsible for space. Dictionaries do have great
    /// time complexity for insertion and access.
    /// </remarks>
    private sealed class Node
    {
        public Node(char? value)
        {
            Children = new Dictionary<char, Node>();
            Value = value;
        }

        public Dictionary<char, Node> Children { get; }

        public char? Value { get; }

        public bool EndMarker { get; set; }
    }
}
