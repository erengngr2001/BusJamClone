using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PriorityQueue<T>
{
    private struct Node
    {
        public T item;
        public int priority;
        public long seq; // to ensure stable ordering
        public Node(T item, int priority, long seq)
        {
            this.item = item;
            this.priority = priority;
            this.seq = seq;
        }
    }

    private List<Node> _heap;
    private long _seqCounter; // to ensure stable ordering

    public PriorityQueue(int initialCapacity = 32)
    {
        _heap = new List<Node>(initialCapacity);
        _seqCounter = 0;
    }

    public int Count()
    {
        return _heap.Count;
    }

    public void Enqueue(T item, int priority)
    {
        _heap.Add(new Node(item, priority, _seqCounter++));
        HeapifyUp(_heap.Count - 1);
    }

    public T Dequeue()
    {
        if (_heap.Count == 0) throw new System.InvalidOperationException("PriorityQueue is empty");
        var first = _heap[0].item;
        var last = _heap[_heap.Count - 1];
        _heap.RemoveAt(_heap.Count - 1);
        if (_heap.Count > 0)
        {
            _heap[0] = last;
            HeapifyDown(0);
        }
        return first;
    }

    public (T, int) Peek()
    {
        if (_heap.Count == 0) throw new System.InvalidOperationException("PriorityQueue is empty");
        var first = _heap[0];
        return (first.item, first.priority);
    }

    public void Clear()
    {
        _heap.Clear();
        _seqCounter = 0;
    }

    private void Swap(int i, int j)
    {
        var temp = _heap[i];
        _heap[i] = _heap[j];
        _heap[j] = temp;
    }

    private int Compare(Node a, Node b)
    {
        int cmp = a.priority.CompareTo(b.priority);
        if (cmp == 0)
            cmp = a.seq.CompareTo(b.seq); // ensure stable ordering
        return cmp;
    }

    private void HeapifyUp(int index)
    {
        while (index > 0)
        {
            int parent = (index - 1) / 2;
            if (Compare(_heap[index], _heap[parent]) >= 0) break;
            Swap(index, parent);
            index = parent;
        }
    }

    private void HeapifyDown(int index)
    {
        int lastIndex = _heap.Count - 1;
        while (true)
        {
            int left = 2 * index + 1;
            int right = 2 * index + 2;
            int smallest = index;
            if (left <= lastIndex && Compare(_heap[left], _heap[smallest]) < 0)
                smallest = left;
            if (right <= lastIndex && Compare(_heap[right], _heap[smallest]) < 0)
                smallest = right;
            if (smallest == index) break;
            Swap(index, smallest);
            index = smallest;
        }
    }
}
