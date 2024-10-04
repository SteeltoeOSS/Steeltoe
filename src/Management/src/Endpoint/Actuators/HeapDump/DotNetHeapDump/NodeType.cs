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
    /// Represents the nodeId of a particular node in the graph.  
    /// </summary>
    [ExcludeFromCodeCoverage]
    internal sealed class NodeType
    {
        /// <summary>
        /// Every nodeId has a name, this is it.  
        /// </summary>
        public string Name
        {
            get
            {
                var ret = m_graph.m_types[(int)m_index].Name;
                if (ret == null && (int)m_index < m_graph.m_deferedTypes.Count)
                {
                    var info = m_graph.m_deferedTypes[(int)m_index];
                    if (m_graph.ResolveTypeName != null)
                    {
                        ret = m_graph.ResolveTypeName(info.TypeID, info.Module);
                        if (info.TypeNameSuffix != null)
                        {
                            ret += info.TypeNameSuffix;
                        }

                        m_graph.m_types.UnderlyingArray[(int)m_index].Name = ret;
                    }
                    ret ??= $"TypeID(0x{info.TypeID:x})";
                }
                return ret;
            }
        }
        /// <summary>
        /// This is the ModuleName ! Name (or just Name if ModuleName does not exist)  
        /// </summary>
        public string FullName
        {
            get
            {
                var moduleName = ModuleName;
                if (moduleName == null)
                {
                    return Name;
                }

                if (moduleName.Length == 0) // TODO should we have this convention?   
                {
                    moduleName = "?";
                }

                return $"{moduleName}!{Name}";
            }
        }
        /// <summary>
        /// Size is defined as the Size of the first node in the graph of a given nodeId.   
        /// For types that always have the same Size this is useful, but for types (like arrays or strings)
        /// that have variable Size, it is not useful.  
        /// 
        /// TODO keep track if the nodeId is of variable Size
        /// </summary>
        public int Size { get { return m_graph.m_types[(int)m_index].Size; } }
        public NodeTypeIndex Index { get { return m_index; } }
        public Graph Graph { get { return m_graph; } }
        /// <summary>
        /// The module associated with the type.  Can be null.  Typically this is the full path name.  
        /// </summary>
        public string ModuleName
        {
            get
            {
                var ret = m_graph.m_types[(int)m_index].ModuleName;
                if (ret == null && (int)m_index < m_graph.m_deferedTypes.Count)
                {
                    var module = m_graph.m_deferedTypes[(int)m_index].Module;
                    if (module != null)
                    {
                        ret = module.Path;
                    }
                }
                return ret;
            }
            set
            {
                var typeInfo = m_graph.m_types[(int)m_index];
                typeInfo.ModuleName = value;
                m_graph.m_types[(int)m_index] = typeInfo;
            }
        }
        public Module Module { get { return m_graph.m_deferedTypes[(int)m_index].Module; } }
        public int RawTypeID { get { return m_graph.m_deferedTypes[(int)m_index].TypeID; } }

        public override string ToString()
        {
            StringWriter sw = new StringWriter();
            WriteXml(sw);
            return sw.ToString();
        }
        public void WriteXml(TextWriter writer, string prefix = "")
        {
            writer.WriteLine("{0}<NodeType Index=\"{1}\" Name=\"{2}\"/>", prefix, (int)Index, SecurityElement.Escape(Name));
        }
        #region private
        protected internal NodeType(Graph graph)
        {
            m_graph = graph;
            m_index = NodeTypeIndex.Invalid;
        }

        internal Graph m_graph;
        internal NodeTypeIndex m_index;
        #endregion
    }
}
