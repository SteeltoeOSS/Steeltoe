// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Common.Http;
using Steeltoe.Common.Net;
using Steeltoe.Common.Options;
using Steeltoe.Connector.Services;
using Steeltoe.Discovery.Client;
using System;
using System.Linq;
using static Steeltoe.Discovery.Client.DiscoveryServiceCollectionExtensions;

namespace Steeltoe.Discovery.Eureka
{
    public class EurekaDiscoveryClientExtension : IDiscoveryClientExtension
    {
        public const string EUREKA_PREFIX = "eureka";

        public string ServiceInfoName { get; private set; }

        public EurekaDiscoveryClientExtension()
            : this(null)
        {
        }

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

            ServiceInfoName ??= config.GetValue<string>("eureka:serviceInfoName");
            var info = string.IsNullOrEmpty(ServiceInfoName)
                ? GetSingletonDiscoveryServiceInfo(config)
                : GetNamedDiscoveryServiceInfo(config, ServiceInfoName);

            ConfigureEurekaServices(services, config, info, netOptions);
            AddEurekaServices(services);
        }

        public bool IsConfigured(IConfiguration configuration, IServiceInfo serviceInfo = null)
        {
            return configuration.GetSection(EUREKA_PREFIX).GetChildren().Any() || serviceInfo is EurekaServiceInfo;
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
            services.TryAddSingleton(serviceProvider =>
            {
                var clientOptions = serviceProvider.GetRequiredService<IOptions<EurekaClientOptions>>();
                return new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(clientOptions.Value.CacheTTL) };
            });
        }

        private static void AddEurekaServices(IServiceCollection services)
        {
            services.AddSingleton<EurekaApplicationInfoManager>();
            services.AddSingleton<EurekaDiscoveryManager>();
            services.AddSingleton<EurekaDiscoveryClient>();
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
            if (certOptions is object && existingHandler is null)
            {
                services.AddSingleton<IHttpClientHandlerProvider, ClientCertificateHttpHandlerProvider>();
            }
        }
    }
}
