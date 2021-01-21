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
#if NETSTANDARD2_1_OR_GREATER
using Steeltoe.Management.Endpoint.Health;
using Steeltoe.Management.Endpoint.Hypermedia;
using Steeltoe.Management.Endpoint.Info;
#endif
using System;
using System.Linq;
using static Steeltoe.Discovery.Client.DiscoveryServiceCollectionExtensions;

namespace Steeltoe.Discovery.Eureka
{
    public class EurekaDiscoveryClientExtension : IDiscoveryClientExtension
    {
        public const string EUREKA_PREFIX = "eureka";
        private const string _springDiscoveryEnabled = "spring:cloud:discovery:enabled";

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

        internal void ConfigureEurekaServices(IServiceCollection services)
        {
            services
                .AddOptions<EurekaClientOptions>()
                .Configure<IConfiguration>((options, config) =>
                {
                    config.GetSection(EurekaClientOptions.EUREKA_CLIENT_CONFIGURATION_PREFIX).Bind(options);

                    // Eureka is enabled by default. If eureka:client:enabled was not set then check spring:cloud:discovery:enabled
                    if (options.Enabled &&
                        config.GetValue<bool?>(EurekaClientOptions.EUREKA_CLIENT_CONFIGURATION_PREFIX + ":enabled") is null &&
                        config.GetValue<bool?>(_springDiscoveryEnabled) == false)
                    {
                        options.Enabled = false;
                    }
                })
                .PostConfigure<IConfiguration>((options, config) =>
                {
                    var info = GetServiceInfo(config);
                    EurekaPostConfigurer.UpdateConfiguration(config, info, options);
                });

            services
                .AddOptions<EurekaInstanceOptions>()
                .Configure<IConfiguration>((options, config) => config.GetSection(EurekaInstanceOptions.EUREKA_INSTANCE_CONFIGURATION_PREFIX).Bind(options))
                .PostConfigure<IServiceProvider>((options, serviceProvider) =>
                {
                    var config = serviceProvider.GetRequiredService<IConfiguration>();
                    var appInfo = serviceProvider.GetRequiredService<IApplicationInstanceInfo>();
                    var inetOptions = config.GetSection(InetOptions.PREFIX).Get<InetOptions>();
                    options.NetUtils = new InetUtils(inetOptions);
                    options.ApplyNetUtils();
#if NETSTANDARD2_1_OR_GREATER
                    var mgmtOptions = serviceProvider.GetService<ActuatorManagementOptions>();
                    if (mgmtOptions is object)
                    {
                        if (string.IsNullOrEmpty(config.GetValue<string>(EurekaInstanceOptions.EUREKA_INSTANCE_CONFIGURATION_PREFIX + ":HealthCheckUrlPath")))
                        {
                            var healthOptions = serviceProvider.GetService<IHealthOptions>();
                            if (healthOptions is object)
                            {
                                options.HealthCheckUrlPath = mgmtOptions.Path + '/' + healthOptions.Path.TrimStart('/');
                            }
                        }

                        if (string.IsNullOrEmpty(config.GetValue<string>(EurekaInstanceOptions.EUREKA_INSTANCE_CONFIGURATION_PREFIX + ":StatusPageUrlPath")))
                        {
                            var infoOptions = serviceProvider.GetService<IInfoOptions>();
                            if (infoOptions is object)
                            {
                                options.StatusPageUrlPath = mgmtOptions.Path + '/' + infoOptions.Path.TrimStart('/');
                            }
                        }
                    }
#endif
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
#pragma warning disable 618
                services.AddSingleton<IHttpClientHandlerProvider, ClientCertificateHttpHandlerProvider>();
#pragma warning restore 618
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
