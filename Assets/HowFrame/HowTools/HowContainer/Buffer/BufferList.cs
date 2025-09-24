using System;
using System.Collections;
using System.Collections.Generic;
namespace HowFrame
{

public class BufferList<T> : IEnumerable<T>
{
	private readonly T[] _buffer;
	private int _head;  // ��һ��д��λ��
	private int _count;

	public BufferList(int capacity)
	{
		if (capacity <= 0) throw new ArgumentException("�����������0");
		_buffer = new T[capacity];
	}

	public int Count => _count;
	public int Capacity => _buffer.Length;
	public bool IsFull => _count == Capacity;

	public void Add(T item)
	{
		_buffer[_head] = item;
		_head = (_head + 1) % Capacity;
		if (_count < Capacity) _count++;
	}

	public T this[int index]
	{
		get
		{
			if (index < 0 || index >= _count) throw new ArgumentOutOfRangeException();
			int realIndex = (_head - _count + index + Capacity) % Capacity;
			return _buffer[realIndex];
		}
		set
		{
			if (index < 0 || index >= _count) throw new ArgumentOutOfRangeException();
			int realIndex = (_head - _count + index + Capacity) % Capacity;
			_buffer[realIndex] = value;
		}
	}

	public void Clear()
	{
		_head = 0;
		_count = 0;
		Array.Clear(_buffer, 0, Capacity);
	}

	public IEnumerator<T> GetEnumerator()
	{
		for (int i = 0; i < _count; i++)
			yield return this[i];
	}

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
}
