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
/// This class is responsible for taking a graph and generating a smaller graph that
/// is a reasonable proxy.   In particular
///     
///     1) A spanning tree is formed, and if a node is selected so are all its 
///        parents in that spanning tree.
///        
///     2) We try hard to keep scale each object type by the count by which the whole
///        graph was reduced.  
/// </summary>
[ExcludeFromCodeCoverage]
internal sealed class GraphSampler
{
    /// <summary>
    /// 
    /// </summary>
    public GraphSampler(MemoryGraph graph, int targetNodeCount, TextWriter log)
    {
        m_graph = graph;
        m_log = log;
        m_targetNodeCount = targetNodeCount;
        m_filteringRatio = (float)graph.NodeCount / targetNodeCount;
        m_nodeStorage = m_graph.AllocNodeStorage();
        m_childNodeStorage = m_graph.AllocNodeStorage();
        m_nodeTypeStorage = m_graph.AllocTypeNodeStorage();
    }

    /// <summary>
    /// Creates a new graph from 'graph' which has the same type statistics as the original
    /// graph but keeps the node count roughly at 'targetNodeCount'
    /// </summary>
    public MemoryGraph GetSampledGraph()
    {
        m_log.WriteLine("************* SAMPLING GRAPH TO REDUCE SIZE ***************");
        m_log.WriteLine("Original graph object count {0:n0}, targetObjectCount {1:n0} targetRatio {2:f2}", m_graph.NodeCount, m_targetNodeCount, m_filteringRatio);
        m_log.WriteLine("Original graph Size MB {0:n0} TypeCount {1:n0}", m_graph.TotalSize, m_graph.NodeTypeCount);

        // Get the spanning tree
        m_spanningTree = new SpanningTree(m_graph, m_log);
        m_spanningTree.ForEach(null);

        // Make the new graph 
        m_newGraph = new MemoryGraph(m_targetNodeCount + m_graph.NodeTypeCount * 2);
        m_newGraph.Is64Bit = m_graph.Is64Bit;

        // Initialize the object statistics
        m_statsByType = new SampleStats[m_graph.NodeTypeCount];
        for (int i = 0; i < m_statsByType.Length; i++)
        {
            m_statsByType[i] = new SampleStats();
        }

        // And initialize the mapping from old nodes to new nodes.  (TODO: this can be a hash table to save size?  )
        m_newIndex = new NodeIndex[m_graph.NodeCount];
        for (int i = 0; i < m_newIndex.Length; i++)
        {
            m_newIndex[i] = NodeIndex.Invalid;
        }

        ValidateStats(false);

        VisitNode(m_graph.RootIndex, true, false); // visit the root for sure.  
        // Sample the nodes, trying to keep the 
        for (NodeIndex nodeIdx = 0; nodeIdx < m_graph.NodeIndexLimit; nodeIdx++)
        {
            VisitNode(nodeIdx, false, false);
        }

        ValidateStats(true);

        // See if we need to flesh out the potential node to become truly sampled node to hit our quota.  
        for (NodeIndex nodeIdx = 0; nodeIdx < (NodeIndex)m_newIndex.Length; nodeIdx++)
        {
            var newIndex = m_newIndex[(int)nodeIdx];
            if (newIndex == PotentialNode)
            {
                var node = m_graph.GetNode(nodeIdx, m_nodeStorage);
                var stats = m_statsByType[(int)node.TypeIndex];
                int quota = (int)(stats.TotalCount / m_filteringRatio + .5);
                int needed = quota - stats.SampleCount;
                if (needed > 0)
                {
                    // If we have not computed the frequency of sampling do it now.  
                    if (stats.SkipFreq == 0)
                    {
                        var available = stats.PotentialCount - stats.SampleCount;
                        Debug.Assert(0 <= available);
                        Debug.Assert(needed <= available);
                        stats.SkipFreq = Math.Max(available / needed, 1);
                    }

                    // Sample every Nth time.  
                    stats.SkipCtr++;
                    if (stats.SkipFreq <= stats.SkipCtr)
                    {
                        // Sample a new node 
                        m_newIndex[(int)nodeIdx] = m_newGraph.CreateNode();
                        stats.SampleCount++;
                        stats.SampleMetric += node.Size;
                        stats.SkipCtr = 0;
                    }
                }
            }
        }

        // OK now m_newIndex tell us which nodes we want.  actually define the selected nodes. 

        // Initialize the mapping from old types to new types.
        m_newTypeIndexes = new NodeTypeIndex[m_graph.NodeTypeCount];
        for (int i = 0; i < m_newTypeIndexes.Length; i++)
        {
            m_newTypeIndexes[i] = NodeTypeIndex.Invalid;
        }

        GrowableArray<NodeIndex> children = new GrowableArray<NodeIndex>(100);
        for (NodeIndex nodeIdx = 0; nodeIdx < (NodeIndex)m_newIndex.Length; nodeIdx++)
        {
            // Add all sampled nodes to the new graph.  
            var newIndex = m_newIndex[(int)nodeIdx];
            if (IsSampledNode(newIndex))
            {
                var node = m_graph.GetNode(nodeIdx, m_nodeStorage);
                // Get the children that are part of the sample (ignore ones that are filter)
                children.Clear();
                for (var childIndex = node.GetFirstChildIndex(); childIndex != NodeIndex.Invalid; childIndex = node.GetNextChildIndex())
                {
                    var newChildIndex = m_newIndex[(int)childIndex];
                    if (0 <= newChildIndex)                 // the child is not filtered out. 
                    {
                        children.Add(newChildIndex);
                    }
                }
                // define the node
                var newTypeIndex = GetNewTypeIndex(node.TypeIndex);
                m_newGraph.SetNode(newIndex, newTypeIndex, node.Size, children);
                m_newGraph.SetAddress(newIndex, m_graph.GetAddress(nodeIdx));
            }
        }

        ValidateStats(true, true);

        // Set the root.
        m_newGraph.RootIndex = m_newIndex[(int)m_graph.RootIndex];
        Debug.Assert(0 <= m_newGraph.RootIndex);            // RootIndex in the tree.  

        m_newGraph.AllowReading();

        // At this point we are done.  The rest is just to report the result to the user.  

        // Sort the m_statsByType
        var sortedTypes = new int[m_statsByType.Length];
        for (int i = 0; i < sortedTypes.Length; i++)
        {
            sortedTypes[i] = i;
        }

        Array.Sort(sortedTypes, delegate (int x, int y)
        {
            var ret = m_statsByType[y].TotalMetric.CompareTo(m_statsByType[x].TotalMetric);
            return ret;
        });

        m_log.WriteLine("Stats of the top types (out of {0:n0})", m_newGraph.NodeTypeCount);
        m_log.WriteLine("OrigSizeMeg SampleSizeMeg   Ratio   |   OrigCnt  SampleCnt    Ratio   | Ave Size | Type Name");
        m_log.WriteLine("---------------------------------------------------------------------------------------------");

        for (int i = 0; i < Math.Min(m_statsByType.Length, 30); i++)
        {
            int typeIdx = sortedTypes[i];
            NodeType type = m_graph.GetType((NodeTypeIndex)typeIdx, m_nodeTypeStorage);
            var stats = m_statsByType[typeIdx];

            m_log.WriteLine("{0,12:n6} {1,11:n6}  {2,9:f2} | {3,10:n0} {4,9:n0}  {5,9:f2} | {6,8:f0} | {7}",
                stats.TotalMetric / 1000000.0, stats.SampleMetric / 1000000.0, stats.SampleMetric == 0 ? 0.0 : (double)stats.TotalMetric / stats.SampleMetric,
                stats.TotalCount, stats.SampleCount, stats.SampleCount == 0 ? 0.0 : (double)stats.TotalCount / stats.SampleCount,
                (double)stats.TotalMetric / stats.TotalCount, type.Name);
        }

        m_log.WriteLine("Sampled Graph node count {0,11:n0} (reduced by {1:f2} ratio)", m_newGraph.NodeCount,
            (double)m_graph.NodeCount / m_newGraph.NodeCount);
        m_log.WriteLine("Sampled Graph type count {0,11:n0} (reduced by {1:f2} ratio)", m_newGraph.NodeTypeCount,
            (double)m_graph.NodeTypeCount / m_newGraph.NodeTypeCount);
        m_log.WriteLine("Sampled Graph node size  {0,11:n0} (reduced by {1:f2} ratio)", m_newGraph.TotalSize,
            (double)m_graph.TotalSize / m_newGraph.TotalSize);
        return m_newGraph;
    }

