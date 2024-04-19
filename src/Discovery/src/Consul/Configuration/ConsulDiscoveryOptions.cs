// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net.NetworkInformation;

namespace Steeltoe.Discovery.Consul.Configuration;

/// <summary>
/// Configuration options for <see cref="ConsulDiscoveryClient" />.
/// </summary>
public sealed class ConsulDiscoveryOptions
{
    internal const string ConfigurationPrefix = "consul:discovery";

    /// <summary>
    /// Gets a value indicating whether heart beats are enabled.
    /// </summary>
    internal bool IsHeartbeatEnabled => Heartbeat is { Enabled: true };

    /// <summary>
    /// Gets a value indicating whether retries are enabled.
    /// </summary>
    internal bool IsRetryEnabled => Retry is { Enabled: true };

    /// <summary>
    /// Gets or sets a value indicating whether the Consul discovery client is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets tags to use when registering a service.
    /// </summary>
    public IList<string> Tags { get; } = new List<string>();

    /// <summary>
    /// Gets metadata to use when registering a service.
    /// </summary>
    public IDictionary<string, string> Metadata { get; } = new Dictionary<string, string>();

    /// <summary>
    /// Gets or sets a value indicating whether <see cref="NetworkInterface.GetAllNetworkInterfaces" /> is used to determine <see cref="IPAddress" /> and
    /// <see cref="HostName" /> .
    /// </summary>
    public bool UseNetworkInterfaces { get; set; }

    /// <summary>
    /// Gets or sets values related to heartbeat.
    /// </summary>
    public ConsulHeartbeatOptions? Heartbeat { get; set; } = new();

    /// <summary>
    /// Gets values related to retrying requests.
    /// </summary>
    public ConsulRetryOptions Retry { get; } = new();

    /// <summary>
    /// Gets or sets the tag to query for in the service list, if one is not listed in serverListQueryTags.
    /// </summary>
    public string? DefaultQueryTag { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to add the "passing" parameter to /v1/health/service/serviceName. This pushes health check passing to the
    /// server.
    /// </summary>
    public bool QueryPassing { get; set; }

    /// <summary>
    /// Gets or sets whether to register an http or https service.
    /// </summary>
    public string? Scheme { get; set; } = "http";

    /// <summary>
    /// Gets or sets a value indicating whether to register health checks in Consul. Useful during development of a service.
    /// </summary>
    public bool RegisterHealthCheck { get; set; } = true;

    /// <summary>
    /// Gets or sets a custom health check url, to override the default.
    /// </summary>
    public string? HealthCheckUrl { get; set; }

    /// <summary>
    /// Gets or sets an alternate server path to invoke for health checking.
    /// </summary>
    public string? HealthCheckPath { get; set; } = "/actuator/health";

    /// <summary>
    /// Gets or sets how often to perform the health check (e.g. 10s), defaults to 10s.
    /// </summary>
    public string? HealthCheckInterval { get; set; } = "10s";

    /// <summary>
    /// Gets or sets the timeout for health checks (e.g. 10s), defaults to 10s.
    /// </summary>
    public string? HealthCheckTimeout { get; set; } = "10s";

    /// <summary>
    /// Gets or sets the timeout to deregister services critical for longer than timeout (e.g. 30m). Requires Consul version 7.x or higher.
    /// </summary>
    public string? HealthCheckCriticalTimeout { get; set; } = "30m";

    /// <summary>
    /// Gets or sets a value indicating whether health check verifies TLS.
    /// </summary>
    public bool HealthCheckTlsSkipVerify { get; set; }

    /// <summary>
    /// Gets or sets the hostname to use when accessing the Consul server.
    /// </summary>
    public string? HostName { get; set; }

    /// <summary>
    /// Gets or sets the IP address to use when accessing the service (must also set <see cref="PreferIPAddress" /> to use).
    /// </summary>
    public string? IPAddress { get; set; }

    /// <summary>
    /// Gets or sets the port to register the service under.
    /// </summary>
    public int Port { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to use an IP address rather than a hostname during registration.
    /// </summary>
    public bool PreferIPAddress { get; set; }

    /// <summary>
    /// Gets or sets the service name.
    /// </summary>
    public string? ServiceName { get; set; }

    /// <summary>
    /// Gets or sets the unique service instance ID.
    /// </summary>
    public string? InstanceId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to use agent address or hostname.
    /// </summary>
    public bool PreferAgentAddress { get; set; }

    /// <summary>
    /// Gets or sets the instance zone to use during registration.
    /// </summary>
    public string? InstanceZone { get; set; }

    /// <summary>
    /// Gets or sets the instance group to use during registration.
    /// </summary>
    public string? InstanceGroup { get; set; }

    /// <summary>
    /// Gets or sets the metadata tag name of the zone.
    /// </summary>
    public string? DefaultZoneMetadataName { get; set; } = "zone";

    /// <summary>
    /// Gets or sets a value indicating whether to throw exceptions during service registration. If false, logs warnings. Defaults to true.
    /// </summary>
    public bool FailFast { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to register as a service in Consul.
    /// </summary>
    public bool Register { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to use automatic de-registration of a service in Consul.
    /// </summary>
    public bool Deregister { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to determine <see cref="Port" /> from ASP.NET Core listening addresses configuration.
    /// </summary>
    public bool UseAspNetCoreUrls { get; set; } = true;
}
