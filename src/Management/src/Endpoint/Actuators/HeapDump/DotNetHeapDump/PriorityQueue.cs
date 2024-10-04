// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#pragma warning disable
using FastSerialization;    // For IStreamReader
using Graphs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Security;
using Address = System.UInt64;
using System.Diagnostics.CodeAnalysis;

// Copy of version in Microsoft/Diagnostics
// All classes made internal

// Copy of version in Microsoft/PerfView

/// <summary>
/// TODO FIX NOW put in its own file.  
/// A priority queue, specialized to be a bit more efficient than a generic version would be. 
/// </summary>
[ExcludeFromCodeCoverage]
internal sealed class PriorityQueue
{
    public PriorityQueue(int initialSize = 32)
    {
        m_heap = new DataItem[initialSize];
    }
    public int Count { get; private set; }

    public void Enqueue(NodeIndex item, float priority)
    {
        var idx = Count;
        if (idx >= m_heap.Length)
        {
            var newArray = new DataItem[m_heap.Length * 3 / 2 + 8];
            Array.Copy(m_heap, newArray, m_heap.Length);
            m_heap = newArray;
        }
        m_heap[idx].value = item;
        m_heap[idx].priority = priority;
        Count = idx + 1;
        for (; ; )
        {
            var parent = idx / 2;
            if (m_heap[parent].priority >= m_heap[idx].priority)
            {
                break;
            }

            (m_heap[idx], m_heap[parent]) = (m_heap[parent], m_heap[idx]);

            if (parent == 0)
            {
                break;
            }

            idx = parent;
        }
        // CheckInvariant();
    }
    public NodeIndex Dequeue(out float priority)
    {
        Debug.Assert(Count > 0);

        var ret = m_heap[0].value;
        priority = m_heap[0].priority;
        --Count;
        m_heap[0] = m_heap[Count];
        var idx = 0;
        for (; ; )
        {
            var childIdx = idx * 2;
            var largestIdx = idx;
            if (childIdx < Count && m_heap[childIdx].priority > m_heap[largestIdx].priority)
            {
                largestIdx = childIdx;
            }

            childIdx++;
            if (childIdx < Count && m_heap[childIdx].priority > m_heap[largestIdx].priority)
            {
                largestIdx = childIdx;
            }

            if (largestIdx == idx)
            {
                break;
            }

            (m_heap[idx], m_heap[largestIdx]) = (m_heap[largestIdx], m_heap[idx]);

            idx = largestIdx;
        }
        // CheckInvariant();
        return ret;
    }

    #region private
#if DEBUG
    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.AppendLine("<PriorityQueue Count=\"").Append(Count).Append("\">").AppendLine();

        // Sort the items in descending order 
        var items = new List<DataItem>(Count);
        for (int i = 0; i < Count; i++)
            items.Add(m_heap[i]);
        items.Sort((x, y) => y.priority.CompareTo(x.priority));
        if (items.Count > 0)
            Debug.Assert(items[0].value == m_heap[0].value);

        foreach (var item in items)
            sb.Append("{").Append((int)item.value).Append(", ").Append(item.priority.ToString("f1")).Append("}").AppendLine();
        sb.AppendLine("</PriorityQueue>");
        return sb.ToString();
    }
#endif

    private struct DataItem
    {
        public DataItem(NodeIndex value, float priority) { this.value = value; this.priority = priority; }
        public float priority;
        public NodeIndex value;
    }
    [Conditional("DEBUG")]
    private void CheckInvariant()
    {
        for (int idx = 1; idx < Count; idx++)
        {
            var parentIdx = idx / 2;
            Debug.Assert(m_heap[parentIdx].priority >= m_heap[idx].priority);
        }
    }

    // In this array form a tree where each child of i is at 2i and 2i+1.   Each child is 
    // less than or equal to its parent.  
    private DataItem[] m_heap;

    #endregion
}
