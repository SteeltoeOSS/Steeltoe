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

// Graph contains generic Graph-Node traversal algorithms (spanning tree etc).

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
