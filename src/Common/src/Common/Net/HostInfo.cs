// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

namespace Steeltoe.Common.Net;

internal sealed class HostInfo
{
    public string Hostname { get; }
    public string IPAddress { get; }

    public HostInfo(string hostname, string ipAddress)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hostname);
        ArgumentException.ThrowIfNullOrWhiteSpace(ipAddress);

        Hostname = hostname;
        IPAddress = ipAddress;
    }
}
