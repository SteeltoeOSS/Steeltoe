// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using Consul;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Common.Discovery;
using Steeltoe.Discovery.Consul.Configuration;
using Steeltoe.Discovery.Consul.Util;

namespace Steeltoe.Discovery.Consul.Registry;

/// <summary>
/// Represents the local application instance, to be registered with the Consul server.
/// </summary>
public sealed class ConsulRegistration : IServiceInstance
{
    private const char Separator = '-';
    private readonly IOptionsMonitor<ConsulDiscoveryOptions> _optionsMonitor;

    internal AgentServiceRegistration InnerRegistration { get; }

    /// <inheritdoc />
    public string ServiceId { get; }

    /// <summary>
    /// Gets the instance ID as registered by the Consul server.
    /// </summary>
    public string InstanceId { get; }

    /// <inheritdoc />
    public string Host { get; }

    /// <inheritdoc />
    public int Port { get; }

    /// <inheritdoc />
    public bool IsSecure => _optionsMonitor.CurrentValue.Scheme == "https";

    /// <inheritdoc />
    public Uri Uri => new($"{_optionsMonitor.CurrentValue.Scheme}://{Host}:{Port}");

    public IReadOnlyList<string> Tags { get; }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, string?> Metadata { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsulRegistration" /> class. Wraps an existing registration.
    /// </summary>
    /// <param name="innerRegistration">
    /// The Consul service registration to wrap.
    /// </param>
    /// <param name="optionsMonitor">
    /// Provides access to <see cref="ConsulDiscoveryOptions" />.
    /// </param>
    internal ConsulRegistration(AgentServiceRegistration innerRegistration, IOptionsMonitor<ConsulDiscoveryOptions> optionsMonitor)
    {
        ArgumentGuard.NotNull(innerRegistration);
        ArgumentGuard.NotNull(optionsMonitor);

        InnerRegistration = innerRegistration;
        _optionsMonitor = optionsMonitor;

        ServiceId = innerRegistration.Name;
        InstanceId = innerRegistration.ID;
        Host = innerRegistration.Address;
        Port = innerRegistration.Port;
        Tags = innerRegistration.Tags;
        Metadata = new ReadOnlyDictionary<string, string?>(innerRegistration.Meta);
    }

    /// <summary>
    /// Creates a registration for the currently running app, to be submitted to the Consul server.
    /// </summary>
    /// <param name="optionsMonitor">
    /// Provides access to <see cref="ConsulDiscoveryOptions" />.
    /// </param>
    /// <param name="applicationInfo">
    /// Info about this app instance.
    /// </param>
    internal static ConsulRegistration Create(IOptionsMonitor<ConsulDiscoveryOptions> optionsMonitor, IApplicationInstanceInfo applicationInfo)
    {
        ArgumentGuard.NotNull(optionsMonitor);
        ArgumentGuard.NotNull(applicationInfo);

        ConsulDiscoveryOptions options = optionsMonitor.CurrentValue;

        var agentServiceRegistration = new AgentServiceRegistration
        {
            ID = GetInstanceId(options, applicationInfo)
        };

        if (!options.PreferAgentAddress)
        {
            agentServiceRegistration.Address = options.HostName;
        }

        string appName = applicationInfo.GetApplicationNameInContext(SteeltoeComponent.Discovery, $"{ConsulDiscoveryOptions.ConfigurationPrefix}:serviceName");

        agentServiceRegistration.Name = NormalizeForConsul(appName);
        agentServiceRegistration.Tags = CreateTags(options);
        agentServiceRegistration.Meta = CreateMetadata(options);

        if (options.Port != 0)
        {
            agentServiceRegistration.Port = options.Port;
            SetCheck(agentServiceRegistration, options);
        }

        return new ConsulRegistration(agentServiceRegistration, optionsMonitor);
    }

    private static IDictionary<string, string> CreateMetadata(ConsulDiscoveryOptions options)
    {
        Dictionary<string, string> metadata = options.Metadata.ToDictionary(pair => pair.Key, pair => pair.Value);

        if (!string.IsNullOrEmpty(options.InstanceZone) && !string.IsNullOrEmpty(options.DefaultZoneMetadataName))
        {
            metadata.Add(options.DefaultZoneMetadataName, options.InstanceZone);
        }

        if (!string.IsNullOrEmpty(options.InstanceGroup))
        {
            metadata.Add("group", options.InstanceGroup);
        }

        // store the secure flag in the metadata so that clients will be able to figure out whether to use http or https automatically
        metadata.Add("secure", options.Scheme == "https" ? "true" : "false");

        return metadata;
    }

    private static string[] CreateTags(ConsulDiscoveryOptions options)
    {
        return options.Tags.ToArray();
    }

    private static string GetInstanceId(ConsulDiscoveryOptions options, IApplicationInstanceInfo applicationInfo)
    {
        if (string.IsNullOrEmpty(options.InstanceId))
        {
            return NormalizeForConsul(GetDefaultInstanceId(applicationInfo));
        }

        return NormalizeForConsul(options.InstanceId);
    }

    private static string GetDefaultInstanceId(IApplicationInstanceInfo applicationInfo)
    {
        string appName = applicationInfo.GetApplicationNameInContext(SteeltoeComponent.Discovery, $"{ConsulDiscoveryOptions.ConfigurationPrefix}:serviceName");

        string instanceId = applicationInfo.InstanceId;

        if (string.IsNullOrEmpty(instanceId))
        {
            instanceId = Random.Shared.Next().ToString(CultureInfo.InvariantCulture);
        }

        return $"{appName}:{instanceId}";
    }

    internal static string NormalizeForConsul(string serviceId)
    {
        if (serviceId == null || !char.IsLetter(serviceId[0]) || !char.IsLetterOrDigit(serviceId[^1]))
        {
            throw new ArgumentException(
                $"Consul service IDs must not be empty, must start with a letter, end with a letter or digit, and have as interior characters only letters, digits, and hyphen: {serviceId}",
                nameof(serviceId));
        }

        var normalized = new StringBuilder();
        char prev = default;

        foreach (char ch in serviceId)
        {
            char toAppend = default;

            if (char.IsLetterOrDigit(ch))
            {
                toAppend = ch;
            }
            else if (prev is default(char) or not Separator)
            {
                toAppend = Separator;
            }

            if (toAppend != default(char))
            {
                normalized.Append(toAppend);
                prev = toAppend;
            }
        }

        return normalized.ToString();
    }

    internal static AgentServiceCheck CreateCheck(int port, ConsulDiscoveryOptions options)
    {
        var check = new AgentServiceCheck();

        if (!string.IsNullOrEmpty(options.HealthCheckCriticalTimeout))
        {
            check.DeregisterCriticalServiceAfter = DateTimeConversions.ToTimeSpan(options.HealthCheckCriticalTimeout);
        }

        if (options is { IsHeartbeatEnabled: true, Heartbeat: not null })
        {
            check.TTL = DateTimeConversions.ToTimeSpan(options.Heartbeat.TimeToLive);
            return check;
        }

        if (port <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(port), port, "Port must be greater than zero.");
        }

        if (!string.IsNullOrEmpty(options.HealthCheckUrl))
        {
            check.HTTP = options.HealthCheckUrl;
        }
        else
        {
            var uri = new Uri($"{options.Scheme}://{options.HostName}:{port}{options.HealthCheckPath}");
            check.HTTP = uri.ToString();
        }

        if (!string.IsNullOrEmpty(options.HealthCheckInterval))
        {
            check.Interval = DateTimeConversions.ToTimeSpan(options.HealthCheckInterval);
        }

        if (!string.IsNullOrEmpty(options.HealthCheckTimeout))
        {
            check.Timeout = DateTimeConversions.ToTimeSpan(options.HealthCheckTimeout);
        }

        check.TLSSkipVerify = options.HealthCheckTlsSkipVerify;
        return check;
    }

    private static void SetCheck(AgentServiceRegistration service, ConsulDiscoveryOptions options)
    {
        if (options.RegisterHealthCheck && service.Check == null)
        {
            service.Check = CreateCheck(service.Port, options);
        }
    }
}
