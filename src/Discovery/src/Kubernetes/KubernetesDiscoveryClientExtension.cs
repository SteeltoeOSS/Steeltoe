// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using k8s;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Common.Kubernetes;
using Steeltoe.Connector.Services;
using Steeltoe.Discovery.Client;
using Steeltoe.Discovery.Kubernetes.Discovery;
using System;
using System.Linq;

namespace Steeltoe.Discovery.Kubernetes
{
    public class KubernetesDiscoveryClientExtension : IDiscoveryClientExtension
    {
        public void ApplyServices(IServiceCollection services)
        {
            var serviceProvider = services.BuildServiceProvider();
            var config = serviceProvider.GetRequiredService<IConfiguration>();
            ConfigureKubernetesServices(services, config);
            AddKubernetesServices(services);
        }

        public bool IsConfigured(IConfiguration configuration, IServiceInfo serviceInfo = null)
        {
            return configuration
                .GetSection(KubernetesDiscoveryOptions.KUBERNETES_DISCOVERY_CONFIGURATION_PREFIX)
                .GetChildren()
                .Any();
        }

        private static void ConfigureKubernetesServices(IServiceCollection services, IConfiguration config)
        {
            var kubernetesSection = config.GetSection(KubernetesDiscoveryOptions.KUBERNETES_DISCOVERY_CONFIGURATION_PREFIX);
            services.Configure<KubernetesDiscoveryOptions>(kubernetesSection);
        }

        private static void AddKubernetesServices(IServiceCollection services)
        {
            services.AddKubernetesClient();
            services.AddOptions<KubernetesDiscoveryOptions>().PostConfigure<KubernetesApplicationOptions>((options, appOptions) =>
            {
                options.ServiceName = appOptions.ApplicationNameInContext(SteeltoeComponent.Kubernetes, appOptions.KubernetesRoot + ":discovery:servicename");
                if (options.Namespace == "default" && appOptions.NameSpace != "default")
                {
                    options.Namespace = appOptions.NameSpace;
                }
            });
            services.AddSingleton((p) =>
            {
                var kubernetesOptions = p.GetRequiredService<IOptionsMonitor<KubernetesDiscoveryOptions>>();
                var kubernetes = p.GetRequiredService<IKubernetes>();
                return KubernetesDiscoveryClientFactory.CreateClient(kubernetesOptions, kubernetes);
            });
            services.TryAddSingleton(serviceProvider =>
            {
                var clientOptions = serviceProvider.GetRequiredService<IOptions<KubernetesDiscoveryOptions>>();
                return new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(clientOptions.Value.CacheTTL) };
            });
        }
    }
}