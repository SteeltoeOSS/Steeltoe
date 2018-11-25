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

using Autofac;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Steeltoe.CloudFoundry.Connector;
using Steeltoe.CloudFoundry.Connector.Services;
using Steeltoe.Common.Discovery;
using Steeltoe.Common.Options.Autofac;
using Steeltoe.Discovery.Eureka;
using System;
using System.Linq;
using System.Threading;

namespace Steeltoe.Discovery.Client
{
    public static class DiscoveryContainerBuilderExtensions
    {
        public const string EUREKA_PREFIX = "eureka";

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
            var clientConfigsection = config.GetSection(EUREKA_PREFIX);
            int childCount = clientConfigsection.GetChildren().Count();
            if (childCount > 0 || info is EurekaServiceInfo)
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
                AddEurekaServices(container, lifecycle);
            }
            else
            {
                throw new ArgumentException("Discovery client type UNKNOWN, check configuration");
            }
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
                    throw new ConnectorException(string.Format("Multiple discovery service types bound to application."));
                }

                return eurekaInfos[0];
            }

            return null;
        }

        private static bool IsRecognizedDiscoveryService(IServiceInfo info)
        {
            return (info as EurekaServiceInfo) != null;
        }

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
