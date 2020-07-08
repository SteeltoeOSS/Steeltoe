// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using k8s;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Steeltoe.Common.Discovery;
using Steeltoe.Common.Http.Discovery;
using Steeltoe.Connector;
using Steeltoe.Connector.Services;
using Steeltoe.Discovery.Client.SimpleClients;
using System;
using System.Threading;

namespace Steeltoe.Discovery.Client
{
    public static class DiscoveryServiceCollectionExtensions
    {
        [Obsolete("This extension has been removed. Please use AddServiceDiscovery instead", true)]
        public static IServiceCollection AddDiscoveryClient(this IServiceCollection services, IConfiguration config, IDiscoveryLifecycle lifecycle = null)
        {
            return services;
        }

        [Obsolete("This extension has been removed. Please use AddServiceDiscovery instead", true)]
        public static IServiceCollection AddDiscoveryClient(this IServiceCollection services, IConfiguration config, string serviceName, IDiscoveryLifecycle lifecycle = null)
        {
            return services;
        }

        public static IServiceCollection AddServiceDiscovery(this IServiceCollection serviceCollection, Action<DiscoveryClientBuilder> optionsAction = null)
        {
            if (serviceCollection is null)
            {
                throw new ArgumentNullException(nameof(serviceCollection));
            }

            if (optionsAction is null)
            {
                optionsAction = (options) => options.Extensions.Add(new NoOpDiscoveryClientExtension());
            }

            ApplyDiscoveryOptions(serviceCollection, optionsAction);

            serviceCollection.TryAddTransient<DiscoveryHttpMessageHandler>();
            serviceCollection.AddSingleton<IServiceInstanceProvider>(p => p.GetService<IDiscoveryClient>());
            return serviceCollection;
        }

        public static IServiceInfo GetNamedDiscoveryServiceInfo(IConfiguration config, string serviceName)
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

        public static IServiceInfo GetSingletonDiscoveryServiceInfo(IConfiguration config)
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

        private static void ApplyDiscoveryOptions(IServiceCollection serviceCollection, Action<DiscoveryClientBuilder> optionsAction)
        {
            var builder = new DiscoveryClientBuilder();

            optionsAction?.Invoke(builder);

            foreach (var ext in builder.Extensions)
            {
                ext.ApplyServices(serviceCollection);
            }
        }

        private static bool IsRecognizedDiscoveryService(IServiceInfo info) => info is EurekaServiceInfo;

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
    }
}
