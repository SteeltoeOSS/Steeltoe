// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Net;

namespace Steeltoe.Discovery.Consul.Discovery;

/// <summary>
/// Configuration options for the ConsulDiscoveryClient.
/// </summary>
public class ConsulDiscoveryOptions
{
    public const string ConsulDiscoveryConfigurationPrefix = "consul:discovery";

    private string _hostName;
    private string _scheme = "http";

    /// <summary>
    /// Gets or sets a value indicating whether Consul Discovery client is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets Tags to use when registering service.
    /// </summary>
    public IList<string> Tags { get; set; }

    /// <summary>
    /// Gets or sets Metadata to use when registering service.
    /// </summary>
    public IDictionary<string, string> Metadata { get; set; }

    public bool UseNetUtils { get; set; }

    public InetUtils NetUtils { get; set; }

    /// <summary>
    /// Gets or sets values related to Heartbeat.
    /// </summary>
    public ConsulHeartbeatOptions Heartbeat { get; set; } = new();

    /// <summary>
    /// Gets or sets values related to Retrying requests.
    /// </summary>
    public ConsulRetryOptions Retry { get; set; } = new();

    /// <summary>
    /// Gets or sets Tag to query for in service list if one is not listed in serverListQueryTags.
    /// </summary>
    public string DefaultQueryTag { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether gets or sets Add the 'passing` parameter to /v1/health/service/serviceName. This pushes health check passing
    /// to the server.
    /// </summary>
    public bool QueryPassing { get; set; }

    /// <summary>
    /// Gets or sets Whether to register an http or https service.
    /// </summary>
    public string Scheme
    {
        get => _scheme;
#pragma warning disable S4040 // Strings should be normalized to uppercase
        set => _scheme = value?.ToLowerInvariant();
#pragma warning restore S4040 // Strings should be normalized to uppercase
    }

    /// <summary>
    /// Gets or sets a value indicating whether gets or sets RegisterHealthCheck in consul. Useful during development of a service.
    /// </summary>
    public bool RegisterHealthCheck { get; set; } = true;

    /// <summary>
    /// Gets or sets Custom health check url to override default.
    /// </summary>
    public string HealthCheckUrl { get; set; }

    /// <summary>
    /// Gets or sets Alternate server path to invoke for health checking.
    /// </summary>
    public string HealthCheckPath { get; set; } = "/actuator/health";

    /// <summary>
    /// Gets or sets How often to perform the health check (e.g. 10s), defaults to 10s.
    /// </summary>
    public string HealthCheckInterval { get; set; } = "10s";

    /// <summary>
    /// Gets or sets Timeout for health check (e.g. 10s).
    /// </summary>
    public string HealthCheckTimeout { get; set; } = "10s";

    /// <summary>
    /// Gets or sets Timeout to deregister services critical for longer than timeout(e.g. 30m). Requires consul version 7.x or higher.
    /// </summary>
    public string HealthCheckCriticalTimeout { get; set; } = "30m";

    /// <summary>
    /// Gets or sets a value indicating whether health check verifies TLS.
    /// </summary>
    public bool HealthCheckTlsSkipVerify { get; set; }

    /// <summary>
    /// Gets or sets Hostname to use when accessing server.
    /// </summary>
    public string HostName
    {
        get => PreferIPAddress ? IPAddress : _hostName;
        set => _hostName = value;
    }

    /// <summary>
    /// Gets or sets IP address to use when accessing service (must also set preferIPAddress to use).
    /// </summary>
    public string IPAddress { get; set; }

    /// <summary>
    /// Gets or sets Port to register the service under.
    /// </summary>
    public int Port { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether gets or sets Use ip address rather than hostname during registration.
    /// </summary>
    public bool PreferIPAddress { get; set; }

    /// <summary>
    /// Gets or sets Service name.
    /// </summary>
    public string ServiceName { get; set; }

    /// <summary>
    /// Gets or sets Unique service instance ID.
    /// </summary>
    public string InstanceId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to use agent address or hostname.
    /// </summary>
    public bool PreferAgentAddress { get; set; }

    /// <summary>
    /// Gets or sets the instance zone to use during registration.
    /// </summary>
    public string InstanceZone { get; set; }

    /// <summary>
    /// Gets or sets the instance group to use during registration.
    /// </summary>
    public string InstanceGroup { get; set; }

    /// <summary>
    /// Gets or sets the metadata tag name of the zone.
    /// </summary>
    public string DefaultZoneMetadataName { get; set; } = "zone";

    /// <summary>
    /// Gets or sets a value indicating whether gets or sets FailFast Throw exceptions during service registration if true, otherwise, log warnings(defaults
    /// to true).
    /// </summary>
    public bool FailFast { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether gets or sets Register as a service in consul.
    /// </summary>
    public bool Register { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether gets or sets Deregister automatic de-registration of service in consul.
    /// </summary>
    public bool Deregister { get; set; } = true;

    /// <summary>
    /// Gets a value indicating whether heart beat is enabled.
    /// </summary>
    public bool IsHeartBeatEnabled => Heartbeat != null && Heartbeat.Enabled;

    /// <summary>
    /// Gets a value indicating whether retry is enabled.
    /// </summary>
    public bool IsRetryEnabled => Retry != null && Retry.Enabled;

    /// <summary>
    /// Gets or sets the time in seconds that service instance cache records should remain active.
    /// </summary>
    public int CacheTtl { get; set; } = 15;

    /// <summary>
    /// Gets or sets a value indicating whether to register a Url from ASP.NET Core configuration.
    /// </summary>
    public bool UseAspNetCoreUrls { get; set; } = true;

    public ConsulDiscoveryOptions()
    {
        if (!UseNetUtils)
        {
            _hostName = DnsTools.ResolveHostName();
            IPAddress = DnsTools.ResolveHostAddress(_hostName);
        }
    }

    public void ApplyNetUtils()
    {
        if (UseNetUtils && NetUtils != null)
        {
            HostInfo host = NetUtils.FindFirstNonLoopbackHostInfo();

            if (host.Hostname != null)
            {
                _hostName = host.Hostname;
            }

            IPAddress = host.IPAddress;
        }
    }

    /// <summary>
    /// Set properties from addresses found in configuration.
    /// </summary>
    /// <param name="addresses">
    /// A list of addresses the application is listening on.
    /// </param>
    /// <param name="wildcardHostname">
    /// String representation of a wildcard hostname.
    /// </param>
    public void ApplyConfigUrls(List<Uri> addresses, string wildcardHostname)
    {
        // try to pull some values out of server config to override defaults, but only if not using NetUtils
        // if NetUtils are configured, the user probably wants to define their own behavior
        if (addresses.Any() && !UseNetUtils && UseAspNetCoreUrls && Port == 0)
        {
            // prefer https
            Uri configAddress = addresses.FirstOrDefault(u => u.Scheme == "https");

            if (configAddress == null)
            {
                configAddress = addresses.First();
            }

            Port = configAddress.Port;

            // only set the host if it isn't a wildcard
            if (configAddress.Host != wildcardHostname && configAddress.Host != "0.0.0.0")
            {
                HostName = configAddress.Host;
            }
        }
    }
}
