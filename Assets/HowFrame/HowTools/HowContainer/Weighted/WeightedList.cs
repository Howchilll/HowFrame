using System;
using System.Collections.Generic;
namespace HowFrame
{

public class WeightedList<T>
{
    private readonly List<(T Item, double Weight)> _items = new();
    private double _totalWeight;
    private readonly Random _random = new();

    public void Add(T item, double weight)
    {
        if (weight <= 0)
            throw new ArgumentException("Ȩ�ر������ 0", nameof(weight));

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
            throw new InvalidOperationException("����Ϊ��");

        double roll = _random.NextDouble() * _totalWeight;
        double cumulative = 0;

        foreach (var (item, weight) in _items)
        {
            cumulative += weight;
            if (roll <= cumulative)
                return item;
        }

        throw new Exception("��ȡʧ�ܣ�Ȩ�ؼ������");
    }

    public int Count => _items.Count;
    public double TotalWeight => _totalWeight;
    public void Clear() { _items.Clear(); _totalWeight = 0; }
}
}
