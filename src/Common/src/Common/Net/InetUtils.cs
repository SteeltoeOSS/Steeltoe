// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Steeltoe.Common.Net;

// Non-sealed because this type is mocked by tests.
internal class InetUtils
{
    private readonly IOptionsMonitor<InetOptions> _optionsMonitor;
    private readonly ILogger<InetUtils> _logger;

    public InetUtils(IOptionsMonitor<InetOptions> optionsMonitor, ILogger<InetUtils> logger)
    {
        ArgumentGuard.NotNull(optionsMonitor);
        ArgumentGuard.NotNull(logger);

        _optionsMonitor = optionsMonitor;
        _logger = logger;
    }

    public virtual HostInfo FindFirstNonLoopbackHostInfo()
    {
        InetOptions inetOptions = _optionsMonitor.CurrentValue;

        IPAddress? address = FindFirstNonLoopbackAddress(inetOptions);

        if (address != null)
        {
            return ConvertAddress(address, inetOptions);
        }

        return new HostInfo(inetOptions.DefaultHostname!, inetOptions.DefaultIPAddress!);
    }

    public IPAddress? FindFirstNonLoopbackAddress()
    {
        return FindFirstNonLoopbackAddress(_optionsMonitor.CurrentValue);
    }

    private IPAddress? FindFirstNonLoopbackAddress(InetOptions inetOptions)
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
                    _logger.LogTrace("Testing interface: {Name}, {Id}", networkInterface.Name, networkInterface.Id);

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

                    if (!IgnoreInterface(networkInterface.Name, inetOptions))
                    {
                        foreach (UnicastIPAddressInformation addressInfo in properties.UnicastAddresses)
                        {
                            IPAddress address = addressInfo.Address;

                            if (IsInet4Address(address) && !IsLoopbackAddress(address) && IsPreferredAddress(address, inetOptions))
                            {
                                _logger.LogTrace("Found non-loopback interface: {Name}", networkInterface.Name);
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

    internal bool IsPreferredAddress(IPAddress address, InetOptions inetOptions)
    {
        if (inetOptions.UseOnlySiteLocalInterfaces)
        {
            bool siteLocalAddress = IsSiteLocalAddress(address);

            if (!siteLocalAddress)
            {
                _logger.LogTrace("Ignoring address: {Address} [UseOnlySiteLocalInterfaces=true, this address is not]", address);
            }

            return siteLocalAddress;
        }

        string[] preferredNetworks = inetOptions.GetPreferredNetworks().ToArray();

        if (preferredNetworks.Length == 0)
        {
            return true;
        }

        foreach (string regex in preferredNetworks)
        {
            string hostAddress = address.ToString();
            var matcher = new Regex(regex, RegexOptions.None, TimeSpan.FromSeconds(1));

            if (matcher.IsMatch(hostAddress) || hostAddress.StartsWith(regex, StringComparison.Ordinal))
            {
                return true;
            }
        }

        _logger.LogTrace("Ignoring address: {Address}", address);
        return false;
    }

    internal bool IgnoreInterface(string interfaceName, InetOptions inetOptions)
    {
        if (string.IsNullOrEmpty(interfaceName))
        {
            return false;
        }

        foreach (string regex in inetOptions.GetIgnoredInterfaces())
        {
            var matcher = new Regex(regex, RegexOptions.None, TimeSpan.FromSeconds(1));

            if (matcher.IsMatch(interfaceName))
            {
                _logger.LogTrace("Ignoring interface: {Name}", interfaceName);
                return true;
            }
        }

        return false;
    }

    internal HostInfo ConvertAddress(IPAddress address, InetOptions inetOptions)
    {
        string hostname;

        if (!inetOptions.SkipReverseDnsLookup)
        {
            try
            {
                // warning: this might take a few seconds...
                IPHostEntry hostEntry = Dns.GetHostEntry(address);
                hostname = hostEntry.HostName;
            }
            catch (Exception exception)
            {
                _logger.LogInformation(exception, "Cannot determine local hostname.");
                hostname = "localhost";
            }
        }
        else
        {
            hostname = inetOptions.DefaultHostname!;
        }

        return new HostInfo(hostname, address.ToString());
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
        try
        {
            return DnsTools.ResolveHostName(true);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Unable to resolve hostname.");
            return null;
        }
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
