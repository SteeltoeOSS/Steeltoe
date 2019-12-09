// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

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
            try
            {
                var parsed = IPAddress.Parse(address);
                if (parsed.AddressFamily == AddressFamily.InterNetworkV6)
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
            }
            catch (Exception)
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