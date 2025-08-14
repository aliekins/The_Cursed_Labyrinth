using System;
using System.Collections.Generic;

/// <summary>
/// Binary heap priority queue for Unity versions without System.Collections.Generic.PriorityQueue.
/// Allows fast access to the element with the smallest priority value.
/// </summary>
/// <typeparam name="TElement">Type of stored element</typeparam>
/// <typeparam name="TPriority">Type of priority (must be IComparable)</typeparam>
public class PriorityQueue<TElement, TPriority> where TPriority : IComparable<TPriority>
{
    // Internal storage
    private readonly List<(TElement Element, TPriority Priority)> _data = new();

    // Number of elements currently in the queue.
    public int Count => _data.Count;

    /// <summary>
    /// Inserts an element with the given priority into the priority queue
    /// </summary>
    public void Enqueue(TElement element, TPriority priority)
    {
        // Add the new element at the end of the heap array
        _data.Add((element, priority));

        // (start at the last inserted element)
        int child_indx = _data.Count - 1;

        // Bubble up until the heap property is restored
        while (child_indx > 0)
        {
            int parent_indx = (child_indx - 1) / 2;

            // Upon satisfying heap property - stop 
            if (_data[child_indx].Priority.CompareTo(_data[parent_indx].Priority) >= 0) break;

            // Otherwise, swap
            (_data[child_indx], _data[parent_indx]) = (_data[parent_indx], _data[child_indx]);

            // Continue at parent's position
            child_indx = parent_indx;
        }
    }

    /// <summary>
    /// Removes and returns the element with the smallest priority value
    /// </summary>
    public TElement Dequeue()
    {
        // Index of the last element in the heap
        int last_indx = _data.Count - 1;

        // The root element (smallest priority) is the one returned
        var frontItem = _data[0];

        // Move the last element to the root
        _data[0] = _data[last_indx];

        // Remove the last element from the list
        _data.RemoveAt(last_indx);
        --last_indx;

        // (start from root)
        int parent_indx = 0;

        // Bubble down until the heap property is restored
        while (true)
        {
            // Left child
            int lchild_indx = parent_indx * 2 + 1;

            if (lchild_indx > last_indx) break;  // There are no children - stop

            // Right child 
            int rchild_indx = lchild_indx + 1;

            // Right child exists and has smaller priority - use instead of left child
            if (rchild_indx <= last_indx && _data[rchild_indx].Priority.CompareTo(_data[lchild_indx].Priority) < 0)
            { 
                lchild_indx = rchild_indx; 
            }

            // Parent priority already smaller or equal to the smaller child - stop
            if (_data[parent_indx].Priority.CompareTo(_data[lchild_indx].Priority) <= 0) break;

            // Swap parent with smaller child
            (_data[parent_indx], _data[lchild_indx]) = (_data[lchild_indx], _data[parent_indx]);

            // Continue at child's position 
            parent_indx = lchild_indx;
        }
        return frontItem.Element;
    }
}