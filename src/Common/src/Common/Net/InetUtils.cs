// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace Steeltoe.Common.Net;

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
        IPAddress address = FindFirstNonLoopbackAddress();

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
            int lowest = int.MaxValue;
            NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();

            foreach (NetworkInterface @interface in interfaces)
            {
                if (@interface.OperationalStatus == OperationalStatus.Up && !@interface.IsReceiveOnly)
                {
                    _logger?.LogTrace("Testing interface: {name}, {id}", @interface.Name, @interface.Id);

                    IPInterfaceProperties props = @interface.GetIPProperties();
                    IPv4InterfaceProperties ipProps = props.GetIPv4Properties();

                    if (ipProps.Index < lowest || result == null)
                    {
                        lowest = ipProps.Index;
                    }
                    else if (result != null)
                    {
                        continue;
                    }

                    if (!IgnoreInterface(@interface.Name))
                    {
                        foreach (UnicastIPAddressInformation addressInfo in props.UnicastAddresses)
                        {
                            IPAddress address = addressInfo.Address;

                            if (IsInet4Address(address) && !IsLoopbackAddress(address) && IsPreferredAddress(address))
                            {
                                _logger?.LogTrace("Found non-loopback interface: {name}", @interface.Name);
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
                _logger?.LogTrace("Ignoring address: {address} [UseOnlySiteLocalInterfaces=true, this address is not]", address);
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

        _logger?.LogTrace("Ignoring address: {address}", address);
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
        var hostInfo = new HostInfo();

        if (!_options.SkipReverseDnsLookup)
        {
            string hostname;

            try
            {
                // warning: this might take a few seconds...
                IPHostEntry hostEntry = Dns.GetHostEntry(address);
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
            IPAddress[] results = Dns.GetHostAddresses(hostName);

            if (results != null && results.Length > 0)
            {
                foreach (IPAddress address in results)
                {
                    if (address.AddressFamily.Equals(AddressFamily.InterNetwork))
                    {
                        result = address;
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
                IPHostEntry response = Dns.GetHostEntry(result);

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
        string text = address.ToString();
        return text.StartsWith("10.") || text.StartsWith("172.16.") || text.StartsWith("192.168.");
    }
}
