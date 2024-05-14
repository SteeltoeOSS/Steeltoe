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

    internal bool IsHeartbeatEnabled => Heartbeat is { Enabled: true };
    internal bool IsRetryEnabled => Retry is { Enabled: true };

    /// <summary>
    /// Gets or sets a value indicating whether to enable the Consul client. Default value: true.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets the tags used when registering the running app.
    /// </summary>
    public IList<string> Tags { get; } = new List<string>();

    /// <summary>
    /// Gets metadata key/value pairs used when registering the running app.
    /// </summary>
    public IDictionary<string, string> Metadata { get; } = new Dictionary<string, string>();

    /// <summary>
    /// Gets or sets a value indicating whether <see cref="NetworkInterface.GetAllNetworkInterfaces" /> is used to determine <see cref="IPAddress" /> and
    /// <see cref="HostName" />. Default value: false.
    /// </summary>
    public bool UseNetworkInterfaces { get; set; }

    /// <summary>
    /// Gets or sets settings related to heartbeats.
    /// </summary>
    public ConsulHeartbeatOptions? Heartbeat { get; set; } = new();

    /// <summary>
    /// Gets settings related to retrying requests.
    /// </summary>
    public ConsulRetryOptions Retry { get; } = new();

    /// <summary>
    /// Gets or sets the tag to filter on when querying for service instances.
    /// </summary>
    public string? DefaultQueryTag { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to filter on health status 'passing' when querying for service instances. Default value: true.
    /// </summary>
    public bool QueryPassing { get; set; } = true;

    /// <summary>
    /// Gets or sets the scheme to register the running app with ("http" or "https"). Default value: http.
    /// </summary>
    public string? Scheme { get; set; } = "http";

    /// <summary>
    /// Gets or sets a value indicating whether to enable periodic health checking for the running app. Default value: true.
    /// </summary>
    public bool RegisterHealthCheck { get; set; } = true;

    /// <summary>
    /// Gets or sets the absolute URL to the health endpoint of the running app (overrides <see cref="HealthCheckPath" />).
    /// </summary>
    /// <remarks>
    /// This setting only has effect when <see cref="RegisterHealthCheck" /> is true and <see cref="ConsulHeartbeatOptions.Enabled" /> is false.
    /// </remarks>
    public string? HealthCheckUrl { get; set; }

    /// <summary>
    /// Gets or sets the relative URL to the health endpoint of the running app. Default value: /actuator/health.
    /// </summary>
    /// <remarks>
    /// This setting only has effect when <see cref="RegisterHealthCheck" /> is true and <see cref="ConsulHeartbeatOptions.Enabled" /> is false.
    /// </remarks>
    public string? HealthCheckPath { get; set; } = "/actuator/health";

    /// <summary>
    /// Gets or sets how often Concur should perform an HTTP health check. Default value: 10s.
    /// </summary>
    /// <remarks>
    /// This setting only has effect when <see cref="RegisterHealthCheck" /> is true and <see cref="ConsulHeartbeatOptions.Enabled" /> is false.
    /// </remarks>
    public string? HealthCheckInterval { get; set; } = "10s";

    /// <summary>
    /// Gets or sets the timeout Concur should use for an HTTP health check. Default value: 10s.
    /// </summary>
    /// <remarks>
    /// This setting only has effect when <see cref="RegisterHealthCheck" /> is true and <see cref="ConsulHeartbeatOptions.Enabled" /> is false.
    /// </remarks>
    public string? HealthCheckTimeout { get; set; } = "10s";

    /// <summary>
    /// Gets or sets the duration after which Consul deregisters the running app when in state critical. Default value: 30m.
    /// </summary>
    /// <remarks>
    /// This setting only has effect when <see cref="RegisterHealthCheck" /> is true.
    /// </remarks>
    public string? HealthCheckCriticalTimeout { get; set; } = "30m";

    /// <summary>
    /// Gets or sets a value indicating whether Concur should skip TLS verification for HTTP health checks. Default value: false.
    /// </summary>
    public bool HealthCheckTlsSkipVerify { get; set; }

    /// <summary>
    /// Gets or sets the host name to register the running app with (if <see cref="PreferIPAddress" /> is false).
    /// </summary>
    public string? HostName { get; set; }

    /// <summary>
    /// Gets or sets the IP address to register the running app with (if <see cref="PreferIPAddress" /> is true).
    /// </summary>
    public string? IPAddress { get; set; }

    /// <summary>
    /// Gets or sets the port number to register the running app with.
    /// </summary>
    public int Port { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to register the running app with IP address instead of host name. Default: false.
    /// </summary>
    public bool PreferIPAddress { get; set; }

    /// <summary>
    /// Gets or sets the friendly name to register the running app with.
    /// </summary>
    public string? ServiceName { get; set; }

    /// <summary>
    /// Gets or sets the unique ID to register the running app under.
    /// </summary>
    public string? InstanceId { get; set; }

    /// <summary>
    /// Gets or sets the metadata zone value to use when registering the running app.
    /// </summary>
    public string? InstanceZone { get; set; }

    /// <summary>
    /// Gets or sets the metadata "group" value to use when registering the running app.
    /// </summary>
    public string? InstanceGroup { get; set; }

    /// <summary>
    /// Gets or sets the metadata key name for <see cref="InstanceZone" />.
    /// </summary>
    public string? DefaultZoneMetadataName { get; set; } = "zone";

    /// <summary>
    /// Gets or sets a value indicating whether to throw an exception (instead of logging an error) if registration fails. Default value: true.
    /// </summary>
    public bool FailFast { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to register the running app as a service instance. Default value: true.
    /// </summary>
    public bool Register { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to de-register the running app on shutdown. Default value: true.
    /// </summary>
    public bool Deregister { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to register with the port number ASP.NET Core is listening on. Default value: true.
    /// </summary>
    public bool UseAspNetCoreUrls { get; set; } = true;
}
