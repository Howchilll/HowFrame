
using System;
using System.Collections;
using System.Collections.Generic;

public class BufferDictionary<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>
{
	private readonly int _capacity;
	private readonly Dictionary<TKey, TValue> _dict = new();
	private  Queue<TKey> _queue = new();

	public BufferDictionary(int capacity)
	{
		if (capacity <= 0) throw new ArgumentException("容量必须大于0");
		_capacity = capacity;
	}

	public int Count => _dict.Count;
	public bool IsFull => _dict.Count == _capacity;

	public void Add(TKey key, TValue value)
	{
		if (_dict.ContainsKey(key))
		{
			_dict[key] = value;
			return;
		}

		if (_dict.Count >= _capacity)
		{
			var oldestKey = _queue.Dequeue();
			_dict.Remove(oldestKey);
		}

		_dict[key] = value;
		_queue.Enqueue(key);
	}

	public TValue this[TKey key]
	{
		get => _dict[key];
		set => Add(key, value); // 覆盖已有 key 或插入新 key
	}

	public bool Remove(TKey key)
	{
		if (_dict.Remove(key))
		{
			_queue = new Queue<TKey>(_queue); // 简单刷新队列，保留顺序
			return true;
		}
		return false;
	}

	public void Clear()
	{
		_dict.Clear();
		_queue.Clear();
	}

	public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => _dict.GetEnumerator();
	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
