// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using System;
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

            var hostInfo = new HostInfo
            {
                Hostname = _options.DefaultHostname,
                IpAddress = _options.DefaultIpAddress
            };
            return hostInfo;
        }

        public IPAddress FindFirstNonLoopbackAddress()
        {
            IPAddress result = null;
            try
            {
                var lowest = int.MaxValue;
                var ifaces = NetworkInterface.GetAllNetworkInterfaces();
                for (var i = 0; i < ifaces.Length; i++)
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
                var siteLocalAddress = IsSiteLocalAddress(address);
                if (!siteLocalAddress)
                {
                    _logger?.LogTrace("Ignoring address: {address} [UseOnlySiteLocalInterfaces=true, this address is not]", address.ToString());
                }

                return siteLocalAddress;
            }

            var preferredNetworks = _options.GetPreferredNetworks();
            if (!preferredNetworks.Any())
            {
                return true;
            }

            foreach (var regex in preferredNetworks)
            {
                var hostAddress = address.ToString();
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

            foreach (var regex in _options.GetIgnoredInterfaces())
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
            var hostInfo = new HostInfo();
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
            else
            {
                hostInfo.Hostname = _options.DefaultHostname;
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
            var hostName = GetHostName();
            if (!string.IsNullOrEmpty(hostName))
            {
                return ResolveHostAddress(hostName);
            }

            return null;
        }

        internal bool IsSiteLocalAddress(IPAddress address)
        {
            var addr = address.ToString();
            return addr.StartsWith("10.") ||
                addr.StartsWith("172.16.") ||
                addr.StartsWith("192.168.");
        }
    }
}
