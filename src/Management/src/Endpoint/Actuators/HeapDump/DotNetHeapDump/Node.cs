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
    /// Node represents a single node in the code:Graph.  These are created lazily and follow a pattern were the 
    /// CALLER provides the storage for any code:Node or code:NodeType value that are returned.   Thus the caller
    /// is responsible for determine when nodes can be reused to minimize GC cost.  
    /// 
    /// A node implicitly knows where the 'next' child is (that is it is an iterator).  
    /// </summary>
    [ExcludeFromCodeCoverage]
    internal class Node
    {
        public int Size
        {
            get
            {
                m_graph.m_reader.Goto(m_graph.m_nodes[(int)m_index]);
                var typeAndSize = ReadCompressedInt(m_graph.m_reader);
                if ((typeAndSize & 1) != 0)     // low bit indicates if Size is encoded explicitly
                {
                    return ReadCompressedInt(m_graph.m_reader);
                }

                // Then it is in the type;
                typeAndSize >>= 1;
                return m_graph.m_types[typeAndSize].Size;
            }
        }
        public bool Defined { get { return m_graph.IsDefined(Index); } }
        public NodeType GetType(NodeType storage)
        {
            return m_graph.GetType(TypeIndex, storage);
        }

        /// <summary>
        /// Reset the internal state so that 'GetNextChildIndex; will return the first child.  
        /// </summary>
        public void ResetChildrenEnumeration()
        {
            m_graph.m_reader.Goto(m_graph.m_nodes[(int)m_index]);
            if ((ReadCompressedInt(m_graph.m_reader) & 1) != 0)        // Skip nodeId and Size
            {
                ReadCompressedInt(m_graph.m_reader);
            }

            m_numChildrenLeft = ReadCompressedInt(m_graph.m_reader);
            Debug.Assert(m_numChildrenLeft < 1660000);     // Not true in general but good enough for unit testing.
            m_current = m_graph.m_reader.Current;
        }

        /// <summary>
        /// Gets the index of the first child of node.  Will return NodeIndex.Invalid if there are no children. 
        /// </summary>
        /// <returns>The index of the child </returns>
        public NodeIndex GetFirstChildIndex()
        {
            ResetChildrenEnumeration();
            return GetNextChildIndex();
        }
        public NodeIndex GetNextChildIndex()
        {
            if (m_numChildrenLeft == 0)
            {
                return NodeIndex.Invalid;
            }

            m_graph.m_reader.Goto(m_current);

            var ret = (NodeIndex)(ReadCompressedInt(m_graph.m_reader) + (int)m_index);
            Debug.Assert((uint)ret < (uint)m_graph.NodeIndexLimit);

            m_current = m_graph.m_reader.Current;
            --m_numChildrenLeft;
            return ret;
        }

        /// <summary>
        /// Returns the number of children this node has.  
        /// </summary>
        public int ChildCount
        {
            get
            {
                m_graph.m_reader.Goto(m_graph.m_nodes[(int)m_index]);
                if ((ReadCompressedInt(m_graph.m_reader) & 1) != 0)        // Skip nodeId and Size
                {
                    ReadCompressedInt(m_graph.m_reader);
                }

                return ReadCompressedInt(m_graph.m_reader);
            }
        }
        public NodeTypeIndex TypeIndex
        {
            get
            {
                m_graph.m_reader.Goto(m_graph.m_nodes[(int)m_index]);
                var ret = (NodeTypeIndex)(ReadCompressedInt(m_graph.m_reader) >> 1);
                return ret;
            }
        }
        public NodeIndex Index { get { return m_index; } }
        public Graph Graph { get { return m_graph; } }
        /// <summary>
        /// Returns true if 'node' is a child of 'this'.  childStorage is simply used as temp space 
        /// as was allocated by Graph.AllocateNodeStorage
        /// </summary>
        public bool Contains(NodeIndex nodeIndex)
        {
            for (NodeIndex childIndex = GetFirstChildIndex(); childIndex != NodeIndex.Invalid; childIndex = GetNextChildIndex())
            {
                if (childIndex == nodeIndex)
                {
                    return true;
                }
            }
            return false;
        }

        public override string ToString()
        {
            StringWriter sw = new StringWriter();
            WriteXml(sw, includeChildren: false);
            return sw.ToString();
        }
        public virtual void WriteXml(TextWriter writer, bool includeChildren = true, string prefix = "", NodeType typeStorage = null, string additinalAttribs = "")
        {
            Debug.Assert(Index != NodeIndex.Invalid);
            typeStorage ??= m_graph.AllocTypeNodeStorage();

            if (m_graph.m_nodes[(int)Index] == StreamLabel.Invalid)
            {
                writer.WriteLine("{0}<Node Index=\"{1}\" Undefined=\"true\"{2}/>", prefix, (int)Index, additinalAttribs);
                return;
            }

            writer.Write("{0}<Node Index=\"{1}\" TypeIndex=\"{2}\" Size=\"{3}\" Type=\"{4}\" NumChildren=\"{5}\"{6}",
                prefix, (int)Index, TypeIndex, Size, SecurityElement.Escape(GetType(typeStorage).Name),
                ChildCount, additinalAttribs);
            var childIndex = GetFirstChildIndex();
            if (childIndex != NodeIndex.Invalid)
            {
                writer.WriteLine(">");
                if (includeChildren)
                {
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
                }
                else
                {
                    writer.Write(prefix);
                    writer.WriteLine($"<!-- {ChildCount} children omitted... -->");
                }
                writer.WriteLine(" </Node>");
            }
            else
            {
                writer.WriteLine("/>");
            }
        }
        #region private
        protected internal Node(Graph graph)
        {
            m_graph = graph;
            m_index = NodeIndex.Invalid;
        }

        // Node information is stored in a compressed form because we have a lot of them. 
        internal static int ReadCompressedInt(SegmentedMemoryStreamReader reader)
        {
            int ret = 0;
            byte b = reader.ReadByte();
            ret = b << 25 >> 25;
            if ((b & 0x80) == 0)
            {
                return ret;
            }

            ret <<= 7;
            b = reader.ReadByte();
            ret += b & 0x7f;
            if ((b & 0x80) == 0)
            {
                return ret;
            }

            ret <<= 7;
            b = reader.ReadByte();
            ret += b & 0x7f;
            if ((b & 0x80) == 0)
            {
                return ret;
            }

            ret <<= 7;
            b = reader.ReadByte();
            ret += b & 0x7f;
            if ((b & 0x80) == 0)
            {
                return ret;
            }

            ret <<= 7;
            b = reader.ReadByte();
            Debug.Assert((b & 0x80) == 0);
            ret += b;
            return ret;
        }

        internal static void WriteCompressedInt(SegmentedMemoryStreamWriter writer, int value)
        {
            if (value << 25 >> 25 == value)
            {
                goto oneByte;
            }

            if (value << 18 >> 18 == value)
            {
                goto twoBytes;
            }

            if (value << 11 >> 11 == value)
            {
                goto threeBytes;
            }

            if (value << 4 >> 4 == value)
            {
                goto fourBytes;
            }

            writer.Write((byte)((value >> 28) | 0x80));
            fourBytes:
            writer.Write((byte)((value >> 21) | 0x80));
            threeBytes:
            writer.Write((byte)((value >> 14) | 0x80));
            twoBytes:
            writer.Write((byte)((value >> 7) | 0x80));
            oneByte:
            writer.Write((byte)(value & 0x7F));
        }

        internal NodeIndex m_index;
        internal Graph m_graph;
        private StreamLabel m_current;          // My current child in the enumerable.
        private int m_numChildrenLeft;          // count of my children
        #endregion
    }
}
