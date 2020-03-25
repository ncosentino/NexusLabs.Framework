using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace NexusLabs.Collections.Generic
{
	public sealed class LruCache<TKey, TValue> : ICache<TKey, TValue>
	{
		private readonly Dictionary<TKey, Entry> _map;
		private Entry _start;
		private Entry _end;

		public LruCache(int capacity)
		{
			if (capacity < 1)
			{
				throw new ArgumentException(
					"Capacity cannot be less than 1.",
					nameof(capacity));
			}

			Capacity = capacity;
			_map = new Dictionary<TKey, Entry>();
		}

		public int Capacity { get; }

		public int Count => _map.Count;

		public TValue this[TKey key]
		{
			get => Get(key);
		}

		public bool ContainsKey(TKey key) => _map.ContainsKey(key);

		public bool TryGet(TKey key, out TValue value)
		{
			if (_map.TryGetValue(key, out var entry))
			{
				RemoveNode(entry);
				AddAtTop(entry);
				value = entry.Value;
				return true;
			}

			value = default;
			return false;
		}

		public TValue Get(TKey key)
		{
			if (_map.TryGetValue(key, out var entry))
			{
				RemoveNode(entry);
				AddAtTop(entry);
				return entry.Value;
			}

			throw new KeyNotFoundException(
				$"Could not find key '{key}'.");
		}

		public void Add(TKey key, TValue value)
		{
			if (_map.TryGetValue(key, out var entry))
			{
				entry.Value = value;
				RemoveNode(entry);
				AddAtTop(entry);
			}
			else
			{
				var newnode = new Entry
				{
					Left = null,
					Right = null,
					Value = value,
					Key = key
				};

				if (_map.Count > Capacity)
				{
					_map.Remove(_end.Key);
					RemoveNode(_end);
					AddAtTop(newnode);
				}
				else
				{
					AddAtTop(newnode);
				}

				_map[key] = newnode;
			}
		}

		public IEnumerator<TValue> GetEnumerator() => _map
			.Values
			.Select(x => x.Value)
			.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		private void AddAtTop(Entry node)
		{
			node.Right = _start;
			node.Left = null;
			if (_start != null)
			{
				_start.Left = node;
			}

			_start = node;
			if (_end == null)
			{
				_end = _start;
			}
		}

		private void RemoveNode(Entry node)
		{
			if (node.Left != null)
			{
				node.Left.Right = node.Right;
			}
			else
			{
				_start = node.Right;
			}

			if (node.Right != null)
			{
				node.Right.Left = node.Left;
			}
			else
			{
				_end = node.Left;
			}
		}

		private sealed class Entry
		{
			public TValue Value { get; set; }

			public TKey Key { get; set; }

			public Entry Left { get; set; }

			public Entry Right { get; set; }
		}
	}
}
