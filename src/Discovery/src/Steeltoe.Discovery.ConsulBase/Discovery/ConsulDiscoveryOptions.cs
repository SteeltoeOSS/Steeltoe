// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace Steeltoe.Discovery.Consul.Discovery
{
    /// <summary>
    /// Configuration options for the ConsulDiscoveryClient
    /// </summary>
    public class ConsulDiscoveryOptions
    {
        public const string CONSUL_DISCOVERY_CONFIGURATION_PREFIX = "consul:discovery";

        private string _hostName;
        private string _hostAddress;
        private string _scheme = "http";

        public ConsulDiscoveryOptions()
        {
            _hostName = ResolveHostName();
            _hostAddress = ResolveHostAddress(_hostName);
        }

        /// <summary>
        /// Gets or sets a value indicating whether Consul Discovery client is enabled
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Gets or sets Tags to use when registering service
        /// </summary>
        public IList<string> Tags { get; set; }

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
            get => PreferIpAddress ? _hostAddress : _hostName;
            set => _hostName = value;
        }

        /// <summary>
        /// Gets or sets IP address to use when accessing service (must also set preferIpAddress to use)
        /// </summary>
        public string IpAddress
        {
            get => _hostAddress;
            set => _hostAddress = value;
        }

        /// <summary>
        /// Gets or sets Port to register the service under (defaults to listening port)
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
        public bool IsHeartBeatEnabled
        {
            get
            {
                return Heartbeat != null ? Heartbeat.Enabled : false;
            }
        }

        /// <summary>
        /// Gets a value indicating whether retry is enabled
        /// </summary>
        public bool IsRetryEnabled
        {
            get
            {
                return Retry != null ? Retry.Enabled : false;
            }
        }

        // TODO: This code lifted from Eureka, refactor into common
        protected virtual string ResolveHostAddress(string hostName)
        {
            string result = null;
            try
            {
                var results = Dns.GetHostAddresses(hostName);
                if (results != null && results.Length > 0)
                {
                    foreach (var addr in results)
                    {
                        if (addr.AddressFamily.Equals(AddressFamily.InterNetwork))
                        {
                            result = addr.ToString();
                            break;
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Ignore
            }

            return result;
        }

        protected virtual string ResolveHostName()
        {
            string result = null;
            try
            {
                result = Dns.GetHostName();
                if (!string.IsNullOrEmpty(result))
                {
                    var response = Dns.GetHostEntry(result);
                    if (response != null)
                    {
                        return response.HostName;
                    }
                }
            }
            catch (Exception)
            {
                // Ignore
            }

            return result;
        }

        // public int CatalogServicesWatchDelay { get; set; } = 1000;

        // public int CatalogServicesWatchTimeout { get; set; } = 2;
    }
}