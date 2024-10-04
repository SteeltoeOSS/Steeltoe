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
    /// Holds all interesting data about a module (in particular enough to look up PDB information)
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class Module : IFastSerializable
    {
        /// <summary>
        /// Create new module.  You must have at least a image base.   Everything else is optional.
        /// </summary>
        public Module(Address imageBase) { ImageBase = imageBase; }

        /// <summary>
        /// The path to the Module (can be null if not known)
        /// </summary>
        public string Path;
        /// <summary>
        /// The location where the image was loaded into memory
        /// </summary>
        public Address ImageBase;
        /// <summary>
        /// The size of the image when loaded in memory
        /// </summary>
        public int Size;
        /// <summary>
        /// The time when this image was built (There is a field in the PE header).   May be MinimumValue if unknown. 
        /// </summary>
        public DateTime BuildTime;      // From in the PE header
        /// <summary>
        /// The name of hte PDB file associated with this module.   Ma bye null if unknown
        /// </summary>
        public string PdbName;
        /// <summary>
        /// The GUID that uniquely identifies this PDB for symbol server lookup.  May be Guid.Empty it not known.  
        /// </summary>
        public Guid PdbGuid;            // PDB Guid 
        /// <summary>
        /// The age (version number) that is used for symbol server lookup.  
        /// </summary>T
        public int PdbAge;

        #region private
        /// <summary>
        /// Implementing IFastSerializable interface.  
        /// </summary>
        public void ToStream(Serializer serializer)
        {
            serializer.Write(Path);
            serializer.Write((long)ImageBase);
            serializer.Write(Size);
            serializer.Write(BuildTime.Ticks);
            serializer.Write(PdbName);
            serializer.Write(PdbGuid);
            serializer.Write(PdbAge);
        }
        /// <summary>
        /// Implementing IFastSerializable interface.  
        /// </summary>
        public void FromStream(Deserializer deserializer)
        {
            deserializer.Read(out Path);
            ImageBase = (Address)deserializer.ReadInt64();
            deserializer.Read(out Size);
            BuildTime = new DateTime(deserializer.ReadInt64());
            deserializer.Read(out PdbName);
            deserializer.Read(out PdbGuid);
            deserializer.Read(out PdbAge);
        }
        #endregion
    }
}
