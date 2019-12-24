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

using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace Steeltoe.Common.Net
{
    public static class DnsTools
    {
        /// <summary>
        /// Get the first listed <see cref="AddressFamily.InterNetwork" /> for the host name
        /// </summary>
        /// <param name="hostName">The host name or address to use</param>
        /// <returns>String representation of the IP Address or <see langword="null"/></returns>
        public static string ResolveHostAddress(string hostName)
        {
            try
            {
                return Dns.GetHostAddresses(hostName).FirstOrDefault(ip => ip.AddressFamily.Equals(AddressFamily.InterNetwork))?.ToString();
            }
            catch (Exception)
            {
                // Ignore
                return null;
            }
        }

        public static string ResolveHostName()
        {
            string result = null;
            try
            {
                result = Dns.GetHostName();
                if (!string.IsNullOrEmpty(result))
                {
                    var response = Dns.GetHostEntry(result);
                    if (response != null)
                    {
                        return response.HostName;
                    }
                }
            }
            catch (Exception)
            {
                // Ignore
            }

            return result;
        }
    }
}
