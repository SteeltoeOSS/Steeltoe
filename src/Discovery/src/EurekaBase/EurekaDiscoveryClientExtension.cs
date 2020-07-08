// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Common.Http;
using Steeltoe.Common.Net;
using Steeltoe.Common.Options;
using Steeltoe.Connector.Services;
using Steeltoe.Discovery.Client;
using static Steeltoe.Discovery.Client.DiscoveryServiceCollectionExtensions;

namespace Steeltoe.Discovery.Eureka
{
    public class EurekaDiscoveryClientExtension : IDiscoveryClientExtension
    {
        public string ServiceInfoName { get; private set; }

        public EurekaDiscoveryClientExtension(string serviceInfoName)
        {
            ServiceInfoName = serviceInfoName;
        }

        /// <inheritdoc />
        public void ApplyServices(IServiceCollection services)
        {
            var serviceProvider = services.BuildServiceProvider();
            var config = serviceProvider.GetRequiredService<IConfiguration>();
            var netOptions = config.GetSection(InetOptions.PREFIX).Get<InetOptions>();
            var lifecyle = serviceProvider.GetService<IDiscoveryLifecycle>();

            var info = string.IsNullOrEmpty(ServiceInfoName)
                ? GetSingletonDiscoveryServiceInfo(config)
                : GetNamedDiscoveryServiceInfo(config, ServiceInfoName);

            ConfigureEurekaServices(services, config, info, netOptions);
            AddEurekaServices(services, lifecyle);
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
    }
}
