// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Consul;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Steeltoe.Consul.Util;
using Steeltoe.Discovery.Consul.Discovery;
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
        /// <param name="config">configuration values to use</param>
        /// <param name="options">configuration options to use</param>
        /// <returns>a registration</returns>
        public static ConsulRegistration CreateRegistration(IConfiguration config, ConsulDiscoveryOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            AgentServiceRegistration service = new AgentServiceRegistration();

            var appName = GetAppName(options, config);
            service.ID = GetInstanceId(options, config);

            if (!options.PreferAgentAddress)
            {
                service.Address = options.HostName;
            }

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

        internal static string GetAppName(ConsulDiscoveryOptions options, IConfiguration config)
        {
            string appName = options.ServiceName;
            if (!string.IsNullOrEmpty(appName))
            {
                return appName;
            }

            return config.GetValue("spring:application:name", "application");
        }

        internal static string GetInstanceId(ConsulDiscoveryOptions options, IConfiguration config)
        {
            if (string.IsNullOrEmpty(options.InstanceId))
            {
                return NormalizeForConsul(GetDefaultInstanceId(options, config));
            }

            return NormalizeForConsul(options.InstanceId);
        }

        internal static string GetDefaultInstanceId(ConsulDiscoveryOptions options, IConfiguration config)
        {
            var appName = GetAppName(options, config);
            string vcapId = config.GetValue<string>("vcap:application:instance_id", null);
            if (!string.IsNullOrEmpty(vcapId))
            {
                return appName + ":" + vcapId;
            }

            string springId = config.GetValue<string>("spring:application:instance_id", null);
            if (!string.IsNullOrEmpty(springId))
            {
                return appName + ":" + springId;
            }

            Random rand = new Random();
            return appName + ":" + rand.Next().ToString();
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

            if (!string.IsNullOrEmpty(options.HealthCheckCriticalTimeout))
            {
                check.DeregisterCriticalServiceAfter = DateTimeConversions.ToTimeSpan(options.HealthCheckCriticalTimeout);
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