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
/// code:MemorySampleSource hooks up a Memory graph to become a Sample source.  Currently we do
/// a breadth-first traversal to form a spanning tree, and then create samples for each node
/// where the 'stack' is the path to the root of this spanning tree.
/// 
/// This is just a first cut...
/// </summary>
[ExcludeFromCodeCoverage]
internal sealed class SpanningTree
{
    public SpanningTree(Graph graph, TextWriter log)
    {
        Graph = graph;
        m_log = log;
        m_nodeStorage = graph.AllocNodeStorage();
        m_childStorage = graph.AllocNodeStorage();
        m_typeStorage = graph.AllocTypeNodeStorage();

        // We need to reduce the graph to a tree.   Each node is assigned a unique 'parent' which is its 
        // parent in a spanning tree of the graph.  
        // The +1 is for orphan node support.  
        m_parent = new NodeIndex[(int)graph.NodeIndexLimit + 1];
    }
    public Graph Graph { get; }

    /// <summary>
    /// Every type is given a priority of 0 unless the type name matches one of 
    /// the patterns in PriorityRegExs.  If it does that type is assigned that priority.
    /// 
    /// A node's priority is defined to be the priority of the type of the node
    /// (as given by PriorityRegExs), plus 1/10 the priority of its parent.  
    /// 
    /// Thus priorities 'decay' by 1/10 through pointers IF the priority of the node's
    /// type is 0 (the default).   
    ///
    /// By default the framework has a priority of -1 which means that you explore all
    /// high priority and user defined types before any framework type.
    /// 
    /// Types with the same priority are enumerate breath-first.  
    /// </summary>
    public string PriorityRegExs
    {
        get
        {
            if (m_priorityRegExs == null)
            {
                PriorityRegExs = DefaultPriorities;
            }

            return m_priorityRegExs;
        }
        set
        {
            m_priorityRegExs = value;
            SetTypePriorities(value);
        }
    }
    public static string DefaultPriorities
    {
        get
        {
            return
                // By types (including user defined types) are 0
                @"v4.0.30319\%!->-1;" +     // Framework is less than default
                @"v2.0.50727\%!->-1;" +     // Framework is less than default
                @"[*local vars]->-1000;" +  // Local variables are not that interesting, since they tend to be transient
                @"mscorlib!Runtime.CompilerServices.ConditionalWeakTable->-10000;" + // We prefer not to use Conditional weak table references even more. 
                @"[COM/WinRT Objects]->-1000000;" + // We prefer to Not use the CCW roots. 
                @"[*handles]->-2000000;" +
                @"[other roots]->-2000000";
        }
    }

    public NodeIndex Parent(NodeIndex node) { return m_parent[(int)node]; }

    public void ForEach(Action<NodeIndex> callback)
    {
        // Initialize the priority 
        if (m_typePriorities == null)
        {
            PriorityRegExs = DefaultPriorities;
        }

        Debug.Assert(m_typePriorities != null);

        // Initialize the breadth-first work queue.
        var nodesToVisit = new PriorityQueue(1024);
        nodesToVisit.Enqueue(Graph.RootIndex, 0.0F);

        // reset the visited information.
        for (int i = 0; i < m_parent.Length; i++)
        {
            m_parent[i] = NodeIndex.Invalid;
        }

        float[] nodePriorities = new float[m_parent.Length];
        bool scanedForOrphans = false;
        var epsilon = 1E-7F;            // Something that is big enough not to bet lost in roundoff error.  
        float order = 0;
        for (int i = 0; ; i++)
        {
            if ((i & 0x1FFF) == 0)  // Every 8K
            {
                System.Threading.Thread.Sleep(0);       // Allow interruption.  
            }

            NodeIndex nodeIndex;
            float nodePriority;
            if (nodesToVisit.Count == 0)
            {
                nodePriority = 0;
                if (!scanedForOrphans)
                {
                    scanedForOrphans = true;
                    AddOrphansToQueue(nodesToVisit);
                }
                if (nodesToVisit.Count == 0)
                {
                    return;
                }
            }
            nodeIndex = nodesToVisit.Dequeue(out nodePriority);

            // Insert any children that have not already been visited (had a parent assigned) into the work queue). 
            var node = Graph.GetNode(nodeIndex, m_nodeStorage);
            var parentPriority = nodePriorities[(int)node.Index];
            for (var childIndex = node.GetFirstChildIndex(); childIndex != NodeIndex.Invalid; childIndex = node.GetNextChildIndex())
            {
                if (m_parent[(int)childIndex] == NodeIndex.Invalid)
                {
                    m_parent[(int)childIndex] = nodeIndex;

                    // the priority of the child is determined by its type and 1/10 by its parent.  
                    var child = Graph.GetNode(childIndex, m_childStorage);
                    var childPriority = m_typePriorities[(int)child.TypeIndex] + parentPriority / 10;
                    nodePriorities[(int)childIndex] = childPriority;

                    // Subtract a small increasing value to keep the queue in order if the priorities are the same. 
                    // This is a bit of a hack since it can get big and purtub the user-defined order.  
                    order += epsilon;
                    nodesToVisit.Enqueue(childIndex, childPriority - order);
                }
            }

            // Return the node.  
            callback?.Invoke(node.Index);
        }
    }

