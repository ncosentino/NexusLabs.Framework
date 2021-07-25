using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace NexusLabs.Collections.Generic
{
	public sealed class LruCache<TKey, TValue> : ICache<TKey, TValue>
	{
		private readonly object _lock;
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
			_lock = new object();
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
			lock (_lock)
			{
				if (_map.TryGetValue(key, out var entry))
				{
					RemoveNode(entry);
					AddAtTop(entry);
					value = entry.Value;
					return true;
				}
			}

			value = default;
			return false;
		}

		public TValue Get(TKey key)
		{
			if (TryGet(key, out var value))
			{
				return value;
			}

			throw new KeyNotFoundException(
				$"Could not find key '{key}'.");
		}

		public void Add(TKey key, TValue value)
		{
			lock (_lock)
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

					if (_map.Count >= Capacity)
					{
						var end = _end;

						_map.Remove(end.Key);
						RemoveNode(end);
						AddAtTop(newnode);
					}
					else
					{
						AddAtTop(newnode);
					}

					_map[key] = newnode;
				}
			}
		}

		public IEnumerator<TValue> GetEnumerator() => _map
			.Values
			.Select(x => x.Value)
			.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		private void AddAtTop(Entry node)
		{
			var start = _start;
			var end = _end;

			node.Right = start;
			node.Left = null;
			if (start != null)
			{
				start.Left = node;
			}

			start = node;
			if (end == null)
			{
				_end = start;
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
