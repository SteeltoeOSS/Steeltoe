// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Consul;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Discovery.Consul.Discovery;
using Steeltoe.Discovery.Consul.Util;
using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Discovery.Consul.Registry
{
    /// <summary>
    /// The registration to be used when registering with the Consul server
    /// </summary>
    public class ConsulRegistration : IConsulRegistration
    {
        private const char SEPARATOR = '-';
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

        /// <summary>
        /// Initializes a new instance of the <see cref="ConsulRegistration"/> class.
        /// </summary>
        /// <param name="agentServiceRegistration">a Consul service registration to use</param>
        /// <param name="options">configuration options</param>
        public ConsulRegistration(AgentServiceRegistration agentServiceRegistration, ConsulDiscoveryOptions options)
        {
            Service = agentServiceRegistration ?? throw new ArgumentNullException(nameof(agentServiceRegistration));
            _options = options ?? throw new ArgumentNullException(nameof(options));

            Initialize(agentServiceRegistration);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConsulRegistration"/> class.
        /// </summary>
        /// <param name="agentServiceRegistration">a Consul service registration to use</param>
        /// <param name="optionsMonitor">configuration options</param>
        public ConsulRegistration(AgentServiceRegistration agentServiceRegistration, IOptionsMonitor<ConsulDiscoveryOptions> optionsMonitor)
        {
            Service = agentServiceRegistration ?? throw new ArgumentNullException(nameof(agentServiceRegistration));
            _optionsMonitor = optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));

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

        /// <inheritdoc/>
        public AgentServiceRegistration Service { get; }

        /// <inheritdoc/>
        public string InstanceId { get; private set; }

        /// <inheritdoc/>
        public string ServiceId { get; private set; }

        /// <inheritdoc/>
        public string Host { get; private set; }

        /// <inheritdoc/>
        public int Port { get; private set; }

        /// <inheritdoc/>
        public bool IsSecure
        {
            get
            {
                return Options.Scheme == "https";
            }
        }

        /// <inheritdoc/>
        public Uri Uri
        {
            get
            {
                return new Uri($"{Options.Scheme}://{Host}:{Port}");
            }
        }

        /// <inheritdoc/>
        public IDictionary<string, string> Metadata { get; private set; }

        /// <summary>
        /// Create a Consul registration
        /// </summary>
        /// <param name="options">configuration options to use</param>
        /// <param name="applicationInfo">Info about this app instance</param>
        /// <returns>a registration</returns>
        public static ConsulRegistration CreateRegistration(ConsulDiscoveryOptions options, IApplicationInstanceInfo applicationInfo)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            var service = new AgentServiceRegistration();

            service.ID = GetInstanceId(options, applicationInfo);

            if (!options.PreferAgentAddress)
            {
                service.Address = options.HostName;
            }

            var appName = applicationInfo.ApplicationNameInContext(SteeltoeComponent.Discovery, ConsulDiscoveryOptions.CONSUL_DISCOVERY_CONFIGURATION_PREFIX + ":serviceName");
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
            List<string> tags = new List<string>();
            if (options.Tags != null)
            {
                tags.AddRange(options.Tags);
            }

            if (!string.IsNullOrEmpty(options.InstanceZone))
            {
                tags.Add(options.DefaultZoneMetadataName + "=" + options.InstanceZone);
            }

            if (!string.IsNullOrEmpty(options.InstanceGroup))
            {
                tags.Add("group=" + options.InstanceGroup);
            }

            // store the secure flag in the tags so that clients will be able to figure out whether to use http or https automatically
            tags.Add("secure=" + (options.Scheme == "https").ToString().ToLower());

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
            var appName = applicationInfo.ApplicationNameInContext(SteeltoeComponent.Discovery, ConsulDiscoveryOptions.CONSUL_DISCOVERY_CONFIGURATION_PREFIX + ":serviceName");
            var instanceId = applicationInfo.InstanceId;
            if (string.IsNullOrEmpty(instanceId))
            {
                var rand = new Random();
                instanceId = rand.Next().ToString();
            }

            return appName + ":" + instanceId;
        }

        internal static string NormalizeForConsul(string s)
        {
            if (s == null || !char.IsLetter(s[0]) || !char.IsLetterOrDigit(s[s.Length - 1]))
            {
                throw new ArgumentException("Consul service ids must not be empty, must start with a letter, end with a letter or digit, and have as interior characters only letters, digits, and hyphen: " + s);
            }

            StringBuilder normalized = new StringBuilder();
            char prev = default;
            foreach (char curr in s)
            {
                char toAppend = default;
                if (char.IsLetterOrDigit(curr))
                {
                    toAppend = curr;
                }
                else if (prev == default(char) || prev != SEPARATOR)
                {
                    toAppend = SEPARATOR;
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
            AgentServiceCheck check = new AgentServiceCheck();

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
                throw new ArgumentException("CreateCheck port must be greater than 0");
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
}