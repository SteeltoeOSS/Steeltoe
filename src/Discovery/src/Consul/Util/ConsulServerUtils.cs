// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Net.Sockets;
using System.Text;
using Consul;

namespace Steeltoe.Discovery.Consul.Util;

internal static class ConsulServerUtils
{
    public static string FindHost(ServiceEntry healthService)
    {
        AgentService service = healthService.Service;
        Node node = healthService.Node;

        if (!string.IsNullOrWhiteSpace(service.Address))
        {
            return FixIPv6Address(service.Address);
        }

        if (!string.IsNullOrWhiteSpace(node.Address))
        {
            return FixIPv6Address(node.Address);
        }

        return node.Address;
    }

    public static string FixIPv6Address(string address)
    {
        if (IPAddress.TryParse(address, out IPAddress parsed) && parsed.AddressFamily == AddressFamily.InterNetworkV6)
        {
            byte[] bytes = parsed.GetAddressBytes();
            var sb = new StringBuilder("[");

            for (int i = 0; i < bytes.Length; i += 2)
            {
                ushort num = (ushort)((bytes[i] << 8) | bytes[i + 1]);
                sb.Append($"{num:x}:");
            }

            sb.Replace(':', ']', sb.Length - 1, 1);
            return sb.ToString();
        }

        return address;
    }
}
