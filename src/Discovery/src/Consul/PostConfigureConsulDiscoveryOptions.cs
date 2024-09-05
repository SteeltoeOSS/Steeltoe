// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Common.Http;
using Steeltoe.Common.Net;
using Steeltoe.Discovery.Consul.Configuration;

namespace Steeltoe.Discovery.Consul;

internal sealed class PostConfigureConsulDiscoveryOptions : IPostConfigureOptions<ConsulDiscoveryOptions>
{
    private const char SafeChar = '-';

    private readonly IConfiguration _configuration;
    private readonly InetUtils _inetUtils;
    private readonly IApplicationInstanceInfo _applicationInstanceInfo;

    public PostConfigureConsulDiscoveryOptions(IConfiguration configuration, InetUtils inetUtils, IApplicationInstanceInfo applicationInstanceInfo)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(inetUtils);
        ArgumentNullException.ThrowIfNull(applicationInstanceInfo);

        _configuration = configuration;
        _inetUtils = inetUtils;
        _applicationInstanceInfo = applicationInstanceInfo;
    }

    public void PostConfigure(string? name, ConsulDiscoveryOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        options.ServiceName = GetServiceName(options);

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

        options.InstanceId = GetInstanceId(options);
    }

    private string GetServiceName(ConsulDiscoveryOptions options)
    {
        string? serviceName = options.ServiceName ?? _applicationInstanceInfo.ApplicationName;
        return NormalizeForConsul(serviceName, nameof(ConsulDiscoveryOptions.ServiceName));
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

    private string GetInstanceId(ConsulDiscoveryOptions options)
    {
        string? instanceId = options.InstanceId;

        if (string.IsNullOrEmpty(instanceId))
        {
            string defaultInstanceId = $"{Random.Shared.Next(10_000_000, 99_999_999):D8}";
            instanceId = $"{options.ServiceName}:{defaultInstanceId}";
        }

        return NormalizeForConsul(instanceId, nameof(ConsulDiscoveryOptions.InstanceId));
    }

    internal static string NormalizeForConsul(string? value, string propertyName)
    {
        if (value == null || !char.IsLetter(value[0]) || !char.IsLetterOrDigit(value[^1]))
        {
            throw new InvalidOperationException(
                $"Consul property '{propertyName}' must not be empty, must start with a letter, end with a letter or digit, and have as interior characters only letters, digits, and hyphen. The value '{value}' is invalid.");
        }

        var normalizedValueBuilder = new StringBuilder();
        char? previousChar = null;

        foreach (char ch in value)
        {
            char? charToAppend = null;

            if (char.IsLetterOrDigit(ch))
            {
                charToAppend = ch;
            }
            else if (previousChar is not SafeChar)
            {
                charToAppend = SafeChar;
            }

            if (charToAppend != null)
            {
                normalizedValueBuilder.Append(charToAppend);
                previousChar = charToAppend;
            }
        }

        return normalizedValueBuilder.ToString();
    }
}
