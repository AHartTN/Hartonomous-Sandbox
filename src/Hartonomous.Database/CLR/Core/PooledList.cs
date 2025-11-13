using System;
using System.Collections.Generic;

namespace SqlClrFunctions.Core;

/// <summary>
/// Allocation-conscious list for SQL CLR aggregates.
/// Simple array-based list for .NET Framework 4.8.1 compatibility.
/// </summary>
internal struct PooledList<T>
{
    private const int DefaultCapacity = 8;
    private T[] _items;
    private int _count;

    internal int Count => _count;
    internal bool IsEmpty => _count == 0;

    internal ref T this[int index]
    {
        get
        {
            if (index < 0 || index >= _count || _items == null)
                throw new ArgumentOutOfRangeException(nameof(index));
            return ref _items[index];
        }
    }

    internal T[] ToArray()
    {
        if (_count == 0) return Array.Empty<T>();
        var result = new T[_count];
        Array.Copy(_items, result, _count);
        return result;
    }

    internal void Add(T item)
    {
        EnsureCapacity(_count + 1);
        _items[_count++] = item;
    }

    internal void AddRange(T[] items)
    {
        if (items == null || items.Length == 0)
            return;

        EnsureCapacity(_count + items.Length);
        Array.Copy(items, 0, _items, _count, items.Length);
        _count += items.Length;
    }

    internal void Reserve(int size) => EnsureCapacity(size);

    internal void RemoveAt(int index)
    {
        if (index < 0 || index >= _count)
            throw new ArgumentOutOfRangeException(nameof(index));

        _count--;
        if (index < _count)
            Array.Copy(_items, index + 1, _items, index, _count - index);

        _items[_count] = default(T);
    }

    internal void RemoveLast()
    {
        if (_count == 0)
            return;
        _count--;
        _items[_count] = default(T);
    }

    internal void Sort(Comparison<T> comparison)
    {
        if (_count <= 1)
            return;
        Array.Sort(_items, 0, _count, Comparer<T>.Create(comparison));
    }

    internal void Clear(bool clearItems = false)
    {
        if (_items != null && clearItems)
        {
            Array.Clear(_items, 0, _count);
        }
        _count = 0;
    }

    private void EnsureCapacity(int size)
    {
        if (_items == null)
        {
            int capacity = Math.Max(DefaultCapacity, size);
            _items = new T[capacity];
            return;
        }

        if (size <= _items.Length)
            return;

        int newCapacity = _items.Length;
        while (newCapacity < size)
            newCapacity *= 2;

        var newArray = new T[newCapacity];
        Array.Copy(_items, 0, newArray, 0, _count);
        _items = newArray;
    }
}
