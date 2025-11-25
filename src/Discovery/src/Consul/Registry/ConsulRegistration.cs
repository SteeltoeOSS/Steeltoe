// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Consul;
using Microsoft.Extensions.Options;
using Steeltoe.Common.Discovery;
using Steeltoe.Discovery.Consul.Configuration;
using Steeltoe.Discovery.Consul.Util;

namespace Steeltoe.Discovery.Consul.Registry;

/// <summary>
/// Represents the local application instance, to be registered with the Consul server.
/// </summary>
internal sealed class ConsulRegistration : IServiceInstance
{
    private readonly IOptionsMonitor<ConsulDiscoveryOptions> _optionsMonitor;

    internal AgentServiceRegistration InnerRegistration { get; }

    /// <inheritdoc />
    public string ServiceId { get; }

    /// <inheritdoc />
    public string InstanceId { get; }

    /// <inheritdoc />
    public string Host { get; }

    /// <inheritdoc />
    public int Port { get; }

    /// <inheritdoc />
    public bool IsSecure => _optionsMonitor.CurrentValue.EffectiveScheme == "https";

    /// <inheritdoc />
    public Uri Uri => new($"{_optionsMonitor.CurrentValue.EffectiveScheme}://{Host}:{Port}");

    /// <inheritdoc />
    public Uri? NonSecureUri => IsSecure ? null : Uri;

    /// <inheritdoc />
    public Uri? SecureUri => IsSecure ? Uri : null;

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
        ArgumentNullException.ThrowIfNull(innerRegistration);
        ArgumentNullException.ThrowIfNull(optionsMonitor);

        InnerRegistration = innerRegistration;
        _optionsMonitor = optionsMonitor;

        ServiceId = innerRegistration.Name;
        InstanceId = innerRegistration.ID;
        Host = innerRegistration.Address;
        Port = innerRegistration.Port;
        Tags = innerRegistration.Tags;
        Metadata = innerRegistration.Meta.AsReadOnly();
    }

    /// <summary>
    /// Creates a registration for the currently running app, to be submitted to the Consul server.
    /// </summary>
    /// <param name="optionsMonitor">
    /// Provides access to <see cref="ConsulDiscoveryOptions" />.
    /// </param>
    public static ConsulRegistration Create(IOptionsMonitor<ConsulDiscoveryOptions> optionsMonitor)
    {
        ArgumentNullException.ThrowIfNull(optionsMonitor);

        ConsulDiscoveryOptions options = optionsMonitor.CurrentValue;

        var agentServiceRegistration = new AgentServiceRegistration
        {
            ID = options.InstanceId,
            Address = options.HostName,
            Name = options.ServiceName,
            Tags = CreateTags(options),
            Meta = CreateMetadata(options)
        };

        if (options.Port > 0)
        {
            agentServiceRegistration.Port = options.Port;
            SetCheck(agentServiceRegistration, options);
        }

        return new ConsulRegistration(agentServiceRegistration, optionsMonitor);
    }

    private static Dictionary<string, string> CreateMetadata(ConsulDiscoveryOptions options)
    {
        Dictionary<string, string> metadata = options.Metadata.ToDictionary(pair => pair.Key, pair => pair.Value);

        if (!string.IsNullOrEmpty(options.InstanceZone) && !string.IsNullOrEmpty(options.DefaultZoneMetadataName))
        {
            metadata.TryAdd(options.DefaultZoneMetadataName, options.InstanceZone);
        }

        if (!string.IsNullOrEmpty(options.InstanceGroup))
        {
            metadata.TryAdd("group", options.InstanceGroup);
        }

        // store the secure flag in the metadata so that clients will be able to figure out whether to use http or https automatically
        metadata.TryAdd("secure", options.EffectiveScheme == "https" ? "true" : "false");

        return metadata;
    }

    private static string[] CreateTags(ConsulDiscoveryOptions options)
    {
        return options.Tags.ToArray();
    }

    public static AgentServiceCheck CreateCheck(int port, ConsulDiscoveryOptions options)
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

        if (!string.IsNullOrEmpty(options.HealthCheckUrl))
        {
            check.HTTP = options.HealthCheckUrl;
        }
        else
        {
            var uri = new Uri($"{options.EffectiveScheme}://{options.HostName}:{port}{options.HealthCheckPath}");
            check.HTTP = uri.ToString();
        }

        check.Header = new Dictionary<string, List<string>>
        {
            // Override Management:Endpoints:UseStatusCodeFromResponse, Consul only looks at HTTP status code.
            ["X-Use-Status-Code-From-Response"] = ["true"]
        };

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
