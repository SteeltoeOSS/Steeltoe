// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace Steeltoe.Common.Net;

internal class InetUtils
{
    private readonly InetOptions _options;
    private readonly ILogger<InetUtils> _logger;

    public InetUtils(InetOptions options, ILogger<InetUtils> logger)
    {
        ArgumentGuard.NotNull(options);
        ArgumentGuard.NotNull(logger);

        _options = options;
        _logger = logger;
    }

    public virtual HostInfo FindFirstNonLoopbackHostInfo()
    {
        IPAddress? address = FindFirstNonLoopbackAddress();

        if (address != null)
        {
            return ConvertAddress(address);
        }

        return new HostInfo
        {
            Hostname = _options.DefaultHostname,
            IPAddress = _options.DefaultIPAddress
        };
    }

    public IPAddress? FindFirstNonLoopbackAddress()
    {
        IPAddress? result = null;

        try
        {
            int lowest = int.MaxValue;
            NetworkInterface[] networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();

            foreach (NetworkInterface networkInterface in networkInterfaces)
            {
                if (networkInterface is { OperationalStatus: OperationalStatus.Up, IsReceiveOnly: false })
                {
                    _logger.LogTrace("Testing interface: {name}, {id}", networkInterface.Name, networkInterface.Id);

                    IPInterfaceProperties properties = networkInterface.GetIPProperties();
                    IPv4InterfaceProperties iPv4Properties = properties.GetIPv4Properties();

                    if (iPv4Properties.Index < lowest || result == null)
                    {
                        lowest = iPv4Properties.Index;
                    }
                    else
                    {
                        continue;
                    }

                    if (!IgnoreInterface(networkInterface.Name))
                    {
                        foreach (UnicastIPAddressInformation addressInfo in properties.UnicastAddresses)
                        {
                            IPAddress address = addressInfo.Address;

                            if (IsInet4Address(address) && !IsLoopbackAddress(address) && IsPreferredAddress(address))
                            {
                                _logger.LogTrace("Found non-loopback interface: {name}", networkInterface.Name);
                                result = address;
                            }
                        }
                    }
                }
            }
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Cannot get first non-loopback address");
        }

        if (result != null)
        {
            return result;
        }

        return GetHostAddress();
    }

    private static bool IsInet4Address(IPAddress address)
    {
        return address.AddressFamily == AddressFamily.InterNetwork;
    }

    private static bool IsLoopbackAddress(IPAddress address)
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
                _logger.LogTrace("Ignoring address: {address} [UseOnlySiteLocalInterfaces=true, this address is not]", address);
            }

            return siteLocalAddress;
        }

        string[] preferredNetworks = _options.GetPreferredNetworks().ToArray();

        if (preferredNetworks.Length == 0)
        {
            return true;
        }

        foreach (string regex in preferredNetworks)
        {
            string hostAddress = address.ToString();
            var matcher = new Regex(regex);

            if (matcher.IsMatch(hostAddress) || hostAddress.StartsWith(regex, StringComparison.Ordinal))
            {
                return true;
            }
        }

        _logger.LogTrace("Ignoring address: {address}", address);
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
                _logger.LogTrace("Ignoring interface: {name}", interfaceName);
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
                _logger.LogInformation(e, "Cannot determine local hostname.");
                hostname = "localhost";
            }

            hostInfo.Hostname = hostname;
        }
        else
        {
            hostInfo.Hostname = _options.DefaultHostname;
        }

        hostInfo.IPAddress = address.ToString();
        return hostInfo;
    }

    private IPAddress? ResolveHostAddress(string hostName)
    {
        IPAddress? result = null;

        try
        {
            IPAddress[] results = Dns.GetHostAddresses(hostName);

            if (results.Length > 0)
            {
                foreach (IPAddress address in results)
                {
                    if (address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        result = address;
                        break;
                    }
                }
            }
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Unable to resolve host address.");
        }

        return result;
    }

    private string? ResolveHostName()
    {
        string? result = null;

        try
        {
            result = Dns.GetHostName();
            IPHostEntry response = Dns.GetHostEntry(result);
            return response.HostName;
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Unable to resolve hostname.");
        }

        return result;
    }

    private IPAddress? GetHostAddress()
    {
        string? hostName = ResolveHostName();
        return !string.IsNullOrEmpty(hostName) ? ResolveHostAddress(hostName) : null;
    }

    private static bool IsSiteLocalAddress(IPAddress address)
    {
        string text = address.ToString();

        return text.StartsWith("10.", StringComparison.Ordinal) || text.StartsWith("172.16.", StringComparison.Ordinal) ||
            text.StartsWith("192.168.", StringComparison.Ordinal);
    }
}
