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
namespace Graphs
{
    /// <summary>
    /// Stuff that is useful but does not need to be in Graph.   
    /// </summary>
    [ExcludeFromCodeCoverage]
    internal static class GraphUtils
    {
        /// <summary>
        /// Write the graph as XML to a string and return it (useful for debugging small graphs).  
        /// </summary>
        /// <returns></returns>
        public static string PrintGraph(this Graph graph)
        {
            StringWriter sw = new StringWriter();
            graph.WriteXml(sw);
            return sw.ToString();
        }
        public static string PrintNode(this Graph graph, NodeIndex nodeIndex)
        {
            return graph.GetNode(nodeIndex, graph.AllocNodeStorage()).ToString();
        }
        public static string PrintNode(this Graph graph, int nodeIndex)
        {
            return graph.PrintNode((NodeIndex)nodeIndex);
        }
        public static string PrintNodes(this Graph graph, List<NodeIndex> nodes)
        {
            var sw = new StringWriter();
            sw.WriteLine("<NodeList>");
            var node = graph.AllocNodeStorage();
            var type1 = graph.AllocTypeNodeStorage();

            foreach (var nodeIndex in nodes)
            {
                node = graph.GetNode(nodeIndex, node);
                node.WriteXml(sw, prefix: "  ", typeStorage: type1);
            }
            sw.WriteLine("<NodeList>");
            return sw.ToString();
        }
        public static string PrintChildren(this Graph graph, NodeIndex nodeIndex)
        {
            return graph.PrintNodes(graph.NodeChildren(nodeIndex));
        }
        public static string PrintChildren(this Graph graph, int nodeIndex)
        {
            return graph.PrintChildren((NodeIndex)nodeIndex);
        }
        // Debugging. 
        /// <summary>
        /// Writes the graph as XML to 'writer'.  Don't use on big graphs.  
        /// </summary>
        public static void WriteXml(this Graph graph, TextWriter writer)
        {
            writer.WriteLine("<MemoryGraph NumNodes=\"{0}\" NumTypes=\"{1}\" TotalSize=\"{2}\" SizeOfGraphDescription=\"{3}\">",
                graph.NodeIndexLimit, graph.NodeTypeIndexLimit, graph.TotalSize, graph.SizeOfGraphDescription());
            writer.WriteLine(" <RootIndex>{0}</RootIndex>", graph.RootIndex);
            writer.WriteLine(" <NodeTypes Count=\"{0}\">", graph.NodeTypeIndexLimit);
            var typeStorage = graph.AllocTypeNodeStorage();
            for (NodeTypeIndex typeIndex = 0; typeIndex < graph.NodeTypeIndexLimit; typeIndex++)
            {
                var type = graph.GetType(typeIndex, typeStorage);
                type.WriteXml(writer, "  ");
            }
            writer.WriteLine(" </NodeTypes>");

            writer.WriteLine(" <Nodes Count=\"{0}\">", graph.NodeIndexLimit);
            var nodeStorage = graph.AllocNodeStorage();
            for (NodeIndex nodeIndex = 0; nodeIndex < graph.NodeIndexLimit; nodeIndex++)
            {
                var node = graph.GetNode(nodeIndex, nodeStorage);
                node.WriteXml(writer, prefix: "  ");
            }
            writer.WriteLine(" </Nodes>");
            writer.WriteLine("</MemoryGraph>");
        }
        public static void DumpNormalized(this MemoryGraph graph, TextWriter writer)
        {
            MemoryNode nodeStorage = (MemoryNode)graph.AllocNodeStorage();
            NodeType typeStorage = graph.AllocTypeNodeStorage();
            Node node;

#if false 
            // Compute reachability info
            bool[] reachable = new bool[(int)graph.NodeIndexLimit];
            Queue<NodeIndex> workQueue = new Queue<NodeIndex>();
            workQueue.Enqueue(graph.RootIndex);
            while (workQueue.Count > 0)
            {
                var nodeIdx = workQueue.Dequeue();
                if (!reachable[(int)nodeIdx])
                {
                    reachable[(int)nodeIdx] = true;
                    node = graph.GetNode(nodeIdx, nodeStorage);
                    for (var childIndex = node.GetFirstChildIndex(); childIndex != NodeIndex.Invalid; childIndex = node.GetNextChildIndex())
                        workQueue.Enqueue(childIndex);
                }
            }

            // Get Reachability count. 
            int reachableCount = 0;
            for (int i = 0; i < reachable.Length; i++)
                if (reachable[i])
                    reachableCount++;
#endif

            // Sort the nodes by virtual address 
            NodeIndex[] sortedNodes = new NodeIndex[(int)graph.NodeIndexLimit];
            for (int i = 0; i < sortedNodes.Length; i++)
            {
                sortedNodes[i] = (NodeIndex)i;
            }

            Array.Sort(sortedNodes, delegate (NodeIndex x, NodeIndex y)
            {
                // Sort first by address
                int ret = graph.GetAddress(x).CompareTo(graph.GetAddress(y));
                if (ret != 0)
                {
                    return ret;
                }
                // Then by name
                return graph.GetNode(x, nodeStorage).GetType(typeStorage).Name.CompareTo(graph.GetNode(y, nodeStorage).GetType(typeStorage).Name);
            });

            node = graph.GetNode(graph.RootIndex, nodeStorage);
            writer.WriteLine("<GraphDump RootNode=\"{0}\" NumNodes=\"{1}\" NumTypes=\"{2}\" TotalSize=\"{3}\" SizeOfGraphDescription=\"{4}\">",
                SecurityElement.Escape(node.GetType(typeStorage).Name),
                graph.NodeIndexLimit,
                graph.NodeTypeIndexLimit,
                graph.TotalSize,
                graph.SizeOfGraphDescription());
            writer.WriteLine(" <Nodes Count=\"{0}\">", graph.NodeIndexLimit);

            SortedDictionary<ulong, bool> roots = new SortedDictionary<ulong, bool>();
            foreach (NodeIndex nodeIdx in sortedNodes)
            {
                // if (!reachable[(int)nodeIdx]) continue;

                node = graph.GetNode(nodeIdx, nodeStorage);
                string name = node.GetType(typeStorage).Name;

                writer.Write("  <Node Address=\"{0:x}\" Size=\"{1}\" Type=\"{2}\"> ", graph.GetAddress(nodeIdx), node.Size, SecurityElement.Escape(name));
                bool isRoot = graph.GetAddress(node.Index) == 0;
                int childCnt = 0;
                for (var childIndex = node.GetFirstChildIndex(); childIndex != NodeIndex.Invalid; childIndex = node.GetNextChildIndex())
                {
                    if (isRoot)
                    {
                        roots[graph.GetAddress(childIndex)] = true;
                    }

                    childCnt++;
                    if (childCnt % 8 == 0)
                    {
                        writer.WriteLine();
                        writer.Write("    ");
                    }
                    writer.Write("{0:x} ", graph.GetAddress(childIndex));
                }
                writer.WriteLine(" </Node>");
            }
            writer.WriteLine(" <Roots>");
            foreach (ulong root in roots.Keys)
            {
                writer.WriteLine("  {0:x}", root);
            }
            writer.WriteLine(" </Roots>");
            writer.WriteLine(" </Nodes>");
            writer.WriteLine("</GraphDump>");
        }