    /// <summary>
    /// returns an array of scaling factors.  This array is indexed by the type index of
    /// the returned graph returned by GetSampledGraph.   If the sampled count for that type multiplied
    /// by this scaling factor, you end up with the count for that type of the original unsampled graph.  
    /// </summary>
    public float[] CountScalingByType
    {
        get
        {
            var ret = new float[m_newGraph.NodeTypeCount];
            for (int i = 0; i < m_statsByType.Length; i++)
            {
                var newTypeIndex = MapTypeIndex((NodeTypeIndex)i);
                if (newTypeIndex != NodeTypeIndex.Invalid)
                {
                    float scale = 1;
                    if (m_statsByType[i].SampleMetric != 0)
                    {
                        scale = (float)((double)m_statsByType[i].TotalMetric / m_statsByType[i].SampleMetric);
                    }

                    ret[(int)newTypeIndex] = scale;
                }
            }
            for (int i = 1; i < ret.Length; i++)
            {
                Debug.Assert(0 < ret[i] && ret[i] <= float.MaxValue);
            }

            return ret;
        }
    }

    /// <summary>
    /// Maps 'oldTypeIndex' to its type index in the output graph
    /// </summary>
    /// <returns>New type index, will be Invalid if the type is not in the output graph</returns>
    public NodeTypeIndex MapTypeIndex(NodeTypeIndex oldTypeIndex)
    {
        return m_newTypeIndexes[(int)oldTypeIndex];
    }

