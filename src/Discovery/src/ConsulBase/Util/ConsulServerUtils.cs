﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Consul;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Steeltoe.Discovery.Consul.Util
{
    public static class ConsulServerUtils
    {
        public static string FindHost(ServiceEntry healthService)
        {
            var service = healthService.Service;
            var node = healthService.Node;

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
            if (IPAddress.TryParse(address, out IPAddress parsed) &&
                parsed.AddressFamily == AddressFamily.InterNetworkV6)
            {
                var bytes = parsed.GetAddressBytes();
                StringBuilder sb = new StringBuilder("[");
                for (int i = 0; i < bytes.Length; i = i + 2)
                {
                    ushort num = (ushort)((bytes[i] << 8) | bytes[i + 1]);
                    sb.Append(num.ToString("x") + ":");
                }

                sb.Replace(':', ']', sb.Length - 1, 1);
                return sb.ToString();
            }
            else
            {
                // Log
            }

            return address;
        }

        public static IDictionary<string, string> GetMetadata(ServiceEntry healthService)
        {
            return GetMetadata(healthService.Service.Tags);
        }

        public static IDictionary<string, string> GetMetadata(IList<string> tags)
        {
            var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (tags != null)
            {
                foreach (var tag in tags)
                {
                    var index = tag.IndexOf('=');
                    string key, value;
                    if (index == -1 || Equals(index + 1, tag.Length))
                    {
                        key = value = tag;
                    }
                    else
                    {
                        key = tag.Substring(0, index);
                        value = tag.Substring(index + 1);
                    }

                    metadata[key] = value;
                }
            }

            return metadata;
        }
    }
}