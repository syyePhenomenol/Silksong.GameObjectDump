namespace Silksong.GameObjectDump.Logging;

using System;
using System.Collections.Generic;

public class PriorityQueue<T>
{
    private readonly List<(T Item, int Priority)> _heap = [];

    public int Count => _heap.Count;
    public bool IsEmpty => _heap.Count == 0;

    public void Enqueue(T item, int priority)
    {
        _heap.Add((item, priority));
        HeapifyUp(_heap.Count - 1);
    }

    public T Dequeue()
    {
        if (_heap.Count == 0) throw new InvalidOperationException("Queue is empty");

        var (Item, _) = _heap[0];

        // Move last to root, shrink, then heapify down
        _heap[0] = _heap[^1];
        _heap.RemoveAt(_heap.Count - 1);

        if (_heap.Count > 0)
            HeapifyDown(0);

        return Item;
    }

    public (T Item, int Priority) Peek()
    {
        if (_heap.Count == 0) throw new InvalidOperationException("Queue is empty");
        return _heap[0];
    }

    private void HeapifyUp(int i)
    {
        while (i > 0)
        {
            int parent = (i - 1) / 2;
            if (_heap[i].Priority >= _heap[parent].Priority) break;

            (_heap[i], _heap[parent]) = (_heap[parent], _heap[i]);
            i = parent;
        }
    }

    private void HeapifyDown(int i)
    {
        int lastIndex = _heap.Count - 1;
        while (true)
        {
            int left = 2 * i + 1;
            int right = 2 * i + 2;
            int smallest = i;

            if (left <= lastIndex && _heap[left].Priority < _heap[smallest].Priority)
                smallest = left;

            if (right <= lastIndex && _heap[right].Priority < _heap[smallest].Priority)
                smallest = right;

            if (smallest == i) break;

            (_heap[i], _heap[smallest]) = (_heap[smallest], _heap[i]);
            i = smallest;
        }
    }
}
