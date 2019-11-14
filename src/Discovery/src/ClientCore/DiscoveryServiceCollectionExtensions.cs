﻿// Copyright 2017 the original author or authors.
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
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Steeltoe.CloudFoundry.Connector;
using Steeltoe.CloudFoundry.Connector.Services;
using Steeltoe.Common.Discovery;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Common.Http.Discovery;
using Steeltoe.Consul.Client;
using Steeltoe.Discovery.Consul.Discovery;
using Steeltoe.Discovery.Consul.Registry;
using Steeltoe.Discovery.Eureka;
using System;
using System.Linq;
using System.Threading;

namespace Steeltoe.Discovery.Client
{
    public static class DiscoveryServiceCollectionExtensions
    {
        public const string EUREKA_PREFIX = "eureka";
        public const string CONSUL_PREFIX = "consul";

        public static IServiceCollection AddDiscoveryClient(this IServiceCollection services, DiscoveryOptions discoveryOptions, IDiscoveryLifecycle lifecycle = null)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (discoveryOptions == null)
            {
                throw new ArgumentNullException(nameof(discoveryOptions));
            }

            if (discoveryOptions.ClientType == DiscoveryClientType.EUREKA)
            {
                EurekaClientOptions clientOptions = discoveryOptions.ClientOptions as EurekaClientOptions;
                if (clientOptions == null)
                {
                    throw new ArgumentException("Missing Client Options");
                }

                services.AddSingleton<IOptionsMonitor<EurekaClientOptions>>(new OptionsMonitorWrapper<EurekaClientOptions>(clientOptions));

                var regOptions = discoveryOptions.RegistrationOptions as EurekaInstanceOptions;
                if (regOptions == null)
                {
                    clientOptions.ShouldRegisterWithEureka = false;
                    regOptions = new EurekaInstanceOptions();
                }

                services.AddSingleton<IOptionsMonitor<EurekaInstanceOptions>>(new OptionsMonitorWrapper<EurekaInstanceOptions>(regOptions));

                AddEurekaServices(services, lifecycle);
            }
            else
            {
                throw new ArgumentException("Client type UNKNOWN");
            }

            services.TryAddTransient<DiscoveryHttpMessageHandler>();
            return services;
        }

        public static IServiceCollection AddDiscoveryClient(this IServiceCollection services, Action<DiscoveryOptions> setupOptions, IDiscoveryLifecycle lifecycle = null)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (setupOptions == null)
            {
                throw new ArgumentNullException(nameof(setupOptions));
            }

            var options = new DiscoveryOptions();
            setupOptions(options);

