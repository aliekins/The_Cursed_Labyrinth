using System;
using System.Collections.Generic;

/**
 * @file PriorityQueue.cs
 * @brief Minimal binary-heap priority queue for Unity versions without
 *        System.Collections.Generic.PriorityQueue.
 * @ingroup Utilities
 * @tparam TElement Stored element type.
 * @tparam TPriority Comparable priority type (lower values are dequeued first).
 */

public class PriorityQueue<TElement, TPriority> where TPriority : IComparable<TPriority>
{
    private readonly List<(TElement Element, TPriority Priority)> _data = new();
    public int Count => _data.Count;

    public void Enqueue(TElement element, TPriority priority)
    {
        _data.Add((element, priority));
        int child_indx = _data.Count - 1;

        while (child_indx > 0)
        {
            int parent_indx = (child_indx - 1) / 2;

            if (_data[child_indx].Priority.CompareTo(_data[parent_indx].Priority) >= 0) break;

            (_data[child_indx], _data[parent_indx]) = (_data[parent_indx], _data[child_indx]);
            child_indx = parent_indx;
        }
    }

    public TElement Dequeue()
    {
        int last_indx = _data.Count - 1;
        var frontItem = _data[0];

        _data[0] = _data[last_indx];

        _data.RemoveAt(last_indx);
        --last_indx;

        int parent_indx = 0;

        while (true)
        {
            int lchild_indx = parent_indx * 2 + 1;
            if (lchild_indx > last_indx) break;

            int rchild_indx = lchild_indx + 1;
            if (rchild_indx <= last_indx && _data[rchild_indx].Priority.CompareTo(_data[lchild_indx].Priority) < 0)
            { 
                lchild_indx = rchild_indx; 
            }

            if (_data[parent_indx].Priority.CompareTo(_data[lchild_indx].Priority) <= 0) break;

            (_data[parent_indx], _data[lchild_indx]) = (_data[lchild_indx], _data[parent_indx]);
            parent_indx = lchild_indx;
        }
        return frontItem.Element;
    }
}