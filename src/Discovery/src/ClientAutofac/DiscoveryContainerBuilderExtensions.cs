// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Autofac;
using Consul;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Steeltoe.CloudFoundry.Connector;
using Steeltoe.CloudFoundry.Connector.Services;
using Steeltoe.Common.Discovery;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Common.Net;
using Steeltoe.Common.Options.Autofac;
using Steeltoe.Consul.Client;
using Steeltoe.Discovery.Consul.Discovery;
using Steeltoe.Discovery.Consul.Registry;
using Steeltoe.Discovery.Eureka;
using System;
using System.Linq;
using System.Threading;

namespace Steeltoe.Discovery.Client
{
    public static class DiscoveryContainerBuilderExtensions
    {
        public const string EUREKA_PREFIX = "eureka";
        public const string CONSUL_PREFIX = "consul";

        public static void RegisterDiscoveryClient(this ContainerBuilder container, DiscoveryOptions discoveryOptions, IDiscoveryLifecycle lifecycle = null)
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }

            if (discoveryOptions == null)
            {
                throw new ArgumentNullException(nameof(discoveryOptions));
            }

            if (discoveryOptions.ClientType == DiscoveryClientType.EUREKA)
            {
                if (!(discoveryOptions.ClientOptions is EurekaClientOptions clientOptions))
                {
                    throw new ArgumentException("Missing Client Options");
                }

                container.RegisterInstance(new OptionsMonitorWrapper<EurekaClientOptions>(clientOptions)).As<IOptionsMonitor<EurekaClientOptions>>().SingleInstance();

                if (!(discoveryOptions.RegistrationOptions is EurekaInstanceOptions regOptions))
                {
                    clientOptions.ShouldRegisterWithEureka = false;
                    regOptions = new EurekaInstanceOptions();
                }

                container.RegisterInstance(new OptionsMonitorWrapper<EurekaInstanceOptions>(regOptions)).As<IOptionsMonitor<EurekaInstanceOptions>>().SingleInstance();

                AddEurekaServices(container, lifecycle);
            }
            else
            {
                throw new ArgumentException("Client type UNKNOWN");
            }
        }

        public static void RegisterDiscoveryClient(this ContainerBuilder container, Action<DiscoveryOptions> setupOptions, IDiscoveryLifecycle lifecycle = null)
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }

            if (setupOptions == null)
            {
                throw new ArgumentNullException(nameof(setupOptions));
            }

            var options = new DiscoveryOptions();
            setupOptions(options);

            container.RegisterDiscoveryClient(options, lifecycle);
        }

        public static void RegisterDiscoveryClient(this ContainerBuilder container, IConfiguration config, IDiscoveryLifecycle lifecycle = null)
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            IServiceInfo info = GetSingletonDiscoveryServiceInfo(config);
            AddDiscoveryServices(container, info, config, lifecycle);
        }

        public static void RegisterDiscoveryClient(
            this ContainerBuilder container,
            IConfiguration config,
            string serviceName,
            IDiscoveryLifecycle lifecycle = null)
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
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

            AddDiscoveryServices(container, info, config, lifecycle);
        }

        public static void StartDiscoveryClient(this IContainer container)
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }

            container.Resolve<IDiscoveryClient>();
        }

        private static void AddDiscoveryServices(
            ContainerBuilder container,
            IServiceInfo info,
            IConfiguration config,
            IDiscoveryLifecycle lifecycle)
        {
            var netOptions = config.GetSection(InetOptions.PREFIX).Get<InetOptions>();
            if (IsEurekaConfigured(config, info))
            {
                ConfigureEurekaServices(container, config, info, netOptions);
                AddEurekaServices(container, lifecycle);
            }
            else if (IsConsulConfigured(config, info))
            {
                ConfigureConsulServices(container, config, info, netOptions);
                AddConsulServices(container, config, lifecycle);
            }
            else
            {
                throw new ArgumentException("Discovery client type UNKNOWN, check configuration");
            }
        }

        #region Consul
        private static bool IsConsulConfigured(IConfiguration config, IServiceInfo info)
        {
            var clientConfigsection = config.GetSection(CONSUL_PREFIX);
            int childCount = clientConfigsection.GetChildren().Count();
            return childCount > 0;
        }

        private static void ConfigureConsulServices(ContainerBuilder container, IConfiguration config, IServiceInfo info, InetOptions netOptions)
        {
            var consulSection = config.GetSection(ConsulOptions.CONSUL_CONFIGURATION_PREFIX);
            container.RegisterOption<ConsulOptions>(consulSection);
            var consulDiscoverySection = config.GetSection(ConsulDiscoveryOptions.CONSUL_DISCOVERY_CONFIGURATION_PREFIX);
            container.RegisterOption<ConsulDiscoveryOptions>(consulDiscoverySection);
            container.RegisterPostConfigure<ConsulDiscoveryOptions>(options =>
            {
                options.NetUtils = new InetUtils(netOptions);
                options.ApplyNetUtils();
            });
        }

        private static void AddConsulServices(ContainerBuilder container, IConfiguration config, IDiscoveryLifecycle lifecycle)
        {
            container.Register(c =>
            {
                var opts = c.Resolve<IOptions<ConsulOptions>>();
                return ConsulClientFactory.CreateClient(opts.Value);
            }).As<IConsulClient>().SingleInstance();

            container.RegisterType<TtlScheduler>().As<IScheduler>().SingleInstance();
            container.RegisterType<ConsulServiceRegistry>().As<IConsulServiceRegistry>().SingleInstance();
            container.Register(c =>
            {
                var opts = c.Resolve<IOptions<ConsulDiscoveryOptions>>();
                return ConsulRegistration.CreateRegistration(config, opts.Value);
            }).As<IConsulRegistration>().SingleInstance();

            container.RegisterType<ConsulServiceRegistrar>().As<IConsulServiceRegistrar>().SingleInstance();
            container.RegisterType<ConsulDiscoveryClient>().As<IDiscoveryClient>().As<IServiceInstanceProvider>().SingleInstance();

            container.RegisterType<ConsulHealthContributor>().As<IHealthContributor>().SingleInstance();
        }
        #endregion Consul

        #region Eureka
        private static bool IsEurekaConfigured(IConfiguration config, IServiceInfo info)
        {
            var clientConfigsection = config.GetSection(EUREKA_PREFIX);
            int childCount = clientConfigsection.GetChildren().Count();
            return childCount > 0 || info is EurekaServiceInfo;
        }

        private static void ConfigureEurekaServices(ContainerBuilder container, IConfiguration config, IServiceInfo info, InetOptions netOptions)
        {
            EurekaServiceInfo einfo = info as EurekaServiceInfo;

            var clientSection = config.GetSection(EurekaClientOptions.EUREKA_CLIENT_CONFIGURATION_PREFIX);
            container.RegisterOption<EurekaClientOptions>(clientSection);
            container.RegisterPostConfigure<EurekaClientOptions>(options =>
            {
                EurekaPostConfigurer.UpdateConfiguration(config, einfo, options);
            });

            var instSection = config.GetSection(EurekaInstanceOptions.EUREKA_INSTANCE_CONFIGURATION_PREFIX);
            container.RegisterOption<EurekaInstanceOptions>(instSection);
            container.RegisterPostConfigure<EurekaInstanceOptions>(options =>
            {
                options.NetUtils = new InetUtils(netOptions);
                options.ApplyNetUtils();
                EurekaPostConfigurer.UpdateConfiguration(config, einfo, options);
            });
        }

        private static void AddEurekaServices(ContainerBuilder container, IDiscoveryLifecycle lifecycle)
        {
            container.RegisterType<EurekaApplicationInfoManager>().SingleInstance();
            container.RegisterType<EurekaDiscoveryManager>().SingleInstance();
            container.RegisterType<EurekaDiscoveryClient>().AsSelf().As<IDiscoveryClient>().As<IServiceInstanceProvider>().SingleInstance();

            if (lifecycle == null)
            {
                container.RegisterType<ApplicationLifecycle>().As<IDiscoveryLifecycle>();
            }
            else
            {
                container.RegisterInstance(lifecycle).SingleInstance();
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
            private CancellationTokenSource source = new CancellationTokenSource();

            public ApplicationLifecycle()
            {
            }

            public void Shutdown()
            {
                source.Cancel();
            }

            public CancellationToken ApplicationStopping
            {
                get
                {
                    return source.Token;
                }
            }
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