    #region private
    /// <summary>
    /// Add any unreachable nodes to the 'nodesToVisit'.   Note that we do this in a 'smart' way
    /// where we only add orphans that are not reachable from other orphans.   That way we get a 
    /// minimal set of orphan 'roots'.  
    /// </summary>
    /// <param name="nodesToVisit"></param>
    private void AddOrphansToQueue(PriorityQueue nodesToVisit)
    {

        for (int i = 0; i < (int)Graph.NodeIndexLimit; i++)
        {
            if (m_parent[i] == NodeIndex.Invalid)
            {
                MarkDecendentsIgnoringCycles((NodeIndex)i, 0);
            }
        }

        // Collect up all the nodes that are not reachable from other nodes as the roots of the
        // orphans.  Also reset orphanVisitedMarker back to NodeIndex.Invalid.
        for (int i = 0; i < (int)Graph.NodeIndexLimit; i++)
        {
            var nodeIndex = (NodeIndex)i;
            var parent = m_parent[(int)nodeIndex];
            if (parent <= NodeIndex.Invalid)
            {
                if (parent == NodeIndex.Invalid)
                {
                    // Thr root index has no parent but is reachable from the root. 
                    if (nodeIndex != Graph.RootIndex)
                    {
                        var node = Graph.GetNode(nodeIndex, m_nodeStorage);
                        var priority = m_typePriorities[(int)node.TypeIndex];
                        nodesToVisit.Enqueue(nodeIndex, priority);
                        m_parent[(int)nodeIndex] = Graph.NodeIndexLimit;               // This is the 'not reachable' parent. 
                    }
                }
                else
                {
                    m_parent[(int)nodeIndex] = NodeIndex.Invalid;
                }
            }
        }
    }

    /// <summary>
    /// A helper for AddOrphansToQueue, so we only add orphans that are not reachable from other orphans.  
    /// 
    /// Mark all descendants (but not nodeIndex itself) as being visited.    Any arcs that form
    /// cycles are ignored, so nodeIndex is guaranteed to NOT be marked.     
    /// </summary>
    private void MarkDecendentsIgnoringCycles(NodeIndex nodeIndex, int recursionCount)
    {
        // TODO We give up if the chains are larger than 10K long (because we stack overflow otherwise)
        // We could have an explicit stack and avoid this...
        if (recursionCount > 10000)
        {
            return;
        }

        Debug.Assert(m_parent[(int)nodeIndex] == NodeIndex.Invalid);

        // This marks that there is a path from another orphan to this one (thus it is not a good root)
        NodeIndex orphanVisitedMarker = NodeIndex.Invalid - 1;

        // To detect cycles we mark all nodes we not committed to (we are visiting, rather than visited)
        // If we detect this mark we understand it is a loop and ignore the arc.  
        NodeIndex orphanVisitingMarker = NodeIndex.Invalid - 2;
        m_parent[(int)nodeIndex] = orphanVisitingMarker;        // We are now visitING

        // Mark all nodes as being visited.  
        var node = Graph.GetNode(nodeIndex, AllocNodeStorage());
        for (var childIndex = node.GetFirstChildIndex(); childIndex != NodeIndex.Invalid; childIndex = node.GetNextChildIndex())
        {
            // Has this child not been seen at all?  If so mark it.  
            // Skip it if we are visiting (it would form a cycle) or visited (or not an orphan)
            if (m_parent[(int)childIndex] == NodeIndex.Invalid)
            {
                MarkDecendentsIgnoringCycles(childIndex, recursionCount + 1);
                m_parent[(int)childIndex] = orphanVisitedMarker;
            }
        }
        FreeNodeStorage(node);

        // We set this above, and should not have changed it.  
        Debug.Assert(m_parent[(int)nodeIndex] == orphanVisitingMarker);
        // Now that we are finished, we reset the visiting bit.  
        m_parent[(int)nodeIndex] = NodeIndex.Invalid;
    }

