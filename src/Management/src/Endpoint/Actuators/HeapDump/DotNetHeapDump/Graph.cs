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
    /// A graph is representation of a node-arc graph.    It tries to be very space efficient.   It is a little
    /// more complex than the  most basic node-arc graph in that each node can have a code:NodeType associated with it 
    /// that contains information that is shared among many nodes.   
    /// 
    /// While the 'obvious' way of representing a graph is to have a 'Node' object that has arcs, we don't do this. 
    /// Instead each node is give an unique code:NodeIndex which represents the node and each node has a list of
    /// NodeIndexes for each of the children.   Using indexes instead of object pointers is valuable because
    /// 
    ///     * You can save 8 bytes (on 32 bit) of .NET object overhead and corresponding cost at GC time by using
    ///       indexes.   This is significant because there can be 10Meg of objects, so any expense adds up
    ///     * Making the nodes be identified by index is more serialization friendly.   It is easier to serialize
    ///       the graph if it has this representation.  
    ///     * It easily allows 3rd parties to 'attach' their own information to each node.  All they need is to
    ///       create an array of the extra information indexed by NodeIndex.   The 'NodeIndexLimit' is designed
    ///       specifically for this purpose.  
    ///       
    /// Because we anticipate VERY large graphs (e.g. dumps of the GC heap) the representation for the nodes is 
    /// very space efficient and we don't have code:Node class object for most of the nodes in the graph.  However
    /// it IS useful to have code:Node objects for the nodes that are being manipulated locally.  
    ///
    /// To avoid creating lots of code:Node objects that die quickly the API adopts the convention that the
    /// CALLer provides a code:Node class as 'storage' when the API needs to return a code:Node.   That way
    /// APIs that return code:Node never allocate.    This allows most graph algorithms to work without having
    /// to allocate more than a handful of code:Node classes, reducing overhead.   You allocate these storage
    /// nodes with the code:Graph.AllocNodeStorage call
    /// 
    /// Thus the basic flow is you call code:Graph.AllocNodeStorage to allocate storage, then call code:Graph.GetRoot
    /// to get your first node.  If you need to provide additional information about the nodes, you can allocate an auxiliary
    /// array of Size code:Graph.NodeIndexLimit to hold it (for example a 'visited' bit).   Then repeatedly call 
    /// code:Node.GetFirstChild, code:Node.GetNextChild to get the children of a node to traverse the graph.
    /// 
    /// OVERHEAD
    ///
    ///     1) 4 bytes per Node for the pointer to the stream for the rest of the data (thus we can have at most 4Gig nodes)
    ///     2) For each node, the number of children, the nodeId, and children are stored as compressed (relative) indexes 
    ///        (figure 1 byte for # of children, 2 bytes per type id, 2-3 bytes per child)
    ///     3) Variable length nodes also need a compressed int for the Size of the node (1-3 bytes)
    ///     4) Types store the name (2 bytes per character), and Size (4 bytes), but typically don't dominate 
    ///        Size of graph.  
    ///
    /// Thus roughly 7 bytes per node + 3 bytes per reference.   Typically nodes on average have 2-3 references, so
    /// figure 13-16 bytes per node.  That gives you 125 Million nodes in a 2 Gig of Memory. 
    /// 
    /// The important point here is that representation of a node is always smaller than the Memory it represents, and
    /// and often significantly smaller (since it does not hold non-GC data, null pointers and even non-null pointers 
    /// are typically half the Size).   For 64 bit heaps, the Size reduction is even more dramatic.  
    /// 
    /// see code:Graph.SizeOfGraphDescription to determine the overhead for any particular graph.
    /// 
    /// </summary>
    [ExcludeFromCodeCoverage]
    internal abstract class Graph : IFastSerializable, IFastSerializableVersion
    {
        /// <summary>
        /// Given an arbitrary code:NodeIndex that identifies the node, Get a code:Node object.  
        /// 
        /// This routine does not allocated but uses the space passed in by 'storage.  
        /// 'storage' should be allocated with code:AllocNodeStorage, and should be aggressively reused.  
        /// </summary>
        public Node GetNode(NodeIndex nodeIndex, Node storage)
        {
            Debug.Assert(storage.m_graph == this);
            storage.m_index = nodeIndex;
            return storage;
        }
        /// <summary>
        /// returns true if SetNode has been called on this node (it is not an undefined object).  
        /// TODO FIX NOW used this instead of the weird if node index grows technique. 
        /// </summary>
        public bool IsDefined(NodeIndex nodeIndex) { return m_nodes[(int)nodeIndex] != m_undefinedObjDef; }
        /// <summary>
        /// Given an arbitrary code:NodeTypeIndex that identifies the nodeId of the node, Get a code:NodeType object.  
        /// 
        /// This routine does not allocated but overwrites the space passed in by 'storage'.  
        /// 'storage' should be allocated with code:AllocNodeTypeStorage, and should be aggressively reused.  
        /// 
        /// Note that this routine does not get used much, instead Node.GetType is normal way of getting the nodeId.  
        /// </summary>
        public NodeType GetType(NodeTypeIndex nodeTypeIndex, NodeType storage)
        {
            storage.m_index = nodeTypeIndex;
            Debug.Assert(storage.m_graph == this);
            return storage;
        }

        // Storage allocation
        /// <summary>
        /// Allocates nodes to be used as storage for methods like code:GetRoot, code:Node.GetFirstChild and code:Node.GetNextChild
        /// </summary>
        /// <returns></returns>
        public virtual Node AllocNodeStorage()
        {
            return new Node(this);
        }
        /// <summary>
        /// Allocates nodes to be used as storage for methods like code:GetType
        /// </summary>
        public virtual NodeType AllocTypeNodeStorage()
        {
            return new NodeType(this);
        }

        /// <summary>
        /// It is expected that users will want additional information associated with nodes of the graph.  They can
        /// do this by allocating an array of code:NodeIndexLimit and then indexing this by code:NodeIndex
        /// </summary>
        public NodeIndex NodeIndexLimit { get { return (NodeIndex)m_nodes.Count; } }
        /// <summary>
        /// Same as NodeIndexLimit, just cast to an integer.  
        /// </summary>
        public int NodeCount { get { return (int)m_nodes.Count; } }
        /// <summary>
        /// It is expected that users will want additional information associated with TYPES of the nodes of the graph.  They can
        /// do this by allocating an array of code:NodeTypeIndexLimit and then indexing this by code:NodeTypeIndex
        /// </summary>
        public NodeTypeIndex NodeTypeIndexLimit { get { return (NodeTypeIndex)m_types.Count; } }
        /// <summary>
        /// Same as NodeTypeIndex cast as an integer.  
        /// </summary>
        public int NodeTypeCount { get { return m_types.Count; } }
        /// <summary>
        /// When a Node is created, you specify how big it is.  This the sum of all those sizes.  
        /// </summary>
        public long TotalSize { get { return m_totalSize; } }
        /// <summary>
        /// The number of references (arcs) in the graph
        /// </summary>
        public int TotalNumberOfReferences { get { return m_totalRefs; } }
        /// <summary>
        /// Specifies the size of each segment in the segmented list.
        /// However, this value must be a power of two or the list will throw an exception.
        /// Considering this requirement and the size of each element as 8 bytes,
        /// the current value will keep its size at approximately 64K.
        /// Having a lesser size than 85K will keep the segments out of the Large Object Heap,
        /// permitting the GC to free up memory by compacting the segments within the heap.
        /// </summary>
        protected const int SegmentSize = 8_192;

        // Creation methods.  
        /// <summary>
        /// Create a new graph from 'nothing'.  Note you are not allowed to read from the graph
        /// until you execute 'AllowReading'.  
        /// 
        /// You can actually continue to write after executing 'AllowReading' however you should
        /// any additional nodes you write should not be accessed until you execute 'AllowReading'
        /// again.  
        /// 
        /// TODO I can eliminate the need for AllowReading.  
        /// </summary>
        public Graph(int expectedNodeCount)
        {
            m_expectedNodeCount = expectedNodeCount;
            m_types = new GrowableArray<TypeInfo>(Math.Max(expectedNodeCount / 100, 2000));
            m_nodes = new SegmentedList<StreamLabel>(SegmentSize, m_expectedNodeCount);
            RootIndex = NodeIndex.Invalid;
            ClearWorker();
        }
        /// <summary>
        /// The NodeIndex of the root node of the graph.   It must be set sometime before calling AllowReading
        /// </summary>
        public NodeIndex RootIndex;
        /// <summary>
        /// Create a new nodeId with the given name and return its node nodeId index.   No interning is done (thus you can
        /// have two distinct NodeTypeIndexes that have exactly the same name.  
        /// 
        /// By default the size = -1 which indicates we will set the type size to the first 'SetNode' for this type.  
        /// </summary>
        public virtual NodeTypeIndex CreateType(string name, string moduleName = null, int size = -1)
        {
            var ret = (NodeTypeIndex)m_types.Count;
            TypeInfo typeInfo = new TypeInfo();
            typeInfo.Name = name;
            typeInfo.ModuleName = moduleName;
            typeInfo.Size = size;
            m_types.Add(typeInfo);
            return ret;
        }
        /// <summary>
        /// Create a new node and return its index.   It is undefined until code:SetNode is called.   We allow undefined nodes
        /// because graphs have loops in them, and thus you need to refer to a node, before you know all the data in the node.
        /// 
        /// It is really expected that every node you did code:CreateNode on you also ultimately do a code:SetNode on.  
        /// </summary>
        /// <returns></returns>
        public virtual NodeIndex CreateNode()
        {
            var ret = (NodeIndex)m_nodes.Count;
            m_nodes.Add(m_undefinedObjDef);
            return ret;
        }
        /// <summary>
        /// Sets the information associated with the node at 'nodeIndex' (which was created via code:CreateNode).  Nodes
        /// have a nodeId, Size and children.  (TODO: should Size be here?)
        /// </summary>
        public void SetNode(NodeIndex nodeIndex, NodeTypeIndex typeIndex, int sizeInBytes, GrowableArray<NodeIndex> children)
        {
            SetNodeTypeAndSize(nodeIndex, typeIndex, sizeInBytes);

            Node.WriteCompressedInt(m_writer, children.Count);
            for (int i = 0; i < children.Count; i++)
            {
                Node.WriteCompressedInt(m_writer, (int)children[i] - (int)nodeIndex);
            }
            m_totalRefs += children.Count;
        }

        /// <summary>
        /// When a graph is constructed with the default constructor, it is in 'write Mode'  You can't read from it until 
        /// you call 'AllowReading' which puts it in 'read mode'.  
        /// </summary>
        public virtual void AllowReading()
        {
            Debug.Assert(m_reader == null && m_writer != null);
            Debug.Assert(RootIndex != NodeIndex.Invalid);
            m_reader = m_writer.GetReader();
            m_writer = null;
            if (RootIndex == NodeIndex.Invalid)
            {
                throw new ApplicationException("RootIndex not set.");
            }
#if false
            // Validate that any referenced node was actually defined and that all node indexes are within range;
            var nodeStorage = AllocNodeStorage();
            for (NodeIndex nodeIndex = 0; nodeIndex < NodeIndexLimit; nodeIndex++)
            {
                var node = GetNode(nodeIndex, nodeStorage);
                Debug.Assert(node.Index != NodeIndex.Invalid);
                Debug.Assert(node.TypeIndex < NodeTypeIndexLimit);
                for (var childIndex = node.GetFirstChildIndex(); childIndex != null; childIndex = node.GetNextChildIndex())
                    Debug.Assert(0 <= childIndex && childIndex < NodeIndexLimit);
                if (!node.Defined)
                    Debug.WriteLine("Warning: undefined object " + nodeIndex);
            }
#endif
        }
        /// <summary>
        /// Used for debugging, returns the node Count and typeNode Count. 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return string.Format("Graph of {0} nodes and {1} types.  Size={2:f3}MB SizeOfDescription={3:f3}MB",
                NodeIndexLimit, NodeTypeIndexLimit, TotalSize / 1000000.0, SizeOfGraphDescription() / 1000000.0);
        }
        // Performance 
        /// <summary>
        /// A pretty good estimate of the how many bytes of Memory it takes just to represent the graph itself. 
        /// 
        /// TODO: Currently this is only correct for the 32 bit version.  
        /// </summary>
        public virtual long SizeOfGraphDescription()
        {
            if (m_reader == null)
            {
                return 0;
            }

            int sizeOfTypes = 0;
            int sizeOfTypeInfo = 8;
            for (int i = 0; i < m_types.Count; i++)
            {
                var typeName = m_types[i].Name;
                var typeNameLen = 0;
                if (typeName != null)
                {
                    typeNameLen = typeName.Length * 2;
                }

                sizeOfTypes += sizeOfTypeInfo + typeNameLen;
            }

            return sizeOfTypes + m_reader.Length + m_nodes.Count * 4;
        }

        /* APIs for deferred lookup of type names */
        /// <summary>
        /// Graph supports the ability to look up the names of a type at a later time.   You use this by 
        /// calling this overload in which you give a type ID (e.g. an RVA) and a module index (from 
        /// CreateModule) to this API.   If later you override the 'ResolveTypeName' delegate below
        /// then when type names are requested you will get back the typeID and module which you an
        /// then use to look up the name (when you do have the PDB). 
        /// 
        /// The Module passed should be reused as much as possible to avoid bloated files.  
        /// </summary>
        public NodeTypeIndex CreateType(int typeID, Module module, int size = -1, string typeNameSuffix = null)
        {
            // make sure the m_types and m_deferedTypes arrays are in sync.  
            while (m_deferedTypes.Count < m_types.Count)
            {
                m_deferedTypes.Add(new DeferedTypeInfo());
            }

            var ret = (NodeTypeIndex)m_types.Count;
            // We still use the m_types array for the size. 
            m_types.Add(new TypeInfo { Size = size });

            // but we put the real information into the m_deferedTypes.  
            m_deferedTypes.Add(new DeferedTypeInfo { Module = module, TypeID = typeID, TypeNameSuffix = typeNameSuffix });
            Debug.Assert(m_deferedTypes.Count == m_types.Count);
            return ret;
        }
        /// <summary>
        /// In advanced scenarios you may not be able to provide a type name when you create the type.  YOu can pass null
        /// for the type name to 'CreateType'   If you provide this callback, later you can provide the mapping from 
        /// type index to name (e.g. when PDBs are available).    Note that this field is NOT serialized.   
        /// </summary>
        public Func<int, Module, string> ResolveTypeName { get; set; }
        /// <summary>
        /// Where any types in the graph creates with the CreateType(int typeID, Module module, int size) overload?
        /// </summary>
        public bool HasDeferedTypeNames { get { return m_deferedTypes.Count > 0; } }

        /* See GraphUtils class for more things you can do with a Graph. */
        // TODO move these to GraphUtils. 
        // Utility (could be implemented using public APIs).  
        public void BreadthFirstVisit(Action<Node> visitor)
        {
            var nodeStorage = AllocNodeStorage();
            var visited = new bool[(int)NodeIndexLimit];
            var work = new Queue<NodeIndex>();
            work.Enqueue(RootIndex);
            while (work.Count > 0)
            {
                var nodeIndex = work.Dequeue();
                var node = GetNode(nodeIndex, nodeStorage);
                visitor(node);
                for (var childIndex = node.GetFirstChildIndex(); childIndex != NodeIndex.Invalid; childIndex = node.GetNextChildIndex())
                {
                    if (!visited[(int)childIndex])
                    {
                        visited[(int)childIndex] = true;
                        work.Enqueue(childIndex);
                    }
                }
            }
        }

        public SizeAndCount[] GetHistogramByType()
        {
            var ret = new SizeAndCount[(int)NodeTypeIndexLimit];
            for (int i = 0; i < ret.Length; i++)
            {
                ret[i] = new SizeAndCount((NodeTypeIndex)i);
            }

            var nodeStorage = AllocNodeStorage();
            for (NodeIndex idx = 0; idx < NodeIndexLimit; idx++)
            {
                var node = GetNode(idx, nodeStorage);
                var sizeAndCount = ret[(int)node.TypeIndex];
                sizeAndCount.Count++;
                sizeAndCount.Size += node.Size;
            }

            Array.Sort(ret, (x, y) => y.Size.CompareTo(x.Size));
#if DEBUG
            int totalCount = 0;
            long totalSize = 0;
            foreach (var sizeAndCount in ret)
            {
                totalCount += sizeAndCount.Count;
                totalSize += sizeAndCount.Size;
            }
            Debug.Assert(TotalSize == totalSize);
            Debug.Assert((int)NodeIndexLimit == totalCount);
#endif
            return ret;
        }
        public class SizeAndCount
        {
            public SizeAndCount(NodeTypeIndex typeIdx) { TypeIdx = typeIdx; }
            public readonly NodeTypeIndex TypeIdx;
            public long Size;
            public int Count;
        }
        public string HistogramByTypeXml(long minSize = 0)
        {
            var sizeAndCounts = GetHistogramByType();
            StringWriter sw = new StringWriter();
            sw.WriteLine("<HistogramByType Size=\"{0}\" Count=\"{1}\">", TotalSize, (int)NodeIndexLimit);
            var typeStorage = AllocTypeNodeStorage();
            foreach (var sizeAndCount in sizeAndCounts)
            {
                if (sizeAndCount.Size <= minSize)
                {
                    break;
                }

                sw.WriteLine("  <Type Name=\"{0}\" Size=\"{1}\" Count=\"{2}\"/>",
                    SecurityElement.Escape(GetType(sizeAndCount.TypeIdx, typeStorage).Name), sizeAndCount.Size, sizeAndCount.Count);
            }
            sw.WriteLine("</HistogramByType>");
            return sw.ToString();
        }

        #region private

        internal void SetNodeTypeAndSize(NodeIndex nodeIndex, NodeTypeIndex typeIndex, int sizeInBytes)
        {
            Debug.Assert(m_nodes[(int)nodeIndex] == m_undefinedObjDef, $"Calling SetNode twice for node index {nodeIndex}");
            m_nodes[(int)nodeIndex] = m_writer.GetLabel();

            Debug.Assert(sizeInBytes >= 0);
            // We are going to assume that if this is negative it is because it is a large positive number.  
            if (sizeInBytes < 0)
            {
                sizeInBytes = int.MaxValue;
            }

            int typeAndSize = (int)typeIndex << 1;
            TypeInfo typeInfo = m_types[(int)typeIndex];
            if (typeInfo.Size < 0)
            {
                typeInfo.Size = sizeInBytes;
                m_types[(int)typeIndex] = typeInfo;
            }
            if (typeInfo.Size == sizeInBytes)
            {
                Node.WriteCompressedInt(m_writer, typeAndSize);
            }
            else
            {
                typeAndSize |= 1;
                Node.WriteCompressedInt(m_writer, typeAndSize);
                Node.WriteCompressedInt(m_writer, sizeInBytes);
            }

            m_totalSize += sizeInBytes;
        }

        /// <summary>
        /// Clear handles puts it back into the state that existed after the constructor returned
        /// </summary>
        protected virtual void Clear()
        {
            ClearWorker();
        }

        /// <summary>
        /// ClearWorker does only that part of clear needed for this level of the hierarchy (and needs
        /// to be done by the constructor too). 
        /// </summary>
        private void ClearWorker()
        {
            RootIndex = NodeIndex.Invalid;
            m_writer ??= new SegmentedMemoryStreamWriter(m_expectedNodeCount * 8);

            m_totalSize = 0;
            m_totalRefs = 0;
            m_types.Count = 0;
            m_writer.Clear();
            m_nodes.Count = 0;

            // Create an undefined node, kind of gross because SetNode expects to have an entry
            // in the m_nodes table, so we make a fake one and then remove it.  
            m_undefinedObjDef = m_writer.GetLabel();
            m_nodes.Add(m_undefinedObjDef);
            SetNode(0, CreateType("UNDEFINED"), 0, new GrowableArray<NodeIndex>());
            Debug.Assert(m_nodes[0] == m_undefinedObjDef);
            m_nodes.Count = 0;
        }

        // To support very space efficient encodings, and to allow for easy serialization (persistence to file)
        // Types are given an index and their data is stored in a m_types array.  TypeInfo is the data in this
        // array.  
        internal struct TypeInfo
        {
            public string Name;                         // If DeferredTypeInfo.Module != null then this is a type name suffix.  
            public int Size;
            public string ModuleName;                   // The name of the module which contains the type (if known).  
        }
        internal struct DeferedTypeInfo
        {
            public int TypeID;
            public Module Module;                       // The name of the module which contains the type (if known).
            public string TypeNameSuffix;               // if non-null it is added to the type name as a suffix.   
        }

        public virtual void ToStream(Serializer serializer)
        {
            serializer.Write(m_totalSize);
            serializer.Write((int)RootIndex);
            // Write out the Types 
            serializer.Write(m_types.Count);
            for (int i = 0; i < m_types.Count; i++)
            {
                serializer.Write(m_types[i].Name);
                serializer.Write(m_types[i].Size);
                serializer.Write(m_types[i].ModuleName);
            }

            // Write out the Nodes 
            serializer.Write(m_nodes.Count);
            for (int i = 0; i < m_nodes.Count; i++)
            {
                serializer.Write((int)m_nodes[i]);
            }

            // Write out the Blob stream.  
            // TODO this is inefficient.  Also think about very large files.  
            int readerLen = (int)m_reader.Length;
            serializer.Write(readerLen);
            m_reader.Goto((StreamLabel)0);
            for (uint i = 0; i < readerLen; i++)
            {
                serializer.Write(m_reader.ReadByte());
            }

            // Are we writing a format for 1 or greater?   If so we can use the new (breaking) format, otherwise
            // to allow old readers to read things, we give up on the new data.  
            if (1 <= ((IFastSerializableVersion)this).MinimumReaderVersion)
            {
                // Because Graph has superclass, you can't add objects to the end of it (since it is not 'the end' of the object)
                // which is a problem if we want to add new fields.  We could have had a worker object but another way of doing
                // it is create a deferred (lazy region).   The key is that ALL readers know how to skip this region, which allows
                // you to add new fields 'at the end' of the region (just like for sealed objects).  
                DeferedRegion expansion = new DeferedRegion();
                expansion.Write(serializer, delegate
                {
                    // I don't need to use Tagged types for my 'first' version of this new region 
                    serializer.Write(m_deferedTypes.Count);
                    for (int i = 0; i < m_deferedTypes.Count; i++)
                    {
                        serializer.Write(m_deferedTypes[i].TypeID);
                        serializer.Write(m_deferedTypes[i].Module);
                        serializer.Write(m_deferedTypes[i].TypeNameSuffix);
                    }

                    // You can place tagged values in here always adding right before the WriteTaggedEnd
                    // for any new fields added after version 1 
                    serializer.WriteTaggedEnd(); // This insures tagged things don't read junk after the region.  
                });
            }
        }

        public void FromStream(Deserializer deserializer)
        {
            deserializer.Read(out m_totalSize);
            RootIndex = (NodeIndex)deserializer.ReadInt();

            // Read in the Types 
            TypeInfo info = new TypeInfo();
            int typeCount = deserializer.ReadInt();
            m_types = new GrowableArray<TypeInfo>(typeCount);
            for (int i = 0; i < typeCount; i++)
            {
                deserializer.Read(out info.Name);
                deserializer.Read(out info.Size);
                deserializer.Read(out info.ModuleName);
                m_types.Add(info);
            }

            // Read in the Nodes 
            int nodeCount = deserializer.ReadInt();
            m_nodes = new SegmentedList<StreamLabel>(SegmentSize, nodeCount);

            for (int i = 0; i < nodeCount; i++)
            {
                m_nodes.Add((StreamLabel)(uint)deserializer.ReadInt());
            }

            // Read in the Blob stream.  
            // TODO be lazy about reading in the blobs.  
            int blobCount = deserializer.ReadInt();
            SegmentedMemoryStreamWriter writer = new SegmentedMemoryStreamWriter(blobCount);
            while (8 <= blobCount)
            {
                writer.Write(deserializer.ReadInt64());
                blobCount -= 8;
            }
            while(0 < blobCount)
            {
                writer.Write(deserializer.ReadByte());
                --blobCount;
            }

            m_reader = writer.GetReader();

            // Stuff added in version 1.   See Version below 
            if (1 <= deserializer.MinimumReaderVersionBeingRead)
            {
                // Because Graph has superclass, you can't add objects to the end of it (since it is not 'the end' of the object)
                // which is a problem if we want to add new fields.  We could have had a worker object but another way of doing
                // it is create a deferred (lazy region).   The key is that ALL readers know how to skip this region, which allows
                // you to add new fields 'at the end' of the region (just like for sealed objects).  
                DeferedRegion expansion = new DeferedRegion();
                expansion.Read(deserializer, delegate
                {
                    // I don't need to use Tagged types for my 'first' version of this new region 
                    int count = deserializer.ReadInt();
                    for (int i = 0; i < count; i++)
                    {
                        m_deferedTypes.Add(new DeferedTypeInfo
                        {
                            TypeID = deserializer.ReadInt(),
                            Module = (Module)deserializer.ReadObject(),
                            TypeNameSuffix = deserializer.ReadString()
                        });
                    }

                    // You can add any tagged objects here after version 1.   You can also use the deserializer.VersionBeingRead
                    // to avoid reading non-existent fields, but the tagging is probably better.   
                });
                expansion.FinishRead(true);  // Immediately read in the fields, preserving the current position in the stream.     
            }
        }

        // These three members control the versioning of the Graph format on disk.   
        public int Version { get { return 1; } }                            // The version of what was written.  It is in the file.       
        public int MinimumVersionCanRead { get { return 0; } }              // Declaration of the oldest format this code can read
        public int MinimumReaderVersion                                     // Will cause readers to fail if their code version is less than this.  
        {
            get
            {
                if (m_deferedTypes.Count != 0)
                {
                    return 1;    // We require that you upgrade to version 1 if you use m_deferedTypes (e.g. projectN)   
                }

                return 0;
            }
        }

        private int m_expectedNodeCount;                // Initial guess at graph Size. 
        private long m_totalSize;                       // Total Size of all the nodes in the graph.  
        internal int m_totalRefs;                       // Total Number of references in the graph
        internal GrowableArray<TypeInfo> m_types;       // We expect only thousands of these
        internal GrowableArray<DeferedTypeInfo> m_deferedTypes; // Types that we only have IDs and module image bases.
        internal SegmentedList<StreamLabel> m_nodes;    // We expect millions of these.  points at a serialize node in m_reader
        internal SegmentedMemoryStreamReader m_reader; // This is the actual data for the nodes.  Can be large
        internal StreamLabel m_undefinedObjDef;         // a node of nodeId 'Unknown'.   New nodes start out pointing to this
        // and then can be set to another nodeId (needed when there are cycles).
        // There should not be any of these left as long as every node referenced
        // by another node has a definition.
        internal SegmentedMemoryStreamWriter m_writer; // Used only during construction to serialize the nodes.
        #endregion
    }

    /// <summary>
    /// Each node is given a unique index (which is dense: an array is a good lookup structure).   
    /// To avoid passing the wrong indexes to methods, we make an enum for each index.   This does
    /// mean you need to cast away this strong typing occasionally (e.g. when you index arrays)
    /// However on the whole it is a good tradeoff.  
    /// </summary>
    public enum NodeIndex { Invalid = -1 }
    /// <summary>
    /// Each node nodeId is given a unique index (which is dense: an array is a good lookup structure).   
    /// To avoid passing the wrong indexes to methods, we make an enum for each index.   This does
    /// mean you need to cast away this strong typing occasionally (e.g. when you index arrays)
    /// However on the whole it is a good tradeoff.  
    /// </summary>    
    public enum NodeTypeIndex { Invalid = -1 }
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
