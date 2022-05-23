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
using Steeltoe.Common.Reflection;
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
                        config.GetValue<bool?>($"{EurekaClientOptions.EUREKA_CLIENT_CONFIGURATION_PREFIX}:enabled") is null &&
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
                    var endpointAssembly = "Steeltoe.Management.EndpointBase";
                    if (ReflectionHelpers.IsAssemblyLoaded(endpointAssembly))
                    {
                        var actuatorOptionsType = ReflectionHelpers.FindType(new string[] { endpointAssembly }, new string[] { "Steeltoe.Management.Endpoint.Hypermedia.ActuatorManagementOptions" });
                        var endpointOptionsBaseType = ReflectionHelpers.FindType(new string[] { "Steeltoe.Management.Abstractions" }, new string[] { "Steeltoe.Management.IEndpointOptions" });
                        var mgmtOptions = serviceProvider.GetService(actuatorOptionsType);
                        if (mgmtOptions != null)
                        {
                            var basePath = $"{(string)actuatorOptionsType.GetProperty("Path").GetValue(mgmtOptions)}/";
                            if (string.IsNullOrEmpty(config.GetValue<string>($"{EurekaInstanceOptions.EUREKA_INSTANCE_CONFIGURATION_PREFIX}:HealthCheckUrlPath")))
                            {
                                var healthOptionsType = ReflectionHelpers.FindType(new string[] { endpointAssembly }, new string[] { "Steeltoe.Management.Endpoint.Health.IHealthOptions" });
                                var healthOptions = serviceProvider.GetService(healthOptionsType);
                                if (healthOptions != null)
                                {
                                    options.HealthCheckUrlPath = basePath + ((string)endpointOptionsBaseType.GetProperty("Path").GetValue(healthOptions)).TrimStart('/');
                                }
                            }

                            if (string.IsNullOrEmpty(config.GetValue<string>($"{EurekaInstanceOptions.EUREKA_INSTANCE_CONFIGURATION_PREFIX}:StatusPageUrlPath")))
                            {
                                var infoOptionsType = ReflectionHelpers.FindType(new string[] { endpointAssembly }, new string[] { "Steeltoe.Management.Endpoint.Info.IInfoOptions" });
                                var infoOptions = serviceProvider.GetService(infoOptionsType);
                                if (infoOptions != null)
                                {
                                    options.StatusPageUrlPath = basePath + ((string)endpointOptionsBaseType.GetProperty("Path").GetValue(infoOptions)).TrimStart('/');
                                }
                            }
                        }
                    }

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

            if (existingHandler is IHttpClientHandlerProvider handlerProvider)
            {
                AddEurekaHttpClient(services)
                    .ConfigurePrimaryHttpMessageHandler(() => handlerProvider.GetHttpClientHandler());
            }
            else
            {
                if (certOptions is null)
                {
                    AddEurekaHttpClient(services);
                }
                else
                {
                    AddEurekaHttpClient(services)
                        .ConfigurePrimaryHttpMessageHandler(services => new ClientCertificateHttpHandler(services.GetRequiredService<IOptionsMonitor<CertificateOptions>>()));
                }
            }
        }

        private IHttpClientBuilder AddEurekaHttpClient(IServiceCollection services)
            => services.AddHttpClient<EurekaDiscoveryClient>("Eureka", (services, client) =>
            {
                var clientOptions = services.GetRequiredService<IOptions<EurekaClientOptions>>();
                if (clientOptions.Value.EurekaServerConnectTimeoutSeconds > 0)
                {
                    client.Timeout = TimeSpan.FromSeconds(clientOptions.Value.EurekaServerConnectTimeoutSeconds);
                }
            });

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
