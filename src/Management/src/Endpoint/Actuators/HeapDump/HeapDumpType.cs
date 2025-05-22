// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.Endpoint.Actuators.HeapDump;

public enum HeapDumpType
{
    /// <summary>
    /// The largest dump containing all memory including the module images.
    /// </summary>
    Full,

    /// <summary>
    /// A large and relatively comprehensive dump containing module lists, thread lists, all stacks, exception information, handle information, and all
    /// memory except for mapped images.
    /// </summary>
    Heap,

    /// <summary>
    /// A small dump containing module lists, thread lists, exception information, and all stacks.
    /// </summary>
    Mini,

    /// <summary>
    /// A small dump containing module lists, thread lists, exception information, all stacks and PII removed.
    /// </summary>
    Triage,

    /// <summary>
    /// A Garbage Collector dump, created by triggering a GC, turning on special events, and regenerating the graph of object roots from the event stream.
    /// </summary>
    GCDump
}
