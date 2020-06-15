// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Common.Discovery;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Common.Http;
using Steeltoe.Common.Http.Discovery;
using Steeltoe.Common.Net;
using Steeltoe.Common.Options;
using Steeltoe.Connector;
using Steeltoe.Connector.Services;
using Steeltoe.Discovery.Consul;
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
                var clientOptions = discoveryOptions.ClientOptions as EurekaClientOptions;
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

            var info = GetSingletonDiscoveryServiceInfo(config);

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

            var info = GetNamedDiscoveryServiceInfo(config, serviceName);

            AddDiscoveryServices(services, info, config, lifecycle);

            return services;
        }

        private static void AddDiscoveryServices(IServiceCollection services, IServiceInfo info, IConfiguration config, IDiscoveryLifecycle lifecycle)
        {
            var netOptions = config.GetSection(InetOptions.PREFIX).Get<InetOptions>();
            if (IsEurekaConfigured(config, info))
            {
                ConfigureEurekaServices(services, config, info, netOptions);
                AddEurekaServices(services, lifecycle);
            }
            else if (IsConsulConfigured(config, info))
            {
                ConfigureConsulServices(services, config, info, netOptions);
                AddConsulServices(services, config, lifecycle);
            }
            else
            {
                throw new ArgumentException("Discovery client type UNKNOWN, check configuration");
            }

            services.TryAddTransient<DiscoveryHttpMessageHandler>();
            services.AddSingleton<IServiceInstanceProvider>(p => p.GetService<IDiscoveryClient>());
        }

        #region Consul
        private static bool IsConsulConfigured(IConfiguration config, IServiceInfo info)
        {
            var clientConfigsection = config.GetSection(CONSUL_PREFIX);
            int childCount = clientConfigsection.GetChildren().Count();
            return childCount > 0;
        }

        private static void ConfigureConsulServices(IServiceCollection services, IConfiguration config, IServiceInfo info, InetOptions netOptions)
        {
            var consulSection = config.GetSection(ConsulOptions.CONSUL_CONFIGURATION_PREFIX);
            services.Configure<ConsulOptions>(consulSection);
            var consulDiscoverySection = config.GetSection(ConsulDiscoveryOptions.CONSUL_DISCOVERY_CONFIGURATION_PREFIX);
            services.Configure<ConsulDiscoveryOptions>(consulDiscoverySection);
            services.PostConfigure<ConsulDiscoveryOptions>(options =>
            {
                options.NetUtils = new InetUtils(netOptions);
                options.ApplyNetUtils();
            });
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
                var appInfo = services.GetApplicationInstanceInfo();
                return ConsulRegistration.CreateRegistration(opts.Value, appInfo);
            });
            services.AddSingleton<IConsulServiceRegistrar, ConsulServiceRegistrar>();
            services.AddSingleton<IDiscoveryClient, ConsulDiscoveryClient>();
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

        private static void ConfigureEurekaServices(IServiceCollection services, IConfiguration config, IServiceInfo info, InetOptions netOptions)
        {
            var einfo = info as EurekaServiceInfo;
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
                IApplicationInstanceInfo appInfo = null;
                if (einfo?.ApplicationInfo == null)
                {
                    appInfo = services.GetApplicationInstanceInfo();
                }

                options.NetUtils = new InetUtils(netOptions);
                options.ApplyNetUtils();
                EurekaPostConfigurer.UpdateConfiguration(config, einfo, options, einfo?.ApplicationInfo ?? appInfo);
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

            services.AddSingleton<IHealthContributor, EurekaServerHealthContributor>();

            var serviceProvider = services.BuildServiceProvider();
            var certOptions = serviceProvider.GetService<IOptions<CertificateOptions>>();
            var existingHandler = serviceProvider.GetService<IHttpClientHandlerProvider>();
            if (certOptions != null && existingHandler is null)
            {
                services.AddSingleton<IHttpClientHandlerProvider, ClientCertificateHttpHandlerProvider>();
            }
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
            public ApplicationLifecycle(IHostApplicationLifetime lifeCycle, IDiscoveryClient client)
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
