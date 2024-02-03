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
/// A RefGraph is derived graph where each node's children are the set of nodes in the original graph 
/// which refer that node (that is A -> B then in refGraph B -> A).   
/// 
/// The NodeIndexes in the refGraph match the NodeIndexes in the original graph.  Thus after creating
/// a refGraph it is easy to answer the question 'who points at me' of the original graph.  
/// 
/// When create the RefGraph the whole reference graph is generated on the spot (thus it must traverse
/// the whole of the original graph) and the size of the resulting RefGraph is  about the same size as the  
/// original graph. 
/// 
/// Thus this is a fairly expensive thing to create.  
/// </summary>
[ExcludeFromCodeCoverage]
internal sealed class RefGraph
{
    public RefGraph(Graph graph)
    {
        m_refsForNodes = new NodeListIndex[(int)graph.NodeIndexLimit];
        // We guess that we need about 1.5X as many slots as there are nodes.   This seems a conservative estimate. 
        m_links = new GrowableArray<RefElem>((int)graph.NodeIndexLimit * 3 / 2);

        var nodeStorage = graph.AllocNodeStorage();
        for (NodeIndex nodeIndex = 0; nodeIndex < graph.NodeIndexLimit; nodeIndex++)
        {
            var node = graph.GetNode(nodeIndex, nodeStorage);
            for (var childIndex = node.GetFirstChildIndex(); childIndex != NodeIndex.Invalid; childIndex = node.GetNextChildIndex())
            {
                AddRefsTo(childIndex, nodeIndex);
            }
        }

        // Sadly, this check is too expensive even for DEBUG 
#if false 
        CheckConsistancy(graph);
#endif
    }
    /// <summary>
    /// Allocates nodes to be used as storage for methods like code:GetNode, code:RefNode.GetFirstChild and code:RefNode.GetNextChild
    /// </summary>
    public RefNode AllocNodeStorage() { return new RefNode(this); }

    /// <summary>
    /// Given an arbitrary code:NodeIndex that identifies the node, Get a code:Node object.  
    /// 
    /// This routine does not allocated but uses the space passed in by 'storage.  
    /// 'storage' should be allocated with code:AllocNodeStorage, and should be aggressively reused.  
    /// </summary>
    public RefNode GetNode(NodeIndex nodeIndex, RefNode storage)
    {
        Debug.Assert(storage.m_graph == this);
        storage.m_index = nodeIndex;
        return storage;
    }

    /// <summary>
    /// This is for debugging 
    /// </summary>
    /// <param name="nodeIndex"></param>
    /// <returns></returns>
    public RefNode GetNode(NodeIndex nodeIndex)
    {
        return GetNode(nodeIndex, AllocNodeStorage());
    }

    #region private
#if DEBUG
    private void CheckConsitancy(Graph graph)
    {
        // This double check is pretty expensive for large graphs (nodes that have large fan-in or fan-out).  
        var nodeStorage = graph.AllocNodeStorage();
        var refStorage = AllocNodeStorage();
        for (NodeIndex nodeIdx = 0; nodeIdx < graph.NodeIndexLimit; nodeIdx++)
        {
            // If Node -> Ref then the RefGraph has a pointer from Ref -> Node 
            var node = graph.GetNode(nodeIdx, nodeStorage);
            for (var childIndex = node.GetFirstChildIndex(); childIndex != NodeIndex.Invalid; childIndex = node.GetNextChildIndex())
            {
                var refsForChild = GetNode(childIndex, refStorage);
                if (!refsForChild.Contains(nodeIdx))
                {
                    _ = node.ToString();
                    _ = refsForChild.ToString();
                    Debug.Assert(false);
                }
            }

            // If the refs graph has a pointer from Ref -> Node then the original graph has a arc from Node ->Ref
            var refNode = GetNode(nodeIdx, refStorage);
            for (var childIndex = refNode.GetFirstChildIndex(); childIndex != NodeIndex.Invalid; childIndex = refNode.GetNextChildIndex())
            {
                var nodeForChild = graph.GetNode(childIndex, nodeStorage);
                if (!nodeForChild.Contains(nodeIdx))
                {
                    _ = nodeForChild.ToString();
                    _ = refNode.ToString();
                    Debug.Assert(false);
                }
            }
        }
    }
#endif

    /// <summary>
    /// Add the fact that 'refSource' refers to refTarget.
    /// </summary>
    private void AddRefsTo(NodeIndex refTarget, NodeIndex refSource)
    {
        NodeListIndex refsToList = m_refsForNodes[(int)refTarget];

        // We represent singles as the childIndex itself.  This is a very common case, so it is good that it is efficient. 
        if (refsToList == NodeListIndex.Empty)
        {
            m_refsForNodes[(int)refTarget] = (NodeListIndex)(refSource + 1);
        }
        else if (refsToList > 0)        // One element list
        {
            var existingChild = (NodeIndex)(refsToList - 1);
            m_refsForNodes[(int)refTarget] = (NodeListIndex)(-AddLink(refSource, AddLink(existingChild)) - 1);
        }
        else // refsToList < 0          more than one element.  
        {
            var listIndex = -(int)refsToList - 1;
            m_refsForNodes[(int)refTarget] = (NodeListIndex)(-AddLink(refSource, listIndex) - 1);
        }
    }

    /// <summary>
    /// A helper function for AddRefsTo.  Allocates a new cell from m_links and initializes its two fields 
    /// (the child index field and 'rest' field), and returns the index (pointer) to the new cell.  
    /// </summary>
    private int AddLink(NodeIndex refIdx, int nextIdx = -1)
    {
        var ret = m_links.Count;
        m_links.Add(new RefElem(refIdx, nextIdx));
        return ret;
    }

    /// <summary>
    ///  Logically a NodeListIndex represents a list of node indexes.   However it is heavily optimized
    ///  to avoid overhead.   0 means empty, a positive number is the NodeIndex+1 and a negative number 
    ///  is index in m_links - 1.  
    /// </summary>
    internal enum NodeListIndex { Empty = 0 };

    /// <summary>
    /// RefElem is a linked list cell that is used to store lists of children that are larger than 1.
    /// </summary>
    internal struct RefElem
    {
        public RefElem(NodeIndex refIdx, int nextIdx) { RefIdx = refIdx; NextIdx = nextIdx; }
        public NodeIndex RefIdx;           // The reference
        public int NextIdx;                // The index to the next element in  m_links.   a negative number when done. 
    }

    /// <summary>
    /// m_refsForNodes maps the NodeIndexes of the reference graph to the children information.   However unlike
    /// a normal Graph RefGraph needs to support incremental addition of children.  Thus we can't use the normal
    /// compression (which assumed you know all the children when you define the node).  
    /// 
    /// m_refsForNodes points at a NodeListIndex which is a compressed list that is tuned for the case where
    /// a node has exactly one child (a very common case).   If that is not true we 'overflow' into a 'linked list'
    /// of RefElems that is stored in m_links.   See NodeListIndex for more on the exact encoding.   
    /// 
    /// </summary>
    internal NodeListIndex[] m_refsForNodes;

    /// <summary>
    /// If the number of children for a node is > 1 then we need to store the data somewhere.  m_links is array
    /// of linked list cells that hold the overflow case.  
    /// </summary>
    internal GrowableArray<RefElem> m_links;      // The rest of the list.  
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
