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

#if false
namespace Experimental
{
    /// <summary>
    /// code:PagedGrowableArray is an array (has an index operation) but can efficiently represent
    /// either very large arrays as well as sparse arrays.  
    /// </summary>
    public struct PagedGrowableArray<T>
    {
        public PagedGrowableArray(int initialSize)
        {
            Debug.Assert(initialSize > 0);
            var numPages = (initialSize + pageSize - 1) / pageSize;
            m_count = 0;
            m_pages = new T[numPages][];
        }
        public T this[int index]
        {
            get
            {
                Debug.Assert((uint)index < (uint)m_count);
                return m_pages[index / pageSize][index % pageSize];
            }
            set
            {
                Debug.Assert((uint)index < (uint)m_count);
                m_pages[index / pageSize][index % pageSize] = value;
            }
        }
        public int Count
        {
            get { return m_count; }
            set
            {
                Debug.Assert(false, "Not completed");
                if (value > m_count)
                {
                    var onLastPage = m_count % pageSize;
                    if (onLastPage != 0)
                    {
                        var lastPage = m_pages[m_count / pageSize];
                        var nullOnLastPage = Math.Min(value - m_count, pageSize);
                        while (nullOnLastPage > onLastPage)
                        {
                            --nullOnLastPage;
                            lastPage[nullOnLastPage] = default(T);
                        }
                    }
                }
                else
                {
                    // Release unused pages
                    while (m_count > value)
                    {

                    }
                }
                m_count = value;
            }
        }
        /// <summary>
        /// Append the value to the end of the array.  
        /// </summary>
        /// <param name="value"></param>
        public void Add(T value)
        {
            if (m_count % pageSize == 0)
            {
                var pageIndex = m_count / pageSize;
                if (pageIndex >= m_pages.Length)
                {
                    var newPageLength = m_pages.Length * 2;
                    var newPages = new T[newPageLength][];
                    Array.Copy(m_pages, newPages, m_pages.Length);
                    m_pages = newPages;
                }
                if (m_pages[pageIndex] == null)
                    m_pages[pageIndex] = new T[pageSize];
            }

            m_pages[m_count / pageSize][m_count % pageSize] = value;
            m_count++;
        }

#region private
        const int pageSize = 4096;

        T[][] m_pages;
        int m_count;
#endregion
    }

    class CompressedGrowableArray : IFastSerializable
    {
        public CompressedGrowableArray()
        {
            m_pages = new Page[256];
        }
        public long this[int index]
        {
            get
            {
                return m_pages[index >> 8][(byte)index];
            }
        }
        /// <summary>
        /// Append the value to the end of the array.  
        /// </summary>
        /// <param name="value"></param>
        public void Add(long value)
        {
            if (m_numPages >= m_pages.Length)
            {
                int newLength = m_pages.Length * 2;
                var newArray = new Page[newLength];
                Array.Copy(m_pages, newArray, m_pages.Length);
                m_pages = newArray;

            }
            // m_pages[m_numPages-1].Add(value);
        }

#region private
        void IFastSerializable.ToStream(Serializer serializer)
        {
            serializer.Write(m_numPages);
            for (int i = 0; i < m_numPages; i++)
                serializer.Write(m_pages[i]);
        }
        void IFastSerializable.FromStream(Deserializer deserializer)
        {
            deserializer.Read(out m_numPages);
            for (int i = 0; i < m_numPages; i++)
                deserializer.Read(out m_pages[i]);
        }

        /// <summary>
        /// A page represents 256 entries in the table.   For each page we remember a 'm_baseValue' and 
        /// we delta encode.  If the offset fit 15 bits you simply add the offset to the base value
        /// Otherwise what is in the table is an offset into the 'm_compressedValues' blob and the offset
        /// is encoded as a variable length signed number.  
        /// </summary>
        class Page : IFastSerializable
        {
            Page(long baseValue)
            {
                m_indexOrOffset = new short[256];
                m_baseValue = baseValue;
            }
            public long this[byte index]
            {
                get
                {
                    short val = m_indexOrOffset[index];
                    if ((val & 0x8000) != 0)
                        return val + m_baseValue;
                    return ValueFromIndex(val);
                }
            }

#region private
            private long ValueFromIndex(short val)
            {
                return m_baseValue + ReadCompressedInt(val & ~0x8000);
            }
            private long ReadCompressedInt(int blobIndex)
            {
                long ret = 0;
                byte b = m_compressedValues[blobIndex++];
                int asInt = b << 25 >> 25;
                ret = asInt;
#if DEBUG
                for (int i = 0; ; i++)
                {
                    Debug.Assert(i < 5);
#else
                for (; ; )
                {
#endif
                    if ((b & 0x80) == 0)
                        return ret;
                    ret <<= 7;
                    b = m_compressedValues[blobIndex++];
                    ret += (b & 0x7f);
                }
            }
            private int WriteCompressedInt(long value)
            {
                throw new NotImplementedException();
            }

            void IFastSerializable.ToStream(Serializer serializer)
            {
                serializer.Write(m_baseValue);
                for (int i = 0; i < 256; i++)
                    serializer.Write(m_indexOrOffset[i]);
                serializer.Write(m_compressedValuesIndex);
                for (int i = 0; i < m_compressedValuesIndex; i++)
                    serializer.Write(m_compressedValues[i]);
            }
            void IFastSerializable.FromStream(Deserializer deserializer)
            {
                deserializer.Read(out m_baseValue);
                for (int i = 0; i < 256; i++)
                    m_indexOrOffset[i] = deserializer.ReadInt16();

                deserializer.Read(out m_compressedValuesIndex);
                if (m_compressedValuesIndex != 0)
                {
                    m_compressedValues = new byte[m_compressedValuesIndex];
                    for (int i = 0; i < m_compressedValuesIndex; i++)
                        m_compressedValues[i] = deserializer.ReadByte();
                }
            }

            long m_baseValue;                  // All values are relative to this.  
            short[] m_indexOrOffset;           // table of value (either offsets or indexes into the compressed blobs)

            byte[] m_compressedValues;          // If all the values are not within 32K of the base, then store them here.  
            int m_compressedValuesIndex;        // Next place to write to in m_compressedValues
#endregion
        }

        int m_numPages;
        Page[] m_pages;
#endregion
    }
}

#endif
