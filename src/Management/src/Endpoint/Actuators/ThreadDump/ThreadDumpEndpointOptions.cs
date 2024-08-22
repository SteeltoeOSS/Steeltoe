// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Management.Configuration;

namespace Steeltoe.Management.Endpoint.Actuators.ThreadDump;

public sealed class ThreadDumpEndpointOptions : EndpointOptions
{
    /// <summary>
    /// Gets or sets the duration (in milliseconds) before signaling to stop the capture. Default value: 10.
    /// </summary>
    public int Duration { get; set; } = 10;
}