    /// <summary>
    /// Maps 'oldNodeIndex' to its new node index in the output graph
    /// </summary>
    /// <returns>New node index, will be less than 0 if the node is not in the output graph</returns>
    public NodeIndex MapNodeIndex(NodeIndex oldNodeIndex)
    {
        return m_newIndex[(int)oldNodeIndex];
    }

    #region private
    /// <summary>
    /// Visits 'nodeIdx', if already visited, do nothing.  If unvisited determine if 
    /// you should add this node to the graph being built.   If 'mustAdd' is true or
    /// if we need samples it keep the right sample/total ratio, then add the sample.  
    /// </summary>
    private void VisitNode(NodeIndex nodeIdx, bool mustAdd, bool doNotAddAncestors)
    {
        var newNodeIdx = m_newIndex[(int)nodeIdx];
        // If this node has been selected already, there is nothing to do.    
        if (IsSampledNode(newNodeIdx))
        {
            return;
        }
        // If we have visited this node and reject it and we are not forced to add it, we are done.
        if (newNodeIdx == RejectedNode && !mustAdd)
        {
            return;
        }

        Debug.Assert(newNodeIdx == NodeIndex.Invalid || newNodeIdx == PotentialNode || (newNodeIdx == RejectedNode && mustAdd));

        var node = m_graph.GetNode(nodeIdx, m_nodeStorage);
        var stats = m_statsByType[(int)node.TypeIndex];

        // If we have never seen this node before, add to our total count.  
        if (newNodeIdx == NodeIndex.Invalid)
        {
            if (stats.TotalCount == 0)
            {
                m_numDistictTypes++;
            }

            stats.TotalCount++;
            stats.TotalMetric += node.Size;
        }

        // Also insure that if there are a large number of types, that we sample them at least some. 
        if (stats.SampleCount == 0 && !mustAdd && (m_numDistictTypesWithSamples + .5F) * m_filteringRatio <= m_numDistictTypes)
        {
            mustAdd = true;
        }

        // We sample if we are forced (it is part of a parent chain), we need it to 
        // mimic the the original statistics, or if it is a large object (we include 
        // all large objects, since the affect overall stats so much).  
        if (mustAdd ||
            (stats.PotentialCount + .5f) * m_filteringRatio <= stats.TotalCount ||
            85000 < node.Size)
        {
            if (stats.SampleCount == 0)
            {
                m_numDistictTypesWithSamples++;
            }

            stats.SampleCount++;
            stats.SampleMetric += node.Size;
            if (newNodeIdx != PotentialNode)
            {
                stats.PotentialCount++;
            }

            m_newIndex[(int)nodeIdx] = m_newGraph.CreateNode();

            // Add all direct children as potential nodes (Potential nodes I can add without adding any other node)
            for (var childIndex = node.GetFirstChildIndex(); childIndex != NodeIndex.Invalid; childIndex = node.GetNextChildIndex())
            {
                var newChildIndex = m_newIndex[(int)childIndex];
                // Already a sampled or potential node.  Nothing to do.  
                if (IsSampledNode(newChildIndex) || newChildIndex == PotentialNode)
                {
                    continue;
                }

                var childNode = m_graph.GetNode(childIndex, m_childNodeStorage);
                var childStats = m_statsByType[(int)childNode.TypeIndex];

                if (newChildIndex == NodeIndex.Invalid)
                {
                    if (stats.TotalCount == 0)
                    {
                        m_numDistictTypes++;
                    }

                    childStats.TotalCount++;
                    childStats.TotalMetric += childNode.Size;
                }
                else
                {
                    Debug.Assert(newChildIndex == RejectedNode);
                }

                m_newIndex[(int)childIndex] = PotentialNode;
                childStats.PotentialCount++;
            }

            // For all ancestors, require them to be in the list
            if (!doNotAddAncestors)
            {
                for (; ; )
                {
                    nodeIdx = m_spanningTree.Parent(nodeIdx);
                    if (nodeIdx == NodeIndex.Invalid || m_newIndex.Length == (int)nodeIdx) // The last index represents the 'orphan' node.  
                    {
                        break;
                    }

                    // Indicate that you should not add ancestors (since I will do this).  
                    // We do the adding in a loop (rather than letting recursion do it) to avoid stack overflows
                    // for long chains of objects.  
                    VisitNode(nodeIdx, true, true);
                }
            }
        }
        else
        {
            if (newNodeIdx != PotentialNode)
            {
                m_newIndex[(int)nodeIdx] = RejectedNode;
            }
        }
    }