        public static List<NodeIndex> NodeChildren(this Graph graph, NodeIndex nodeIndex)
        {
            var node = graph.GetNode(nodeIndex, graph.AllocNodeStorage());
            var ret = new List<NodeIndex>();
            for (var childIndex = node.GetFirstChildIndex(); childIndex != NodeIndex.Invalid; childIndex = node.GetNextChildIndex())
            {
                ret.Add(childIndex);
            }

            return ret;
        }
        public static List<NodeIndex> NodesOfType(this Graph graph, string regExpression)
        {
            var typeSet = new Dictionary<NodeTypeIndex, NodeTypeIndex>();
            var type = graph.AllocTypeNodeStorage();
            for (NodeTypeIndex typeId = 0; typeId < graph.NodeTypeIndexLimit; typeId = typeId + 1)
            {
                type = graph.GetType(typeId, type);
                if (Regex.IsMatch(type.Name, regExpression))
                {
                    typeSet.Add(typeId, typeId);
                }
            }

            var ret = new List<NodeIndex>();
            var node = graph.AllocNodeStorage();
            for (NodeIndex nodeId = 0; nodeId < graph.NodeIndexLimit; nodeId = nodeId + 1)
            {
                node = graph.GetNode(nodeId, node);
                if (typeSet.ContainsKey(node.TypeIndex))
                {
                    ret.Add(nodeId);
                }
            }
            return ret;
        }
    }
}
