// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace Steeltoe.Discovery.Client
{
    public static class DiscoveryServiceCollectionExtensions
    {
        /// <summary>
        /// Adds service discovery to your application. Uses reflection to determine which clients are available and configured.
        /// If no clients are available or configured, a <see cref="NoOpDiscoveryClient"/> will be configured
        /// </summary>
        /// <param name="services"><see cref="IServiceCollection"/> to configure</param>
        /// <param name="config">Application configuration</param>
        public static IServiceCollection AddDiscoveryClient(this IServiceCollection services, IConfiguration config = null)
        {
            return services.AddDiscoveryClient(config, null);
        }

        /// <summary>
        /// Adds service discovery to your application. Uses reflection to determine which clients are available and configured.
        /// If no clients are available or configured, a <see cref="NoOpDiscoveryClient"/> will be configured
        /// </summary>
        /// <param name="services"><see cref="IServiceCollection"/> to configure</param>
        /// <param name="config">Application configuration</param>
        /// <param name="lifecycle">Add custom code for app shutdown events</param>
        public static IServiceCollection AddDiscoveryClient(this IServiceCollection services, IConfiguration config, IDiscoveryLifecycle lifecycle = null)
        {
            return services.AddDiscoveryClient(config, null, lifecycle);
        }

        /// <summary>
        /// Adds service discovery to your application. Uses reflection to determine which clients are available and configured.
        /// If no clients are available or configured, a <see cref="NoOpDiscoveryClient"/> will be configured
        /// </summary>
        /// <param name="services"><see cref="IServiceCollection"/> to configure</param>
        /// <param name="config">Application configuration</param>
        /// <param name="serviceName">Specify the name of a service binding to use</param>
        /// <param name="lifecycle">Add custom code for app shutdown events</param>
        public static IServiceCollection AddDiscoveryClient(this IServiceCollection services, IConfiguration config, string serviceName = null, IDiscoveryLifecycle lifecycle = null)
        {
            Action<DiscoveryClientBuilder> builderAction = null;
            config ??= services.BuildServiceProvider().GetRequiredService<IConfiguration>();
            var info = string.IsNullOrEmpty(serviceName)
                ? GetSingletonDiscoveryServiceInfo(config)
                : GetNamedDiscoveryServiceInfo(config, serviceName);

            // todo: provide a hook for specifying additional assemblies to search
            var assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(n => n.FullName.StartsWith("Steeltoe.Discovery"));

            // iterate assemblies for implementations of IDiscoveryClientExtension
            var implementations = new List<IDiscoveryClientExtension>();
            foreach (var assembly in assemblies)
            {
                var clientExtensions = assembly.GetTypes().Where(t => t.GetInterface("IDiscoveryClientExtension") != null);
                foreach (var clientExtension in clientExtensions)
                {
                    implementations.Add(Activator.CreateInstance(clientExtension) as IDiscoveryClientExtension);
                }
            }

            if (implementations.Count == 1)
            {
                builderAction = builder => builder.Extensions.Add(implementations.First());
            }
            else if (implementations.Count > 1)
            {
                // if none configured, that's ok because AddServiceDiscovery has a plan
                var configured = implementations.Where(client => client.IsConfigured(config, info));
                if (configured.Count() == 1)
                {
                    builderAction = builder => builder.Extensions.Add(configured.Single());
                }
                else if (configured.Count() > 1)
                {
                    throw new AmbiguousMatchException("Multiple IDiscoveryClient implementations have been added and configured! This is not supported, please only configure a single client type.");
                }
            }

            if (lifecycle != null)
            {
                services.AddSingleton(lifecycle);
            }

            return services.AddServiceDiscovery(builderAction);
        }

        /// <summary>
        /// Adds service discovery to your application. If <paramref name="builderAction"/> is not provided, a <see cref="NoOpDiscoveryClient"/> will be configured
        /// </summary>
        /// <param name="serviceCollection"><see cref="IServiceCollection"/> to configure</param>
        /// <param name="builderAction">Provided by the desired <see cref="IDiscoveryClient"/> implementation</param>
        /// <remarks>Also configures named HttpClients "DiscoveryRandom" and "DiscoveryRoundRobin" for automatic injection</remarks>
        /// <exception cref="AmbiguousMatchException">Thrown if multiple IDiscoveryClient implementations are configured</exception>
        /// <exception cref="ConnectorException">Thrown if no service info with expected name or type are found or when multiple service infos are found and a single was expected</exception>
        public static IServiceCollection AddServiceDiscovery(this IServiceCollection serviceCollection, Action<DiscoveryClientBuilder> builderAction = null)
        {
            if (serviceCollection is null)
            {
                throw new ArgumentNullException(nameof(serviceCollection));
            }

            if (builderAction is null)
            {
                builderAction = (builder) => builder.Extensions.Add(new NoOpDiscoveryClientExtension());
            }

            ApplyDiscoveryOptions(serviceCollection, builderAction);

            serviceCollection.TryAddTransient<DiscoveryHttpMessageHandler>();
            serviceCollection.AddSingleton<IServiceInstanceProvider>(p => p.GetService<IDiscoveryClient>());
            serviceCollection.AddHttpClient("DiscoveryRandom").AddRandomLoadBalancer();
            serviceCollection.AddHttpClient("DiscoveryRoundRobin").AddRoundRobinLoadBalancer();
            serviceCollection.TryAddSingleton<IDiscoveryLifecycle, ApplicationLifecycle>();
            return serviceCollection;
        }

        /// <summary>
        /// Retrieve a single, named <see cref="IServiceInfo"/> that is expected to work with Service Discovery
        /// </summary>
        /// <param name="config">The <see cref="IConfiguration"/> to search</param>
        /// <param name="serviceName">Name of service binding to find</param>
        /// <exception cref="ConnectorException">Thrown if no service info with expected name or type are found</exception>
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

        /// <summary>
        /// Retrieve a single <see cref="IServiceInfo"/> that is expected to work with Service Discovery
        /// </summary>
        /// <param name="config">The <see cref="IConfiguration"/> to search</param>
        /// <exception cref="ConnectorException">Thrown if multiple service infos are found</exception>
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

        private static void ApplyDiscoveryOptions(IServiceCollection serviceCollection, Action<DiscoveryClientBuilder> builderAction)
        {
            var builder = new DiscoveryClientBuilder();

            builderAction?.Invoke(builder);

            foreach (var ext in builder.Extensions)
            {
                ext.ApplyServices(serviceCollection);
            }

            if (serviceCollection.Count(descriptor => descriptor.ServiceType.IsAssignableFrom(typeof(IDiscoveryClient))) > 1)
            {
                throw new AmbiguousMatchException("Multiple IDiscoveryClient implementations have been configured! This is not supported, please only use a single client type.");
            }
        }

        private static bool IsRecognizedDiscoveryService(IServiceInfo info) => info is EurekaServiceInfo;

        public class ApplicationLifecycle : IDiscoveryLifecycle
        {
            public ApplicationLifecycle(IHostApplicationLifetime lifeCycle, IDiscoveryClient client)
            {
                ApplicationStopping = lifeCycle.ApplicationStopping;

                // hook things up so that that things are unregistered when the application terminates
                ApplicationStopping.Register(() =>
                {
                    client.ShutdownAsync().GetAwaiter().GetResult();
                });
            }

            public CancellationToken ApplicationStopping { get; set; }
        }
    }
}