    /// <summary>
    /// Maps 'oldTypeIndex' to its type index in the output graph
    /// </summary>
    /// <param name="oldTypeIndex"></param>
    /// <returns></returns>
    private NodeTypeIndex GetNewTypeIndex(NodeTypeIndex oldTypeIndex)
    {
        var ret = m_newTypeIndexes[(int)oldTypeIndex];
        if (ret == NodeTypeIndex.Invalid)
        {
            var oldType = m_graph.GetType(oldTypeIndex, m_nodeTypeStorage);
            ret = m_newGraph.CreateType(oldType.Name, oldType.ModuleName, oldType.Size);
            m_newTypeIndexes[(int)oldTypeIndex] = ret;
        }
        return ret;
    }


    [Conditional("DEBUG")]
    private void ValidateStats(bool allNodesVisited, bool completed = false)
    {
        var statsCheckByType = new SampleStats[m_statsByType.Length];
        for (int i = 0; i < statsCheckByType.Length; i++)
        {
            statsCheckByType[i] = new SampleStats();
        }

        int total = 0;
        long totalSize = 0;
        int sampleTotal = 0;
        var typeStorage = m_graph.AllocTypeNodeStorage();
        for (NodeIndex nodeIdx = 0; nodeIdx < (NodeIndex)m_newIndex.Length; nodeIdx++)
        {
            var node = m_graph.GetNode(nodeIdx, m_nodeStorage);
            var stats = statsCheckByType[(int)node.TypeIndex];
            var type = node.GetType(typeStorage);
            var typeName = type.Name;
            var newNodeIdx = m_newIndex[(int)nodeIdx];

            if (newNodeIdx == NodeIndex.Invalid)
            {
                // We should have visited every node, so there should be no Invalid nodes. 
                Debug.Assert(!allNodesVisited);
            }
            else
            {
                total++;
                stats.TotalCount++;
                stats.TotalMetric += node.Size;
                totalSize += node.Size;
                Debug.Assert(node.Size != 0 || typeName.StartsWith('[') || typeName == "UNDEFINED");
                if (IsSampledNode(newNodeIdx) || newNodeIdx == PotentialNode)
                {
                    if (nodeIdx != m_graph.RootIndex)
                    {
                        Debug.Assert(IsSampledNode(m_spanningTree.Parent(nodeIdx)));
                    }

                    stats.PotentialCount++;
                    if (IsSampledNode(newNodeIdx))
                    {
                        stats.SampleCount++;
                        sampleTotal++;
                        stats.SampleMetric += node.Size;
                    }
                }
                else
                {
                    Debug.Assert(newNodeIdx == RejectedNode);
                }
            }
            statsCheckByType[(int)node.TypeIndex] = stats;
        }

        float[] scalings = null;
        if (completed)
        {
            scalings = CountScalingByType;
        }

        for (NodeTypeIndex typeIdx = 0; typeIdx < m_graph.NodeTypeIndexLimit; typeIdx++)
        {
            _ = m_graph.GetType(typeIdx, typeStorage);
            var statsCheck = statsCheckByType[(int)typeIdx];
            var stats = m_statsByType[(int)typeIdx];

            Debug.Assert(stats.TotalMetric == statsCheck.TotalMetric);
            Debug.Assert(stats.TotalCount == statsCheck.TotalCount);
            Debug.Assert(stats.SampleCount == statsCheck.SampleCount);
            Debug.Assert(stats.SampleMetric == statsCheck.SampleMetric);
            Debug.Assert(stats.PotentialCount == statsCheck.PotentialCount);

            Debug.Assert(stats.PotentialCount <= statsCheck.TotalCount);
            Debug.Assert(stats.SampleCount <= statsCheck.PotentialCount);

            // We should be have at least m_filterRatio of Potential objects 
            Debug.Assert(!((stats.PotentialCount + .5f) * m_filteringRatio <= stats.TotalCount));

            // If we completed, then we converted potentials to true samples.   
            if (completed)
            {
                Debug.Assert(!((stats.SampleCount + .5f) * m_filteringRatio <= stats.TotalCount));

                // Make sure that scalings that we finally output were created correctly
                if (stats.SampleMetric > 0)
                {
                    var newTypeIdx = MapTypeIndex(typeIdx);
                    var estimatedTotalMetric = scalings[(int)newTypeIdx] * stats.SampleMetric;
                    Debug.Assert(Math.Abs(estimatedTotalMetric - stats.TotalMetric) / stats.TotalMetric < .01);
                }
            }

            if (stats.SampleCount == 0)
            {
                Debug.Assert(stats.SampleMetric == 0);
            }

            if (stats.TotalMetric == 0)
            {
                Debug.Assert(stats.TotalMetric == 0);
            }
        }

        if (allNodesVisited)
        {
            Debug.Assert(total == m_graph.NodeCount);
            // TODO The assert should be Debug.Assert(totalSize == m_graph.TotalSize);
            // but we have to give a 1% error margin to get things passing. Fix this.
            Debug.Assert(Math.Abs(totalSize - m_graph.TotalSize) / totalSize < .01);
        }
        Debug.Assert(sampleTotal == m_newGraph.NodeCount);
    }

