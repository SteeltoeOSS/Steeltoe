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
