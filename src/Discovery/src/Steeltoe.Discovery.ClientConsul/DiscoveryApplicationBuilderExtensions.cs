// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except
// in compliance with the License. You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software distributed under the License
// is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
// or implied. See the License for the specific language governing permissions and limitations under
// the License.

using System;
using Consul;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Steeltoe.Common.Discovery;
using Steeltoe.Discovery.Consul.Discovery;
using Steeltoe.Discovery.Consul.ServiceRegistry;
using Steeltoe.Discovery.Consul.Util;

namespace Steeltoe.Discovery.ClientConsul
{
    public static class DiscoveryApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseConsulDiscoveryClient(this IApplicationBuilder app)
        {
            var services = app.ApplicationServices;

            var consulDiscoveryOptions = services.GetService<IOptions<ConsulDiscoveryOptions>>()?.Value;
            var heartbeatOptions = services.GetService<IOptions<HeartbeatOptions>>()?.Value;

            if (consulDiscoveryOptions == null || !consulDiscoveryOptions.Register)
            {
                return app;
            }

            var registration = BuildRegistration(consulDiscoveryOptions, heartbeatOptions);

            var consulRegistration = new ConsulRegistration(registration, consulDiscoveryOptions);

            var consulServiceRegistry = services.GetRequiredService<ConsulServiceRegistry>();
            consulServiceRegistry.RegisterAsync(consulRegistration).GetAwaiter().GetResult();

            if (!consulDiscoveryOptions.Deregister)
            {
                return app;
            }

            var discoveryLifecycle = app.ApplicationServices.GetRequiredService<IDiscoveryLifecycle>();
            discoveryLifecycle.ApplicationStopping.Register(async () =>
            {
                await consulServiceRegistry.DeregisterAsync(consulRegistration);
            });

            return app;
        }

        private static AgentServiceRegistration BuildRegistration(
            ConsulDiscoveryOptions options,
            HeartbeatOptions heartbeatOptions)
        {
            if (!options.Port.HasValue)
            {
                throw new ArgumentException("Port can not be empty.");
            }

            return new AgentServiceRegistration
            {
                Address = options.HostName,
                ID = options.InstanceId,
                Name = options.ServiceName,
                Port = options.Port.Value,
                Tags = options.Tags,
                Check = CreateCheck(options, heartbeatOptions)
            };
        }

        private static bool SetHeartbeat(AgentServiceCheck check, HeartbeatOptions heartbeatOptions)
        {
            if (!heartbeatOptions.Enable || heartbeatOptions.TtlValue <= 0 ||
                string.IsNullOrEmpty(heartbeatOptions.TtlUnit))
            {
                return false;
            }

            check.Interval = null;
            check.HTTP = null;

            TimeSpan? ttl = DateTimeConversions.ToTimeSpan(heartbeatOptions.TtlValue + heartbeatOptions.TtlUnit);
            check.TTL = ttl;

            return true;
        }

        private static bool SetHttpCheck(AgentServiceCheck check, ConsulDiscoveryOptions options)
        {
            var healthCheckUrl = options.HealthCheckUrl;

            if (string.IsNullOrEmpty(healthCheckUrl))
            {
                var hostString = options.HostName;
                var port = options.Port;
                hostString += ":" + port;

                var healthCheckPath = options.HealthCheckPath;
                if (!healthCheckPath.StartsWith("/"))
                {
                    healthCheckPath = "/" + healthCheckPath;
                }

                healthCheckUrl = $"{options.Scheme}://{hostString}{healthCheckPath}";
            }

            TimeSpan? interval = null;

            if (!string.IsNullOrWhiteSpace(options.HealthCheckInterval))
            {
                interval = DateTimeConversions.ToTimeSpan(options.HealthCheckInterval);
            }

            if (string.IsNullOrEmpty(healthCheckUrl) || interval == null)
            {
                return false;
            }

            check.HTTP = healthCheckUrl;
            check.Interval = interval;

            return true;
        }

        private static AgentServiceCheck CreateCheck(ConsulDiscoveryOptions options, HeartbeatOptions heartbeatOptions)
        {
            if (!options.RegisterHealthCheck)
            {
                return null;
            }

            TimeSpan? deregisterCriticalServiceAfter = null;
            TimeSpan? timeout = null;

            if (!string.IsNullOrWhiteSpace(options.HealthCheckTimeout))
            {
                timeout = DateTimeConversions.ToTimeSpan(options.HealthCheckTimeout);
            }

            if (!string.IsNullOrWhiteSpace(options.HealthCheckCriticalTimeout))
            {
                deregisterCriticalServiceAfter = DateTimeConversions.ToTimeSpan(options.HealthCheckCriticalTimeout);
            }

            var check = new AgentServiceCheck
            {
                Timeout = timeout,
                DeregisterCriticalServiceAfter = deregisterCriticalServiceAfter
            };

            if (heartbeatOptions.Enable)
            {
                return SetHeartbeat(check, heartbeatOptions) ? check : null;
            }

            return SetHttpCheck(check, options) ? check : null;
        }
    }
}