    private sealed class SampleStats
    {
        public int TotalCount;          // The number of objects of this type in the original graph
        public int SampleCount;         // The number of objects of this type we have currently added to the new graph
        public int PotentialCount;      // SampleCount + The number of objects of this type that can be added without needing to add other nodes
        public long TotalMetric;
        public long SampleMetric;
        public int SkipFreq;          // When sampling potentials, take every Nth one where this is the N
        public int SkipCtr;             // This remembers our last N.  
    };

    /// <summary>
    /// This value goes in the m_newIndex[].   If we accept the node into the sampled graph, we put the node
    /// index in the NET graph in m_newIndex.   If we reject the node we use the special RejectedNode value
    /// below
    /// </summary>
    private const NodeIndex RejectedNode = (NodeIndex)(-2);

    /// <summary>
    /// This value also goes in m_newIndex[].   If we can add this node without needing to add any other nodes
    /// to the new graph (that is it is one hop from an existing accepted node, then we mark it specially as
    /// a PotentialNode).   We add these in a second pass over the data.  
    /// </summary>
    private const NodeIndex PotentialNode = (NodeIndex)(-3);

    private bool IsSampledNode(NodeIndex nodeIdx) { return 0 <= nodeIdx; }

    private MemoryGraph m_graph;
    private int m_targetNodeCount;
    private TextWriter m_log;
    private Node m_nodeStorage;
    private Node m_childNodeStorage;
    private NodeType m_nodeTypeStorage;
    private float m_filteringRatio;
    private SampleStats[] m_statsByType;
    private int m_numDistictTypesWithSamples;
    private int m_numDistictTypes;
    private NodeIndex[] m_newIndex;
    private NodeTypeIndex[] m_newTypeIndexes;
    private SpanningTree m_spanningTree;
    private MemoryGraph m_newGraph;
    #endregion
}
