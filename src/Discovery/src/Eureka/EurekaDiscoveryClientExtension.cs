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
            ConfigureEurekaServices(services);
            AddEurekaServices(services);
        }

        public bool IsConfigured(IConfiguration configuration, IServiceInfo serviceInfo = null)
        {
            return configuration.GetSection(EUREKA_PREFIX).GetChildren().Any() || serviceInfo is EurekaServiceInfo;
        }

        private void ConfigureEurekaServices(IServiceCollection services)
        {
            services
                .AddOptions<EurekaClientOptions>()
                .Configure<IConfiguration>((options, config) =>
                {
                    config.GetSection(EurekaClientOptions.EUREKA_CLIENT_CONFIGURATION_PREFIX).Bind(options);
                })
                .PostConfigure<IConfiguration>((options, config) =>
                {
                    var info = GetServiceInfo(config);
                    EurekaPostConfigurer.UpdateConfiguration(config, info, options);
                });

            services
                .AddOptions<EurekaInstanceOptions>()
                .Configure<IConfiguration>((options, config) => config.GetSection(EurekaInstanceOptions.EUREKA_INSTANCE_CONFIGURATION_PREFIX).Bind(options))
                .PostConfigure<IConfiguration, IApplicationInstanceInfo>((options, config, appInfo) =>
                {
                    var inetOptions = config.GetSection(InetOptions.PREFIX).Get<InetOptions>();
                    options.NetUtils = new InetUtils(inetOptions);
                    options.ApplyNetUtils();
                    var info = GetServiceInfo(config);
                    EurekaPostConfigurer.UpdateConfiguration(config, info, options, info?.ApplicationInfo ?? appInfo);
                });

            services.TryAddSingleton(serviceProvider =>
            {
                var clientOptions = serviceProvider.GetRequiredService<IOptions<EurekaClientOptions>>();
                return new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(clientOptions.Value.CacheTTL) };
            });
        }

        private void AddEurekaServices(IServiceCollection services)
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

            var certOptions = services.FirstOrDefault(descriptor => descriptor.ServiceType == typeof(IConfigureOptions<CertificateOptions>));
            var existingHandler = services.FirstOrDefault(descriptor => descriptor.ServiceType == typeof(IHttpClientHandlerProvider));

            if (certOptions is object && existingHandler is null)
            {
                services.AddSingleton<IHttpClientHandlerProvider, ClientCertificateHttpHandlerProvider>();
            }
        }

        private EurekaServiceInfo GetServiceInfo(IConfiguration config)
        {
            ServiceInfoName ??= config.GetValue<string>("eureka:serviceInfoName");
            var info = string.IsNullOrEmpty(ServiceInfoName)
                ? GetSingletonDiscoveryServiceInfo(config)
                : GetNamedDiscoveryServiceInfo(config, ServiceInfoName);

            return info as EurekaServiceInfo;
        }
    }
}
