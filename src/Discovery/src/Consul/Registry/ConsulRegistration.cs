// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Text;
using Consul;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Discovery.Consul.Discovery;
using Steeltoe.Discovery.Consul.Util;

namespace Steeltoe.Discovery.Consul.Registry;

/// <summary>
/// The registration to be used when registering with the Consul server.
/// </summary>
public class ConsulRegistration : IConsulRegistration
{
    private const char Separator = '-';
    private readonly IOptionsMonitor<ConsulDiscoveryOptions> _optionsMonitor;
    private readonly ConsulDiscoveryOptions _options;

    internal ConsulDiscoveryOptions Options
    {
        get
        {
            if (_optionsMonitor != null)
            {
                return _optionsMonitor.CurrentValue;
            }

            return _options;
        }
    }

    /// <inheritdoc />
    public AgentServiceRegistration Service { get; }

    /// <inheritdoc />
    public string InstanceId { get; private set; }

    /// <inheritdoc />
    public string ServiceId { get; private set; }

    /// <inheritdoc />
    public string Host { get; private set; }

    /// <inheritdoc />
    public int Port { get; private set; }

    /// <inheritdoc />
    public bool IsSecure => Options.Scheme == "https";

    /// <inheritdoc />
    public Uri Uri => new($"{Options.Scheme}://{Host}:{Port}");

    /// <inheritdoc />
    public IDictionary<string, string> Metadata { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsulRegistration" /> class.
    /// </summary>
    /// <param name="agentServiceRegistration">
    /// a Consul service registration to use.
    /// </param>
    /// <param name="options">
    /// configuration options.
    /// </param>
    public ConsulRegistration(AgentServiceRegistration agentServiceRegistration, ConsulDiscoveryOptions options)
    {
        ArgumentGuard.NotNull(agentServiceRegistration);
        ArgumentGuard.NotNull(options);

        Service = agentServiceRegistration;
        _options = options;

        Initialize(agentServiceRegistration);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsulRegistration" /> class.
    /// </summary>
    /// <param name="agentServiceRegistration">
    /// a Consul service registration to use.
    /// </param>
    /// <param name="optionsMonitor">
    /// configuration options.
    /// </param>
    public ConsulRegistration(AgentServiceRegistration agentServiceRegistration, IOptionsMonitor<ConsulDiscoveryOptions> optionsMonitor)
    {
        ArgumentGuard.NotNull(agentServiceRegistration);
        ArgumentGuard.NotNull(optionsMonitor);

        Service = agentServiceRegistration;
        _optionsMonitor = optionsMonitor;

        Initialize(agentServiceRegistration);
    }

    // For testing
    internal ConsulRegistration()
    {
    }

    internal void Initialize(AgentServiceRegistration agentServiceRegistration)
    {
        InstanceId = agentServiceRegistration.ID;
        ServiceId = agentServiceRegistration.Name;
        Host = agentServiceRegistration.Address;
        Port = agentServiceRegistration.Port;
        Metadata = ConsulServerUtils.GetMetadata(agentServiceRegistration.Tags);
    }

    /// <summary>
    /// Create a Consul registration.
    /// </summary>
    /// <param name="options">
    /// configuration options to use.
    /// </param>
    /// <param name="applicationInfo">
    /// Info about this app instance.
    /// </param>
    /// <returns>
    /// a registration.
    /// </returns>
    public static ConsulRegistration CreateRegistration(ConsulDiscoveryOptions options, IApplicationInstanceInfo applicationInfo)
    {
        ArgumentGuard.NotNull(options);

        var service = new AgentServiceRegistration();
        service.ID = GetInstanceId(options, applicationInfo);

        if (!options.PreferAgentAddress)
        {
            service.Address = options.HostName;
        }

        string appName = applicationInfo.GetApplicationNameInContext(SteeltoeComponent.Discovery,
            $"{ConsulDiscoveryOptions.ConsulDiscoveryConfigurationPrefix}:serviceName");

        service.Name = NormalizeForConsul(appName);
        service.Tags = CreateTags(options);

        if (options.Port != 0)
        {
            service.Port = options.Port;
            SetCheck(service, options);
        }

        return new ConsulRegistration(service, options);
    }

    internal static string[] CreateTags(ConsulDiscoveryOptions options)
    {
        var tags = new List<string>();

        if (options.Tags != null)
        {
            tags.AddRange(options.Tags);
        }

        if (!string.IsNullOrEmpty(options.InstanceZone))
        {
            tags.Add($"{options.DefaultZoneMetadataName}={options.InstanceZone}");
        }

        if (!string.IsNullOrEmpty(options.InstanceGroup))
        {
            tags.Add($"group={options.InstanceGroup}");
        }

        // store the secure flag in the tags so that clients will be able to figure out whether to use http or https automatically
#pragma warning disable S4040 // Strings should be normalized to uppercase
        tags.Add($"secure={(options.Scheme == "https").ToString(CultureInfo.InvariantCulture).ToLowerInvariant()}");
#pragma warning restore S4040 // Strings should be normalized to uppercase

        return tags.ToArray();
    }

    internal static string GetInstanceId(ConsulDiscoveryOptions options, IApplicationInstanceInfo applicationInfo)
    {
        if (string.IsNullOrEmpty(options.InstanceId))
        {
            return NormalizeForConsul(GetDefaultInstanceId(applicationInfo));
        }

        return NormalizeForConsul(options.InstanceId);
    }

    internal static string GetDefaultInstanceId(IApplicationInstanceInfo applicationInfo)
    {
        string appName = applicationInfo.GetApplicationNameInContext(SteeltoeComponent.Discovery,
            $"{ConsulDiscoveryOptions.ConsulDiscoveryConfigurationPrefix}:serviceName");

        string instanceId = applicationInfo.InstanceId;

        if (string.IsNullOrEmpty(instanceId))
        {
            instanceId = Random.Shared.Next().ToString(CultureInfo.InvariantCulture);
        }

        return $"{appName}:{instanceId}";
    }

    internal static string NormalizeForConsul(string serviceId)
    {
        if (serviceId == null || !char.IsLetter(serviceId[0]) || !char.IsLetterOrDigit(serviceId[serviceId.Length - 1]))
        {
            throw new ArgumentException(
                $"Consul service ids must not be empty, must start with a letter, end with a letter or digit, and have as interior characters only letters, digits, and hyphen: {serviceId}",
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
            else if (prev == default(char) || prev != Separator)
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

        if (options.IsHeartBeatEnabled)
        {
            check.TTL = DateTimeConversions.ToTimeSpan(options.Heartbeat.Ttl);
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

        // check.setHeader(properties.getHealthCheckHeaders());
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

    internal static void SetCheck(AgentServiceRegistration service, ConsulDiscoveryOptions options)
    {
        if (options.RegisterHealthCheck && service.Check == null)
        {
            service.Check = CreateCheck(service.Port, options);
        }
    }
}
