// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Net.NetworkInformation;

namespace Steeltoe.Common.Net;

internal sealed class NetworkInterfaceProvider : INetworkInterfaceProvider
{
    public static NetworkInterfaceProvider Instance { get; } = new();

    private NetworkInterfaceProvider()
    {
    }

    public IReadOnlyList<NetworkInterfaceSnapshot> GetAllNetworkInterfaces()
    {
        NetworkInterface[] networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
        var snapshots = new List<NetworkInterfaceSnapshot>(networkInterfaces.Length);

        foreach (NetworkInterface networkInterface in networkInterfaces)
        {
            bool isUp = networkInterface.OperationalStatus == OperationalStatus.Up;
            bool isReceiveOnly = networkInterface.IsReceiveOnly;
            int ipv4Index = -1;
            IReadOnlyList<IPAddress> unicastAddresses = Array.Empty<IPAddress>();

            if (isUp && !isReceiveOnly)
            {
                IPInterfaceProperties properties = networkInterface.GetIPProperties();
                ipv4Index = properties.GetIPv4Properties().Index;
                unicastAddresses = properties.UnicastAddresses.Select(addressInfo => addressInfo.Address).ToArray();
            }

            snapshots.Add(new NetworkInterfaceSnapshot(networkInterface.Name, networkInterface.Id, isUp, isReceiveOnly, ipv4Index, unicastAddresses));
        }

        return snapshots;
    }

    public string ResolveHostName(IPAddress address)
    {
        ArgumentNullException.ThrowIfNull(address);

        // Warning: this might take a few seconds...
        IPHostEntry hostEntry = Dns.GetHostEntry(address);
        return hostEntry.HostName;
    }
}
