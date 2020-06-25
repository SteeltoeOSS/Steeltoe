﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Net;
using System.Collections.Generic;

namespace Steeltoe.Discovery.Consul.Discovery
{
    /// <summary>
    /// Configuration options for the ConsulDiscoveryClient
    /// </summary>
    public class ConsulDiscoveryOptions
    {
        public const string CONSUL_DISCOVERY_CONFIGURATION_PREFIX = "consul:discovery";

        private string _hostName;
        private string _scheme = "http";

        public ConsulDiscoveryOptions()
        {
            if (!UseNetUtils)
            {
#pragma warning disable S1699 // Constructors should only call non-overridable methods
                _hostName = DnsTools.ResolveHostName();
                IpAddress = DnsTools.ResolveHostAddress(_hostName);
#pragma warning restore S1699 // Constructors should only call non-overridable methods
            }
        }

        public void ApplyNetUtils()
        {
            if (UseNetUtils && NetUtils != null)
            {
                var host = NetUtils.FindFirstNonLoopbackHostInfo();
                if (host.Hostname != null)
                {
                    _hostName = host.Hostname;
                }

                IpAddress = host.IpAddress;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether Consul Discovery client is enabled
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Gets or sets Tags to use when registering service
        /// </summary>
        public IList<string> Tags { get; set; }

        public bool UseNetUtils { get; set; }

        public InetUtils NetUtils { get; set; }

        /// <summary>
        /// Gets or sets values related to Heartbeat
        /// </summary>
        public ConsulHeartbeatOptions Heartbeat { get; set; } = new ConsulHeartbeatOptions();

        /// <summary>
        /// Gets or sets values related to Retrying requests
        /// </summary>
        public ConsulRetryOptions Retry { get; set; } = new ConsulRetryOptions();

        /// <summary>
        /// Gets or sets Tag to query for in service list if one is not listed in serverListQueryTags.
        /// </summary>
        public string DefaultQueryTag { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether gets or sets Add the 'passing` parameter to
        /// /v1/health/service/serviceName. This pushes health check passing to the server.
        /// </summary>
        public bool QueryPassing { get; set; } = false;

        /// <summary>
        /// Gets or sets Whether to register an http or https service
        /// </summary>
        public string Scheme
        {
            get => _scheme;
            set => _scheme = value?.ToLower();
        }

        /// <summary>
        /// Gets or sets a value indicating whether gets or sets RegisterHealthCheck in consul.
        /// Useful during development of a service.
        /// </summary>
        public bool RegisterHealthCheck { get; set; } = true;

        /// <summary>
        /// Gets or sets Custom health check url to override default
        /// </summary>
        public string HealthCheckUrl { get; set; }

        /// <summary>
        /// Gets or sets Alternate server path to invoke for health checking
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
        /// Gets or sets Timeout to deregister services critical for longer than timeout(e.g. 30m).
        /// Requires consul version 7.x or higher.
        /// </summary>
        public string HealthCheckCriticalTimeout { get; set; } = "30m";

        /// <summary>
        /// Gets or sets a value indicating whether health check verifies TLS
        /// </summary>
        public bool HealthCheckTlsSkipVerify { get; set; } = false;

        /// <summary>
        /// Gets or sets Hostname to use when accessing server
        /// </summary>
        public string HostName
        {
            get => PreferIpAddress ? IpAddress : _hostName;
            set => _hostName = value;
        }

        /// <summary>
        /// Gets or sets IP address to use when accessing service (must also set preferIpAddress to use)
        /// </summary>
        public string IpAddress { get; set; }

        /// <summary>
        /// Gets or sets Port to register the service under
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether gets or sets Use ip address rather than hostname
        /// during registration
        /// </summary>
        public bool PreferIpAddress { get; set; } = false;

        /// <summary>
        /// Gets or sets Service name
        /// </summary>
        public string ServiceName { get; set; }

        /// <summary>
        /// Gets or sets Unique service instance id
        /// </summary>
        public string InstanceId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to use agent address or hostname
        /// </summary>
        public bool PreferAgentAddress { get; set; } = false;

        /// <summary>
        /// Gets or sets the instance zone to use during registration
        /// </summary>
        public string InstanceZone { get; set; }

        /// <summary>
        /// Gets or sets the instance groupt to use during registration
        /// </summary>
        public string InstanceGroup { get; set; }

        /// <summary>
        /// Gets or sets the metadata tag name of the zone
        /// </summary>
        public string DefaultZoneMetadataName { get; set; } = "zone";

        /// <summary>
        /// Gets or sets a value indicating whether gets or sets FailFast Throw exceptions during
        /// service registration if true, otherwise, log warnings(defaults to true).
        /// </summary>
        public bool FailFast { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether gets or sets Register as a service in consul.
        /// </summary>
        public bool Register { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether gets or sets Deregister automatic de-registration
        /// of service in consul.
        /// </summary>
        public bool Deregister { get; set; } = true;

        /// <summary>
        /// Gets a value indicating whether heart beat is enabled
        /// </summary>
        public bool IsHeartBeatEnabled => Heartbeat != null && Heartbeat.Enabled;

        /// <summary>
        /// Gets a value indicating whether retry is enabled
        /// </summary>
        public bool IsRetryEnabled => Retry != null && Retry.Enabled;

        // public int CatalogServicesWatchDelay { get; set; } = 1000;

        // public int CatalogServicesWatchTimeout { get; set; } = 2;
    }
}