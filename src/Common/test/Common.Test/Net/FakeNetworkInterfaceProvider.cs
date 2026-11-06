// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using Steeltoe.Common.Net;

namespace Steeltoe.Common.Test.Net;

internal sealed class FakeNetworkInterfaceProvider : INetworkInterfaceProvider
{
    private readonly List<NetworkInterfaceSnapshot> _networkInterfaces = [];

    public Exception? ErrorInGetAllNetworkInterfaces { get; set; }
    public string? ReverseLookupHostName { get; set; }
    public Exception? ErrorInResolveHostName { get; set; }
    public int ResolveHostNameCallCount { get; private set; }

    public void Add(string name, bool isUp, bool isReceiveOnly, int ipv4Index, params IPAddress[] unicastAddresses)
    {
        var snapshot = new NetworkInterfaceSnapshot(name, $"{name}-id", isUp, isReceiveOnly, ipv4Index, unicastAddresses);
        _networkInterfaces.Add(snapshot);
    }

    public IReadOnlyList<NetworkInterfaceSnapshot> GetAllNetworkInterfaces()
    {
        if (ErrorInGetAllNetworkInterfaces != null)
        {
            throw ErrorInGetAllNetworkInterfaces;
        }

        return _networkInterfaces;
    }

    public string ResolveHostName(IPAddress address)
    {
        ResolveHostNameCallCount++;

        if (ErrorInResolveHostName != null)
        {
            throw ErrorInResolveHostName;
        }

        if (ReverseLookupHostName == null)
        {
            throw new InvalidOperationException($"{nameof(ReverseLookupHostName)} was not configured.");
        }

        return ReverseLookupHostName;
    }
}
