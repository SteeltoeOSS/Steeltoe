// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Management.Configuration;

namespace Steeltoe.Management.Endpoint.Actuators.ThreadDump;

public sealed class ThreadDumpEndpointOptions : EndpointOptions
{
    /// <summary>
    /// Gets or sets the permissions required to access this endpoint, when running on Cloud Foundry. Default value: Full.
    /// </summary>
    public override EndpointPermissions RequiredPermissions { get; set; } = EndpointPermissions.Full;

    /// <summary>
    /// Gets or sets the time (in milliseconds) to trace for, before automatically stopping the trace. Default value: 100.
    /// </summary>
    /// <remarks>
    /// This is how long the EventPipe session captures samples from the "Microsoft-DotNETCore-SampleProfiler" provider, which samples on-CPU threads at a
    /// fixed ~1 ms interval. Threads that are not scheduled on a CPU core during this window (for example, because they are idle or blocked waiting on I/O)
    /// may get few or no samples, so a short duration risks missing threads entirely. A longer duration increases the odds of observing such threads, at the
    /// cost of the endpoint taking longer to respond.
    /// </remarks>
    public int Duration { get; set; } = 100;
}
