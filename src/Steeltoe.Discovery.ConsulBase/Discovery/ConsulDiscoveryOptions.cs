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

namespace Steeltoe.Discovery.Consul.Discovery
{
    public class ConsulDiscoveryOptions
    {
        public ConsulDiscoveryOptions()
        {
            Register = true;
            Deregister = true;
            RegisterHealthCheck = true;
            FailFast = true;
            HealthCheckPath = "/actuator/health";
        }

        /// <summary>
        /// Gets or sets Tags to use when registering service
        /// </summary>
        public string[] Tags { get; set; }

        /// <summary>
        /// Gets or sets Alternate server path to invoke for health checking
        /// </summary>
        public string HealthCheckPath { get; set; }

        /// <summary>
        /// Gets or sets Custom health check url to override default
        /// </summary>
        public string HealthCheckUrl { get; set; }

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
        /// Gets or sets IP address to use when accessing service (must also set preferIpAddress to use)
        /// </summary>
        public string IpAddress { get; set; }

        private string _hostName;

        /// <summary>
        /// Gets or sets Hostname to use when accessing server
        /// </summary>
        public string HostName
        {
            get => PreferIpAddress ? IpAddress : _hostName;
            set => _hostName = value;
        }

        /// <summary>
        /// Gets or sets Port to register the service under (defaults to listening port)
        /// </summary>
        public int? Port { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether gets or sets Use ip address rather than hostname
        /// during registration
        /// </summary>
        public bool PreferIpAddress { get; set; }

        /// <summary>
        /// Gets or sets Service name
        /// </summary>
        public string ServiceName { get; set; }

        /// <summary>
        /// Gets or sets Unique service instance id
        /// </summary>
        public string InstanceId { get; set; }

        private string _scheme = "http";

        /// <summary>
        /// Gets or sets Whether to register an http or https service
        /// </summary>
        public string Scheme
        {
            get => _scheme;
            set => _scheme = value?.ToLower();
        }

        /// <summary>
        /// Gets or sets Tag to query for in service list if one is not listed in serverListQueryTags.
        /// </summary>
        public string DefaultQueryTag { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether gets or sets Add the 'passing` parameter to
        /// /v1/health/service/serviceName. This pushes health check passing to the server.
        /// </summary>
        public bool QueryPassing { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether gets or sets Register as a service in consul.
        /// </summary>
        public bool Register { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether gets or sets Deregister automatic de-registration
        /// of service in consul.
        /// </summary>
        public bool Deregister { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether gets or sets RegisterHealthCheck in consul.
        /// Useful during development of a service.
        /// </summary>
        public bool RegisterHealthCheck { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether gets or sets FailFast Throw exceptions during
        /// service registration if true, otherwise, log warnings(defaults to true).
        /// </summary>
        public bool FailFast { get; set; }
    }
}