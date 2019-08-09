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

using Autofac;
using Consul;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Steeltoe.CloudFoundry.Connector;
using Steeltoe.CloudFoundry.Connector.Services;
using Steeltoe.Common.Discovery;
using Steeltoe.Common.HealthChecks;
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
                EurekaClientOptions clientOptions = discoveryOptions.ClientOptions as EurekaClientOptions;
                if (clientOptions == null)
                {
                    throw new ArgumentException("Missing Client Options");
                }

                container.RegisterInstance(new OptionsMonitorWrapper<EurekaClientOptions>(clientOptions)).As<IOptionsMonitor<EurekaClientOptions>>().SingleInstance();

                var regOptions = discoveryOptions.RegistrationOptions as EurekaInstanceOptions;
                if (regOptions == null)
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
            if (IsEurekaConfigured(config, info))
            {
                ConfigureEurekaServices(container, config, info);
                AddEurekaServices(container, lifecycle);
            }
            else if (IsConsulConfigured(config, info))
            {
                ConfigureConsulServices(container, config, info);
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

        private static void ConfigureConsulServices(ContainerBuilder container, IConfiguration config, IServiceInfo info)
        {
            var consulSection = config.GetSection(ConsulOptions.CONSUL_CONFIGURATION_PREFIX);
            container.RegisterOption<ConsulOptions>(consulSection);
            var consulDiscoverySection = config.GetSection(ConsulDiscoveryOptions.CONSUL_DISCOVERY_CONFIGURATION_PREFIX);
            container.RegisterOption<ConsulDiscoveryOptions>(consulDiscoverySection);
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
            container.RegisterType<ConsulDiscoveryClient>().As<IDiscoveryClient>().SingleInstance();

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

        private static void ConfigureEurekaServices(ContainerBuilder container, IConfiguration config, IServiceInfo info)
        {
            EurekaServiceInfo einfo = info as EurekaServiceInfo;

            var clientSection = config.GetSection(EurekaClientOptions.EUREKA_CLIENT_CONFIGURATION_PREFIX);
            container.RegisterOption<EurekaClientOptions>(clientSection);
            container.RegisterPostConfigure<EurekaClientOptions>((options) =>
            {
                EurekaPostConfigurer.UpdateConfiguration(config, einfo, options);
            });

            var instSection = config.GetSection(EurekaInstanceOptions.EUREKA_INSTANCE_CONFIGURATION_PREFIX);
            container.RegisterOption<EurekaInstanceOptions>(instSection);
            container.RegisterPostConfigure<EurekaInstanceOptions>((options) =>
            {
                EurekaPostConfigurer.UpdateConfiguration(config, einfo, options);
            });
        }

        private static void AddEurekaServices(ContainerBuilder container, IDiscoveryLifecycle lifecycle)
        {
            container.RegisterType<EurekaApplicationInfoManager>().SingleInstance();
            container.RegisterType<EurekaDiscoveryManager>().SingleInstance();
            container.RegisterType<EurekaDiscoveryClient>().AsSelf().As<IDiscoveryClient>().SingleInstance();

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
