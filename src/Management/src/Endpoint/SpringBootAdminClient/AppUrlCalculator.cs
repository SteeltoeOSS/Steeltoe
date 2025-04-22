// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Steeltoe.Common.Net;
using Steeltoe.Management.Endpoint.Configuration;

namespace Steeltoe.Management.Endpoint.SpringBootAdminClient;

/// <summary>
/// Calculates the URL for this app to register with in Spring Boot Admin server.
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

        string? host = GetHostnameOrIPAddress(clientOptions);
        string? url = null;

        if (host != null)
        {
            url = GetFromOptions(clientOptions, _managementOptionsMonitor.CurrentValue, host);

            if (url == null)
            {
                BindingAddress? bindingAddress = SelectBindingAddress(clientOptions);

                if (bindingAddress != null)
                {
                    int port = clientOptions.BasePort ?? bindingAddress.Port;
                    string path = clientOptions.BasePathRooted ?? bindingAddress.PathBase;

                    url = port > 0
                        ? $"{bindingAddress.Scheme}{Uri.SchemeDelimiter}{host}:{port}{path}"
                        : $"{bindingAddress.Scheme}{Uri.SchemeDelimiter}{host}{path}";
                }
            }
        }

        return url;
    }

    private string? GetHostnameOrIPAddress(SpringBootAdminClientOptions clientOptions)
    {
        if (clientOptions.BaseHost != null)
        {
            return clientOptions.BaseHost;
        }

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

    private static string? GetFromOptions(SpringBootAdminClientOptions clientOptions, ManagementOptions managementOptions, string host)
    {
        if (clientOptions is { BaseScheme: not null, BasePort: not null })
        {
            return $"{clientOptions.BaseScheme}{Uri.SchemeDelimiter}{host}:{clientOptions.BasePort}{clientOptions.BasePathRooted}";
        }

        if (IsPortValid(managementOptions.Port))
        {
            int port = clientOptions.BasePort ?? managementOptions.Port;
            string scheme = clientOptions.BaseScheme ?? (managementOptions.SslEnabled ? Uri.UriSchemeHttps : Uri.UriSchemeHttp);
            return $"{scheme}{Uri.SchemeDelimiter}{host}:{port}{clientOptions.BasePathRooted}";
        }

        return null;
    }

    private BindingAddress? SelectBindingAddress(SpringBootAdminClientOptions clientOptions)
    {
        var serverAddressesFeature = _server.Features.Get<IServerAddressesFeature>();

        if (serverAddressesFeature != null)
        {
            BindingAddress[] addresses = serverAddressesFeature.Addresses.Select(BindingAddress.Parse).ToArray();

            return clientOptions.BaseScheme != null
                ? FindAddressWithScheme(addresses, clientOptions.BaseScheme)
                : FindAddressWithScheme(addresses, Uri.UriSchemeHttps) ?? FindAddressWithScheme(addresses, Uri.UriSchemeHttp);
        }

        return null;
    }

    private static BindingAddress? FindAddressWithScheme(BindingAddress[] addresses, string scheme)
    {
        return Array.Find(addresses, address => string.Equals(address.Scheme, scheme, StringComparison.OrdinalIgnoreCase) && IsPortValid(address.Port));
    }

    private static bool IsPortValid(int port)
    {
        return port is > 0 and < 65536;
    }
}
