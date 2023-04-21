// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common;

namespace Steeltoe.Management.Endpoint.HeapDump;

public sealed class HeapDumpEndpointOptions : EndpointOptionsBase
{
    public string HeapDumpType { get; set; }

    // Default to disabled on Linux + Cloud Foundry until PTRACE is allowed
    public override bool DefaultEnabled { get; } = !(Platform.IsCloudFoundry && Platform.IsLinux);
}
