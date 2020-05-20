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

using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace Steeltoe.Common.Net
{
    public class InetUtils
    {
        private readonly InetOptions _options;
        private readonly ILogger _logger;

        public InetUtils(InetOptions options, ILogger logger = null)
        {
            _options = options;
            _logger = logger;
        }

        public virtual HostInfo FindFirstNonLoopbackHostInfo()
        {
            var address = FindFirstNonLoopbackAddress();
            if (address != null)
            {
                return ConvertAddress(address);
            }

            HostInfo hostInfo = new HostInfo();
            hostInfo.Hostname = _options.DefaultHostname;
            hostInfo.IpAddress = _options.DefaultIpAddress;
            return hostInfo;
        }

        public IPAddress FindFirstNonLoopbackAddress()
        {
            IPAddress result = null;
            try
            {
                int lowest = int.MaxValue;
                var ifaces = NetworkInterface.GetAllNetworkInterfaces();
                for (int i = 0; i < ifaces.Length; i++)
                {
                    var ifc = ifaces[i];

                    if (ifc.OperationalStatus == OperationalStatus.Up && !ifc.IsReceiveOnly)
                    {
                        _logger?.LogTrace("Testing interface: {name}, {id}", ifc.Name, ifc.Id);

                        var props = ifc.GetIPProperties();
                        var ipprops = props.GetIPv4Properties();

                        if (ipprops.Index < lowest || result == null)
                        {
                            lowest = ipprops.Index;
                        }
                        else if (result != null)
                        {
                            continue;
                        }

                        if (!IgnoreInterface(ifc.Name))
                        {
                            foreach (var addressInfo in props.UnicastAddresses)
                            {
                                var address = addressInfo.Address;
                                if (IsInet4Address(address)
                                    && !IsLoopbackAddress(address)
                                    && IsPreferredAddress(address))
                                {
                                    _logger?.LogTrace("Found non-loopback interface: {name}", ifc.Name);
                                    result = address;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Cannot get first non-loopback address");
            }

            if (result != null)
            {
                return result;
            }

            return GetHostAddress();
        }

        internal bool IsInet4Address(IPAddress address)
        {
            return address.AddressFamily == AddressFamily.InterNetwork;
        }

        internal bool IsLoopbackAddress(IPAddress address)
        {
            return IPAddress.IsLoopback(address);
        }

        internal bool IsPreferredAddress(IPAddress address)
        {
            if (_options.UseOnlySiteLocalInterfaces)
            {
                bool siteLocalAddress = IsSiteLocalAddress(address);
                if (!siteLocalAddress)
                {
                    _logger?.LogTrace("Ignoring address: {address} ", address.ToString());
                }

                return siteLocalAddress;
            }

            IEnumerable<string> preferredNetworks = _options.GetPreferredNetworks();
            if (!preferredNetworks.Any())
            {
                return true;
            }

            foreach (string regex in preferredNetworks)
            {
                string hostAddress = address.ToString();
                var matcher = new Regex(regex);
                if (matcher.IsMatch(hostAddress) || hostAddress.StartsWith(regex))
                {
                    return true;
                }
            }

            _logger?.LogTrace("Ignoring address: {address}", address.ToString());
            return false;
        }

        internal bool IgnoreInterface(string interfaceName)
        {
            if (string.IsNullOrEmpty(interfaceName))
            {
                return false;
            }

            foreach (string regex in _options.GetIgnoredInterfaces())
            {
                var matcher = new Regex(regex);
                if (matcher.IsMatch(interfaceName))
                {
                    _logger?.LogTrace("Ignoring interface: {name}", interfaceName);
                    return true;
                }
            }

            return false;
        }

        internal HostInfo ConvertAddress(IPAddress address)
        {
            HostInfo hostInfo = new HostInfo();
            if (!_options.SkipReverseDnsLookup)
            {
                string hostname;
                try
                {
                    // warning: this might take a few seconds...
                    var hostEntry = Dns.GetHostEntry(address);
                    hostname = hostEntry.HostName;
                }
                catch (Exception e)
                {
                    _logger?.LogInformation(e, "Cannot determine local hostname");
                    hostname = "localhost";
                }

                hostInfo.Hostname = hostname;
            }

            hostInfo.IpAddress = address.ToString();
            return hostInfo;
        }

        internal IPAddress ResolveHostAddress(string hostName)
        {
            IPAddress result = null;
            try
            {
                var results = Dns.GetHostAddresses(hostName);
                if (results != null && results.Length > 0)
                {
                    foreach (var addr in results)
                    {
                        if (addr.AddressFamily.Equals(AddressFamily.InterNetwork))
                        {
                            result = addr;
                            break;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                _logger?.LogWarning(e, "Unable to resolve host address");
            }

            return result;
        }

        internal string ResolveHostName()
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
            catch (Exception e)
            {
                _logger?.LogWarning(e, "Unable to resolve hostname");
            }

            return result;
        }

        internal string GetHostName()
        {
            return ResolveHostName();
        }

        internal IPAddress GetHostAddress()
        {
            string hostName = GetHostName();
            if (!string.IsNullOrEmpty(hostName))
            {
                return ResolveHostAddress(hostName);
            }

            return null;
        }

        internal bool IsSiteLocalAddress(IPAddress address)
        {
            string addr = address.ToString();
            return addr.StartsWith("10.") ||
                addr.StartsWith("172.16.") ||
                addr.StartsWith("192.168.");
        }
    }
}
