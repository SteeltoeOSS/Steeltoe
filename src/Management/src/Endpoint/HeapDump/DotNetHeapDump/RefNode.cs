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

[ExcludeFromCodeCoverage]
internal sealed class RefNode
{
    /// <summary>
    /// Gets the first child for the node.  Will return null if there are no children.  
    /// </summary>
    public NodeIndex GetFirstChildIndex()
    {
        var refsToList = m_graph.m_refsForNodes[(int)m_index];

        if (refsToList == RefGraph.NodeListIndex.Empty)
        {
            return NodeIndex.Invalid;
        }

        if (refsToList > 0)        // One element list
        {
            m_cur = -1;
            return (NodeIndex)(refsToList - 1);
        }
        else // refsToList < 0          more than one element.  
        {
            var listIndex = -(int)refsToList - 1;
            var refElem = m_graph.m_links[listIndex];
            m_cur = refElem.NextIdx;
            return refElem.RefIdx;
        }
    }
    /// <summary>
    /// Returns the next child for the node.   Will return NodeIndex.Invalid if there are no more children 
    /// </summary>
    public NodeIndex GetNextChildIndex()
    {
        if (m_cur < 0)
        {
            return NodeIndex.Invalid;
        }

        var refElem = m_graph.m_links[m_cur];
        m_cur = refElem.NextIdx;
        return refElem.RefIdx;
    }

    /// <summary>
    /// Returns the count of children (nodes that reference this node). 
    /// </summary>
    public int ChildCount
    {
        get
        {
            var ret = 0;
            for (NodeIndex childIndex = GetFirstChildIndex(); childIndex != NodeIndex.Invalid; childIndex = GetNextChildIndex())
            {
                ret++;
            }

            return ret;
        }
    }

    public RefGraph Graph { get { return m_graph; } }
    public NodeIndex Index { get { return m_index; } }

    /// <summary>
    /// Returns true if 'node' is a child of 'this'.  childStorage is simply used as temp space 
    /// as was allocated by RefGraph.AllocateNodeStorage
    /// </summary>
    public bool Contains(NodeIndex node)
    {
        for (NodeIndex childIndex = GetFirstChildIndex(); childIndex != NodeIndex.Invalid; childIndex = GetNextChildIndex())
        {
            if (childIndex == node)
            {
                return true;
            }
        }
        return false;
    }

    public override string ToString()
    {
        StringWriter sw = new StringWriter();
        WriteXml(sw);
        return sw.ToString();
    }
    public void WriteXml(TextWriter writer, string prefix = "")
    {
        Debug.Assert(Index != NodeIndex.Invalid);


        writer.Write("{0}<Node Index=\"{1}\" NumChildren=\"{2}\"", prefix, (int)Index, ChildCount);
        var childIndex = GetFirstChildIndex();
        if (childIndex != NodeIndex.Invalid)
        {
            writer.WriteLine(">");
            writer.Write(prefix);
            int i = 0;
            do
            {
                writer.Write(" {0}", childIndex);
                childIndex = GetNextChildIndex();
                i++;
                if (i >= 32)
                {
                    writer.WriteLine();
                    writer.Write(prefix);
                    i = 0;
                }
            } while (childIndex != NodeIndex.Invalid);
            writer.WriteLine(" </Node>");
        }
        else
        {
            writer.WriteLine("/>");
        }
    }

    #region private
    internal RefNode(RefGraph refGraph)
    {
        m_graph = refGraph;
    }

    internal RefGraph m_graph;
    internal NodeIndex m_index;     // My index.  
    internal int m_cur;             // A pointer to where we are in the list of elements (index into m_links)
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
