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

using System;
using System.Net;
using System.Threading;
using Consul;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Steeltoe.Common.Discovery;
using Steeltoe.Discovery.Consul;
using Steeltoe.Discovery.Consul.Discovery;
using Steeltoe.Discovery.Consul.ServiceRegistry;
using Steeltoe.Discovery.Consul.Util;

namespace Steeltoe.Discovery.ClientConsul
{
    public static class DiscoveryServiceCollectionExtensions
    {
        public static IServiceCollection AddConsulDiscoveryClient(
            this IServiceCollection services,
            IConfiguration configuration,
            IDiscoveryLifecycle lifecycle = null)
        {
            var consulConfigSection = configuration.GetSection("consul");

            services.Configure<ConsulOptions>(consulConfigSection);

            var discoveryConfigurationSection = consulConfigSection.GetSection("discovery");
            services.Configure<ConsulDiscoveryOptions>(discoveryConfigurationSection);

            services.Configure<HeartbeatOptions>(discoveryConfigurationSection.GetSection("heartbeat"));

            services.PostConfigure<ConsulDiscoveryOptions>(options =>
            {
                if (!options.PreferIpAddress && string.IsNullOrWhiteSpace(options.HostName))
                {
                    options.HostName = Dns.GetHostName();
                }

                if (string.IsNullOrWhiteSpace(options.InstanceId))
                {
                    options.InstanceId = configuration["spring:application:instance_id"];
                }

                if (string.IsNullOrWhiteSpace(options.InstanceId))
                {
                    options.InstanceId = options.HostName + ":" + options.Port;
                }

                if (string.IsNullOrWhiteSpace(options.ServiceName))
                {
                    options.ServiceName = configuration["spring:application:name"];
                }

                if (string.IsNullOrWhiteSpace(options.ServiceName))
                {
                    options.ServiceName = configuration["applicationName"];
                }
            });

            AddConsulServices(services, lifecycle);
            return services;
        }

        private static void AddConsulServices(IServiceCollection services, IDiscoveryLifecycle lifecycle)
        {
            services.AddSingleton(s =>
            {
                var consulOptions = s.GetRequiredService<IOptions<ConsulOptions>>().Value;
                return new ConsulClient(options =>
                {
                    options.Address = new Uri($"{consulOptions.Scheme}://{consulOptions.Host}:{consulOptions.Port}");
                    options.Datacenter = consulOptions.Datacenter;
                    options.Token = consulOptions.Token;
                    if (!string.IsNullOrWhiteSpace(consulOptions.WaitTime))
                    {
                        options.WaitTime = DateTimeConversions.ToTimeSpan(consulOptions.WaitTime);
                    }
                });
            });
            services.AddSingleton<TtlScheduler>();

            services.AddSingleton<ConsulDiscoveryClient>();

            services.AddSingleton<ConsulServiceRegistry>();

            if (lifecycle == null)
            {
                services.AddSingleton<IDiscoveryLifecycle, ApplicationLifecycle>();
            }
            else
            {
                services.AddSingleton(lifecycle);
            }

            services.AddSingleton<IDiscoveryClient>(p => p.GetService<ConsulDiscoveryClient>());
        }

        internal class ApplicationLifecycle : IDiscoveryLifecycle
        {
            public ApplicationLifecycle(IApplicationLifetime lifeCycle)
            {
                ApplicationStopping = lifeCycle.ApplicationStopping;
            }

            public CancellationToken ApplicationStopping { get; set; }
        }
    }
}