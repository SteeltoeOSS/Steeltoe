// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Steeltoe.Common.Http;
using Steeltoe.Common.Net;
using Steeltoe.Discovery.Consul.Configuration;

namespace Steeltoe.Discovery.Consul;

internal sealed class PostConfigureConsulDiscoveryOptions : IPostConfigureOptions<ConsulDiscoveryOptions>
{
    private readonly IConfiguration _configuration;
    private readonly InetUtils _inetUtils;

    public PostConfigureConsulDiscoveryOptions(IConfiguration configuration, InetUtils inetUtils)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(inetUtils);

        _configuration = configuration;
        _inetUtils = inetUtils;
    }

    public void PostConfigure(string? name, ConsulDiscoveryOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        HostInfo? hostInfo = options.UseNetworkInterfaces ? _inetUtils.FindFirstNonLoopbackHostInfo() : null;
        options.HostName ??= hostInfo != null ? hostInfo.Hostname : DnsTools.ResolveHostName();

        if (string.IsNullOrWhiteSpace(options.IPAddress))
        {
            if (hostInfo != null && !string.IsNullOrWhiteSpace(hostInfo.IPAddress))
            {
                options.IPAddress = hostInfo.IPAddress;
            }
            else if (!string.IsNullOrEmpty(options.HostName))
            {
                options.IPAddress = DnsTools.ResolveHostAddress(options.HostName);
            }
        }

        if (options.PreferIPAddress && !string.IsNullOrEmpty(options.IPAddress))
        {
            options.HostName = options.IPAddress;
        }

        if (options.Port == 0)
        {
            ICollection<string> addresses = _configuration.GetListenAddresses();
            SetPortsFromListenAddresses(options, addresses);
        }
    }

    private void SetPortsFromListenAddresses(ConsulDiscoveryOptions options, IEnumerable<string> listenOnAddresses)
    {
        // Try to pull some values out of server configuration to override defaults, but only if not using NetUtils.
        // If NetUtils are configured, the user probably wants to define their own behavior.
        if (options is { UseAspNetCoreUrls: true, Port: 0 })
        {
            int? listenHttpPort = null;
            int? listenHttpsPort = null;

            foreach (string address in listenOnAddresses)
            {
                BindingAddress bindingAddress = BindingAddress.Parse(address);

                if (bindingAddress is { Scheme: "http", Port: > 0 } && listenHttpPort == null)
                {
                    listenHttpPort = bindingAddress.Port;
                }
                else if (bindingAddress is { Scheme: "https", Port: > 0 } && listenHttpsPort == null)
                {
                    listenHttpsPort = bindingAddress.Port;
                }
            }

            int? listenPort = listenHttpsPort ?? listenHttpPort;

            if (listenPort != null)
            {
                options.Port = listenPort.Value;
            }
        }
    }
}