            return services.AddDiscoveryClient(options, lifecycle);
        }

        public static IServiceCollection AddDiscoveryClient(this IServiceCollection services, IConfiguration config, IDiscoveryLifecycle lifecycle = null)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            IServiceInfo info = GetSingletonDiscoveryServiceInfo(config);

            AddDiscoveryServices(services, info, config, lifecycle);

            return services;
        }

        public static IServiceCollection AddDiscoveryClient(this IServiceCollection services, IConfiguration config, string serviceName, IDiscoveryLifecycle lifecycle = null)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (string.IsNullOrEmpty(serviceName))
            {
                throw new ArgumentNullException(nameof(serviceName));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            IServiceInfo info = GetNamedDiscoveryServiceInfo(config, serviceName);

            AddDiscoveryServices(services, info, config, lifecycle);

            return services;
        }

        private static void AddDiscoveryServices(IServiceCollection services, IServiceInfo info, IConfiguration config, IDiscoveryLifecycle lifecycle)
        {
            if (IsEurekaConfigured(config, info))
            {
                ConfigureEurekaServices(services, config, info);
                AddEurekaServices(services, lifecycle);
            }
            else if (IsConsulConfigured(config, info))
            {
                ConfigureConsulServices(services, config, info);
                AddConsulServices(services, config, lifecycle);
            }
            else
            {
                throw new ArgumentException("Discovery client type UNKNOWN, check configuration");
            }

            services.TryAddTransient<DiscoveryHttpMessageHandler>();
        }

        #region Consul
        private static bool IsConsulConfigured(IConfiguration config, IServiceInfo info)
        {
            var clientConfigsection = config.GetSection(CONSUL_PREFIX);
            int childCount = clientConfigsection.GetChildren().Count();
            return childCount > 0;
        }

        private static void ConfigureConsulServices(IServiceCollection services, IConfiguration config, IServiceInfo info)
        {
            var consulSection = config.GetSection(ConsulOptions.CONSUL_CONFIGURATION_PREFIX);
            services.Configure<ConsulOptions>(consulSection);
            var consulDiscoverySection = config.GetSection(ConsulDiscoveryOptions.CONSUL_DISCOVERY_CONFIGURATION_PREFIX);
            services.Configure<ConsulDiscoveryOptions>(consulDiscoverySection);
        }

        private static void AddConsulServices(IServiceCollection services, IConfiguration config, IDiscoveryLifecycle lifecycle)
        {
            services.AddSingleton((p) =>
            {
                var consulOptions = p.GetRequiredService<IOptions<ConsulOptions>>();
                return ConsulClientFactory.CreateClient(consulOptions.Value);
            });

            services.AddSingleton<IScheduler, TtlScheduler>();
            services.AddSingleton<IConsulServiceRegistry, ConsulServiceRegistry>();
            services.AddSingleton<IConsulRegistration>((p) =>
            {
                var opts = p.GetRequiredService<IOptions<ConsulDiscoveryOptions>>();
                return ConsulRegistration.CreateRegistration(config, opts.Value);
            });
            services.AddSingleton<IConsulServiceRegistrar, ConsulServiceRegistrar>();
            services.AddSingleton<IDiscoveryClient, ConsulDiscoveryClient>();
            services.AddSingleton<IServiceInstanceProvider, ConsulDiscoveryClient>();
            services.AddSingleton<IHealthContributor, ConsulHealthContributor>();
        }
        #endregion Consul

        #region Eureka
        private static bool IsEurekaConfigured(IConfiguration config, IServiceInfo info)
        {
            var clientConfigsection = config.GetSection(EUREKA_PREFIX);
            int childCount = clientConfigsection.GetChildren().Count();
            return childCount > 0 || info is EurekaServiceInfo;
        }

        private static void ConfigureEurekaServices(IServiceCollection services, IConfiguration config, IServiceInfo info)
        {
            EurekaServiceInfo einfo = info as EurekaServiceInfo;
            var clientSection = config.GetSection(EurekaClientOptions.EUREKA_CLIENT_CONFIGURATION_PREFIX);
            services.Configure<EurekaClientOptions>(clientSection);
            services.PostConfigure<EurekaClientOptions>((options) =>
            {
                EurekaPostConfigurer.UpdateConfiguration(config, einfo, options);
            });

            var instSection = config.GetSection(EurekaInstanceOptions.EUREKA_INSTANCE_CONFIGURATION_PREFIX);
            services.Configure<EurekaInstanceOptions>(instSection);
            services.PostConfigure<EurekaInstanceOptions>((options) =>
            {
                EurekaPostConfigurer.UpdateConfiguration(config, einfo, options);
            });
        }

        private static void AddEurekaServices(IServiceCollection services, IDiscoveryLifecycle lifecycle)
        {
            services.AddSingleton<EurekaApplicationInfoManager>();
            services.AddSingleton<EurekaDiscoveryManager>();

            services.AddSingleton<EurekaDiscoveryClient>();
            if (lifecycle == null)
            {
                services.AddSingleton<IDiscoveryLifecycle, ApplicationLifecycle>();
            }
            else
            {
                services.AddSingleton(lifecycle);
            }

            services.AddSingleton<IDiscoveryClient>((p) =>
            {
                var eurekaService = p.GetService<EurekaDiscoveryClient>();

                // Wire in health checker if present
                if (eurekaService != null)
                {
                    eurekaService.HealthCheckHandler = p.GetService<IHealthCheckHandler>();
                }

                return eurekaService;
            });

            services.AddSingleton<IServiceInstanceProvider>(p => p.GetService<EurekaDiscoveryClient>());
            services.AddSingleton<IHealthContributor, EurekaServerHealthContributor>();
        }

        #endregion Eureka

        #region ServiceInfo
        private static IServiceInfo GetNamedDiscoveryServiceInfo(IConfiguration config, string serviceName)
        {
            var info = config.GetServiceInfo(serviceName);
            if (info == null)
            {
                throw new ConnectorException(string.Format("No service with name: {0} found.", serviceName));
            }

            if (!IsRecognizedDiscoveryService(info))
            {
                throw new ConnectorException(string.Format("Service with name: {0} unrecognized Discovery ServiceInfo.", serviceName));
            }

            return info;
        }

        private static IServiceInfo GetSingletonDiscoveryServiceInfo(IConfiguration config)
        {
            // Note: Could be other discovery type services in future
            var eurekaInfos = config.GetServiceInfos<EurekaServiceInfo>();

            if (eurekaInfos.Count > 0)
            {
                if (eurekaInfos.Count != 1)
                {
                    throw new ConnectorException("Multiple discovery service types bound to application.");
                }

                return eurekaInfos[0];
            }

            return null;
        }

        private static bool IsRecognizedDiscoveryService(IServiceInfo info)
        {
            return info is EurekaServiceInfo;
        }

        #endregion ServiceInfo

        public class ApplicationLifecycle : IDiscoveryLifecycle
        {
            public ApplicationLifecycle(IApplicationLifetime lifeCycle, IDiscoveryClient client)
            {
                ApplicationStopping = lifeCycle.ApplicationStopping;

                // hook things up so that that things are unregistered when the application terminates
                ApplicationStopping.Register(() => { client.ShutdownAsync().GetAwaiter().GetResult(); });
            }

            public CancellationToken ApplicationStopping { get; set; }
        }

        public class OptionsMonitorWrapper<T> : IOptionsMonitor<T>
        {
            private T _option;

            public OptionsMonitorWrapper(T option)
            {
                _option = option;
            }

            public T CurrentValue => _option;

            public T Get(string name)
            {
                return _option;
            }

            public IDisposable OnChange(Action<T, string> listener)
            {
                return null;
            }
        }
    }
}
