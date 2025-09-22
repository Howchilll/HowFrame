using System;
using System.Collections.Generic;

public class WeightedDictionary<TKey, TValue>
{
	private readonly Dictionary<TKey, (TValue Value, double Weight)> _items = new();
	private double _totalWeight;
	private readonly Random _random = new();

	public void Add(TKey key, TValue value, double weight)
	{
		if (weight <= 0)
			throw new ArgumentException("权重必须大于 0", nameof(weight));

		if (_items.TryGetValue(key, out var old))
		{
			_totalWeight -= old.Weight;
		}

		_items[key] = (value, weight);
		_totalWeight += weight;
	}

	public bool Remove(TKey key)
	{
		if (_items.TryGetValue(key, out var old))
		{
			_items.Remove(key);
			_totalWeight -= old.Weight;
			return true;
		}
		return false;
	}

	public TValue Next()
	{
		if (_items.Count == 0)
			throw new InvalidOperationException("集合为空");

		double roll = _random.NextDouble() * _totalWeight;
		double cumulative = 0;

		foreach (var kv in _items)
		{
			cumulative += kv.Value.Weight;
			if (roll <= cumulative)
				return kv.Value.Value;
		}

		throw new Exception("抽取失败：权重计算错误");
	}

	public TValue Get()
	{
		if (_items.Count == 0)
			throw new InvalidOperationException("集合为空");

		double roll = _random.NextDouble() * _totalWeight;
		double cumulative = 0;

		foreach (var kv in _items)
		{
			cumulative += kv.Value.Weight;
			if (roll <= cumulative)
			{
				Remove(kv.Key);
				return kv.Value.Value;
			}
		}

		throw new Exception("抽取失败：权重计算错误");
	}

	public TValue Choose(TKey key)
	{
		if (_items.TryGetValue(key, out var item))
			return item.Value;

		throw new KeyNotFoundException($"Key {key} 不存在");
	}

	public TValue ChooseAndRemove(TKey key)
	{
		if (_items.TryGetValue(key, out var item))
		{
			Remove(key);
			return item.Value;
		}

		throw new KeyNotFoundException($"Key {key} 不存在");
	}

	public int Count => _items.Count;
	public double TotalWeight => _totalWeight;
	public void Clear() { _items.Clear(); _totalWeight = 0; }
}



public class WeightedRandomList<T>
{
	private readonly List<(T Item, double Weight)> _items = new();
	private double _totalWeight;
	private readonly Random _random = new();

	public void Add(T item, double weight)
	{
		if (weight <= 0)
			throw new ArgumentException("权重必须大于 0", nameof(weight));

		_items.Add((item, weight));
		_totalWeight += weight;
	}

	public bool RemoveAt(int index)
	{
		if (index < 0 || index >= _items.Count) return false;

		_totalWeight -= _items[index].Weight;
		_items.RemoveAt(index);
		return true;
	}

	public T Next()
	{
		if (_items.Count == 0)
			throw new InvalidOperationException("集合为空");

		double roll = _random.NextDouble() * _totalWeight;
		double cumulative = 0;

		foreach (var (item, weight) in _items)
		{
			cumulative += weight;
			if (roll <= cumulative)
				return item;
		}

		throw new Exception("抽取失败：权重计算错误");
	}

	public int Count => _items.Count;
	public double TotalWeight => _totalWeight;
	public void Clear() { _items.Clear(); _totalWeight = 0; }
}
