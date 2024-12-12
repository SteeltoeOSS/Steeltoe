// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Management.Configuration;

namespace Steeltoe.Management.Endpoint.Actuators.HeapDump;

public sealed class HeapDumpEndpointOptions : EndpointOptions
{
    /// <summary>
    /// Gets or sets the type of dump to create. Possible values: GcDump, Normal, WithHeap, Triage, Full. Default value: Full.
    /// </summary>
    public string? HeapDumpType { get; set; }
}