    /// <summary>
    /// Gives back nodes that are no longer in use.  This is a memory optimization. 
    /// </summary>
    private void FreeNodeStorage(Node node)
    {
        m_cachedNodeStorage = node;
    }
    /// <summary>
    /// Gets a node that can be written on.  It is a simple cache
    /// </summary>
    /// <returns></returns>
    private Node AllocNodeStorage()
    {
        var ret = m_cachedNodeStorage;                // See if we have a free node. 
        if (ret == null)
        {
            ret = Graph.AllocNodeStorage();
        }
        else
        {
            m_cachedNodeStorage = null;               // mark that that node is in use.  
        }

        return ret;
    }

    /// <summary>
    /// Convert a string from my regular expression format (where you only have * and {  } as grouping operators
    /// and convert them to .NET regular expressions string
    /// TODO FIX NOW cloned code (also in FilterStackSource)
    /// </summary>
    internal static string ToDotNetRegEx(string str)
    {
        // A leading @ sign means the rest is a .NET regular expression.  (Undocumented, not really needed yet.)
        if (str.StartsWith('@'))
        {
            return str.Substring(1);
        }

        str = Regex.Escape(str);                // Assume everything is ordinary
        str = str.Replace(@"%", @"[.\w\d?]*");  // % means any number of alpha-numeric chars. 
        str = str.Replace(@"\*", @".*");        // * means any number of any characters.  
        str = str.Replace(@"\^", @"^");         // ^ means anchor at the beginning.  
        str = str.Replace(@"\|", @"|");         // | means is the or operator  
        str = str.Replace(@"\{", "(");
        str = str.Replace('}', ')');
        return str;
    }

    private void SetTypePriorities(string priorityPats)
    {
        m_typePriorities ??= new float[(int)Graph.NodeTypeIndexLimit];

        string[] priorityPatArray = priorityPats.Split(';');
        Regex[] priorityRegExArray = new Regex[priorityPatArray.Length];
        float[] priorityArray = new float[priorityPatArray.Length];
        for (int i = 0; i < priorityPatArray.Length; i++)
        {
            var m = Regex.Match(priorityPatArray[i], @"(.*)->(-?\d+.?\d*)");
            if (!m.Success)
            {
                if (string.IsNullOrWhiteSpace(priorityPatArray[i]))
                {
                    continue;
                }

                throw new ApplicationException($"Priority pattern {priorityPatArray[i]} is not of the form Pat->Num.");
            }

            var dotNetRegEx = ToDotNetRegEx(m.Groups[1].Value.Trim());
            priorityRegExArray[i] = new Regex(dotNetRegEx, RegexOptions.IgnoreCase);
            priorityArray[i] = float.Parse(m.Groups[2].Value);
        }

        // Assign every type index a priority in m_typePriorities based on if they match a pattern.  
        NodeType typeStorage = Graph.AllocTypeNodeStorage();
        for (NodeTypeIndex typeIdx = 0; typeIdx < Graph.NodeTypeIndexLimit; typeIdx++)
        {
            var type = Graph.GetType(typeIdx, typeStorage);

            var fullName = type.FullName;
            for (int regExIdx = 0; regExIdx < priorityRegExArray.Length; regExIdx++)
            {
                var priorityRegEx = priorityRegExArray[regExIdx];
                if (priorityRegEx == null)
                {
                    continue;
                }

                var m = priorityRegEx.Match(fullName);
                if (m.Success)
                {
                    m_typePriorities[(int)typeIdx] = priorityArray[regExIdx];
                    // m_log.WriteLine("Type {0} assigned priority {1:f3}", fullName, priorityArray[regExIdx]);
                    break;
                }
            }
        }
    }

    private NodeIndex[] m_parent;               // We keep track of the parents of each node in our breadth-first scan. 

    // We give each type a priority (using the m_priority Regular expressions) which guide the breadth-first scan. 
    private string m_priorityRegExs;
    private float[] m_typePriorities;
    private NodeType m_typeStorage;
    private Node m_nodeStorage;                 // Only for things that can't be reentrant
    private Node m_childStorage;
    private Node m_cachedNodeStorage;           // Used when it could be reentrant
    private TextWriter m_log;                   // processing messages 
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
