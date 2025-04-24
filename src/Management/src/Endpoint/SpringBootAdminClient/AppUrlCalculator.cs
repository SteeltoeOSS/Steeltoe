// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Steeltoe.Common.Net;
using Steeltoe.Management.Endpoint.Configuration;

namespace Steeltoe.Management.Endpoint.SpringBootAdminClient;

/// <summary>
/// Calculates the URL for this app to register under in Spring Boot Admin server.
/// </summary>
internal sealed class AppUrlCalculator
{
    private readonly IServer _server;
    private readonly InetUtils _inetUtils;
    private readonly IDomainNameResolver _domainNameResolver;
    private readonly IOptionsMonitor<ManagementOptions> _managementOptionsMonitor;

    public AppUrlCalculator(IServer server, IDomainNameResolver domainNameResolver, InetUtils inetUtils,
        IOptionsMonitor<ManagementOptions> managementOptionsMonitor)
    {
        ArgumentNullException.ThrowIfNull(server);
        ArgumentNullException.ThrowIfNull(inetUtils);
        ArgumentNullException.ThrowIfNull(domainNameResolver);
        ArgumentNullException.ThrowIfNull(managementOptionsMonitor);

        _server = server;
        _inetUtils = inetUtils;
        _domainNameResolver = domainNameResolver;
        _managementOptionsMonitor = managementOptionsMonitor;
    }

    public string? AutoDetectAppUrl(SpringBootAdminClientOptions clientOptions)
    {
        ArgumentNullException.ThrowIfNull(clientOptions);

        ManagementOptions managementOptions = _managementOptionsMonitor.CurrentValue;
        Uri? uri;

        if (clientOptions is { BaseScheme: not null, BasePort: not null })
        {
            // Use case: Reverse proxy, API gateway or SSL offloading. Don't bother looking for host in address bindings.
            string? host = clientOptions.BaseHost ?? ResolveHostNameOrIPAddress(clientOptions);
            uri = host == null ? null : TryCreateUri(clientOptions.BaseScheme, host, clientOptions.BasePort.Value, clientOptions.BasePath);
        }
        else if (IsPortValid(managementOptions.Port))
        {
            // Use case: Alternate management port.
            uri = GetFromManagementPort(clientOptions, managementOptions);
        }
        else
        {
            // Use case: Pick the best match from ASP.NET Core bindings.
            uri = GetBestMatchFromBindingAddresses(clientOptions);
        }

        return uri == null || !uri.IsWellFormedOriginalString() ? null : uri.AbsoluteUri;
    }

    private Uri? GetFromManagementPort(SpringBootAdminClientOptions clientOptions, ManagementOptions managementOptions)
    {
        // Alternate management port always binds to wildcard address, so it's safe to use any host/IP.
        string? host = clientOptions.BaseHost ?? ResolveHostNameOrIPAddress(clientOptions);

        if (host != null)
        {
            int port = clientOptions.BasePort ?? managementOptions.Port;
            string scheme = clientOptions.BaseScheme ?? (managementOptions.SslEnabled ? Uri.UriSchemeHttps : Uri.UriSchemeHttp);
            return TryCreateUri(scheme, host, port, clientOptions.BasePath);
        }

        return null;
    }

    private Uri? GetBestMatchFromBindingAddresses(SpringBootAdminClientOptions clientOptions)
    {
        var serverAddressesFeature = _server.Features.Get<IServerAddressesFeature>();

        if (serverAddressesFeature != null)
        {
            BindingAddress[] addresses = serverAddressesFeature.Addresses.Select(BindingAddress.Parse).ToArray();
            BindingAddress? selectedAddress = FindCompatibleAddress(addresses, clientOptions.BaseScheme, clientOptions.PreferIPAddress);

            if (selectedAddress != null)
            {
                string? host = GetHostForSelectedAddress(selectedAddress, clientOptions);

                if (host != null)
                {
                    int port = clientOptions.BasePort ?? selectedAddress.Port;
                    string path = clientOptions.BasePath ?? selectedAddress.PathBase;
                    return TryCreateUri(selectedAddress.Scheme, host, port, path);
                }
            }
        }

        return null;
    }

    private static BindingAddress? FindCompatibleAddress(BindingAddress[] addresses, string? schemeFilter, bool preferIPAddress)
    {
        BindingAddress? bestMatch = null;

        foreach (BindingAddress address in addresses.Where(address => IsPortValid(address.Port)))
        {
            if (IsBetterMatch(address, schemeFilter, preferIPAddress, bestMatch))
            {
                bestMatch = address;
            }
        }

        return bestMatch;
    }

    private static bool IsBetterMatch(BindingAddress address, string? schemeFilter, bool preferIPAddress, BindingAddress? lastBestMatch)
    {
        if (schemeFilter != null && !string.Equals(address.Scheme, schemeFilter, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (lastBestMatch == null)
        {
            return true;
        }

        if (schemeFilter == null && string.Equals(address.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(lastBestMatch.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (preferIPAddress && IsIPAddress(address) && !IsIPAddress(lastBestMatch))
        {
            return true;
        }

        if (!IsWildcardAddress(address) && IsWildcardAddress(lastBestMatch))
        {
            return true;
        }

        return false;
    }

    private string? GetHostForSelectedAddress(BindingAddress address, SpringBootAdminClientOptions clientOptions)
    {
        string? host = clientOptions.BaseHost;

        if (host == null && !IsWildcardAddress(address))
        {
            host = address.Host;
        }

        host ??= ResolveHostNameOrIPAddress(clientOptions);

        return host;
    }

    private static bool IsWildcardAddress(BindingAddress address)
    {
        // From https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel/endpoints?view=aspnetcore-8.0#url-formats:
        // > Anything not recognized as a valid IP address or localhost is treated as a wildcard that binds to all IPv4 and IPv6 addresses.
        // > Some people like to use * or + to be more explicit.
        return !string.Equals(address.Host, "localhost", StringComparison.OrdinalIgnoreCase) && !IsIPAddress(address);
    }

    private static bool IsIPAddress(BindingAddress address)
    {
        return IPAddress.TryParse(address.Host, out _);
    }

    private static bool IsPortValid(int port)
    {
        return port is > 0 and < 65536;
    }

    private string? ResolveHostNameOrIPAddress(SpringBootAdminClientOptions clientOptions)
    {
        HostInfo? hostInfo = clientOptions.UseNetworkInterfaces ? _inetUtils.FindFirstNonLoopbackHostInfo() : null;
        string? hostName = hostInfo != null ? hostInfo.Hostname : _domainNameResolver.ResolveHostName();

        if (clientOptions.PreferIPAddress)
        {
            if (hostInfo != null)
            {
                return hostInfo.IPAddress;
            }

            if (hostName != null)
            {
                return _domainNameResolver.ResolveHostAddress(hostName);
            }

            return null;
        }

#pragma warning disable S4040 // Strings should be normalized to uppercase
        return hostName?.ToLowerInvariant();
#pragma warning restore S4040 // Strings should be normalized to uppercase
    }

    private static Uri? TryCreateUri(string scheme, string host, int port, string? path)
    {
        var builder = new UriBuilder(scheme, host);

        if (IsPortValid(port))
        {
            builder.Port = port;
        }

        if (path != null)
        {
            builder.Path = path;
        }

        try
        {
            return builder.Uri;
        }
        catch (UriFormatException)
        {
            return null;
        }
    }
}
