using System;
using System.Collections.Generic;

public class PriorityQueue<T> where T : SimEvent
{
    private List<T> heap;

    public int Count => heap.Count;

    public PriorityQueue()
    {
        heap = new List<T>();
    }

    public void Enqueue(T item)
    {
        heap.Add(item);
        int i = heap.Count - 1;
        while (i > 0)
        {
            int parent = (i - 1) / 2;
            if (heap[parent].Time <= heap[i].Time)
                break;
            T temp = heap[parent];
            heap[parent] = heap[i];
            heap[i] = temp;
            i = parent;
        }
    }

    public T Dequeue()
    {
        if (heap.Count == 0)
            throw new InvalidOperationException("Queue is empty");

        T result = heap[0];
        int lastIndex = heap.Count - 1;
        heap[0] = heap[lastIndex];
        heap.RemoveAt(lastIndex);
        lastIndex--;

        if (lastIndex > 0)
        {
            int i = 0;
            while (true)
            {
                int smallest = i;
                int left = 2 * i + 1;
                int right = 2 * i + 2;

                if (left <= lastIndex && heap[left].Time < heap[smallest].Time)
                    smallest = left;
                if (right <= lastIndex && heap[right].Time < heap[smallest].Time)
                    smallest = right;

                if (smallest == i)
                    break;

                T temp = heap[i];
                heap[i] = heap[smallest];
                heap[smallest] = temp;
                i = smallest;
            }
        }

        return result;
    }

    public T Peek()
    {
        if (heap.Count == 0)
            throw new InvalidOperationException("Queue is empty");
        return heap[0];
    }
} 