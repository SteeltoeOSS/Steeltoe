// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Steeltoe.Common;
using Steeltoe.Common.Discovery;
using Steeltoe.Common.Http.Discovery;
using Steeltoe.Common.Http.LoadBalancer;
using Steeltoe.Common.Reflection;
using Steeltoe.Connector;
using Steeltoe.Connector.Services;
using Steeltoe.Discovery.Client.SimpleClients;

namespace Steeltoe.Discovery.Client;

public static class DiscoveryServiceCollectionExtensions
{
    /// <summary>
    /// Adds service discovery to your application. Uses reflection to determine which clients are available and configured. If no clients are available or
    /// configured, a <see cref="NoOpDiscoveryClient" /> will be configured.
    /// </summary>
    /// <param name="services">
    /// <see cref="IServiceCollection" /> to configure.
    /// </param>
    /// <param name="configuration">
    /// Application configuration.
    /// </param>
    public static IServiceCollection AddDiscoveryClient(this IServiceCollection services, IConfiguration configuration = null)
    {
        return services.AddDiscoveryClient(configuration, null);
    }

    /// <summary>
    /// Adds service discovery to your application. Uses reflection to determine which clients are available and configured. If no clients are available or
    /// configured, a <see cref="NoOpDiscoveryClient" /> will be configured.
    /// </summary>
    /// <param name="services">
    /// <see cref="IServiceCollection" /> to configure.
    /// </param>
    /// <param name="configuration">
    /// Application configuration.
    /// </param>
    /// <param name="lifecycle">
    /// Add custom code for app shutdown events.
    /// </param>
    public static IServiceCollection AddDiscoveryClient(this IServiceCollection services, IConfiguration configuration, IDiscoveryLifecycle lifecycle = null)
    {
        return services.AddDiscoveryClient(configuration, null, lifecycle);
    }

    /// <summary>
    /// Adds service discovery to your application. Uses reflection to determine which clients are available and configured. If no clients are available or
    /// configured, a <see cref="NoOpDiscoveryClient" /> will be configured.
    /// </summary>
    /// <param name="services">
    /// <see cref="IServiceCollection" /> to configure.
    /// </param>
    /// <param name="configuration">
    /// Application configuration.
    /// </param>
    /// <param name="serviceName">
    /// Specify the name of a service binding to use.
    /// </param>
    /// <param name="lifecycle">
    /// Add custom code for app shutdown events.
    /// </param>
    public static IServiceCollection AddDiscoveryClient(this IServiceCollection services, IConfiguration configuration, string serviceName = null,
        IDiscoveryLifecycle lifecycle = null)
    {
        Action<DiscoveryClientBuilder> builderAction = null;

        configuration ??= services.BuildServiceProvider().GetRequiredService<IConfiguration>();

        IServiceInfo info = string.IsNullOrEmpty(serviceName)
            ? GetSingletonDiscoveryServiceInfo(configuration)
            : GetNamedDiscoveryServiceInfo(configuration, serviceName);

        // iterate assemblies for implementations of IDiscoveryClientExtension
        var implementations = new List<IDiscoveryClientExtension>();

        IEnumerable<Type> extensions = ReflectionHelpers.FindInterfacedTypesFromAssemblyAttribute<DiscoveryClientAssemblyAttribute>();

        foreach (Type clientExtension in extensions)
        {
            implementations.Add(Activator.CreateInstance(clientExtension) as IDiscoveryClientExtension);
        }

        if (implementations.Count == 1)
        {
            builderAction = builder => builder.Extensions.Add(implementations.First());
        }
        else if (implementations.Count > 1)
        {
            // if none configured, that's ok because AddServiceDiscovery has a plan
            IEnumerable<IDiscoveryClientExtension> configured = implementations.Where(client => client.IsConfigured(configuration, info));

            if (configured.Count() == 1)
            {
                builderAction = builder => builder.Extensions.Add(configured.Single());
            }
            else if (configured.Count() > 1)
            {
                throw new AmbiguousMatchException(
                    "Multiple IDiscoveryClient implementations have been added and configured! This is not supported, please only configure a single client type.");
            }
        }

        if (lifecycle != null)
        {
            services.AddSingleton(lifecycle);
        }

        return services.AddServiceDiscovery(builderAction);
    }

    /// <summary>
    /// Adds service discovery to your application. If <paramref name="builderAction" /> is not provided, a <see cref="NoOpDiscoveryClient" /> will be
    /// configured.
    /// </summary>
    /// <param name="serviceCollection">
    /// <see cref="IServiceCollection" /> to configure.
    /// </param>
    /// <param name="builderAction">
    /// Provided by the desired <see cref="IDiscoveryClient" /> implementation.
    /// </param>
    /// <remarks>
    /// Also configures named HttpClients "DiscoveryRandom" and "DiscoveryRoundRobin" for automatic injection.
    /// </remarks>
    /// <exception cref="AmbiguousMatchException">
    /// Thrown if multiple IDiscoveryClient implementations are configured.
    /// </exception>
    /// <exception cref="ConnectorException">
    /// Thrown if no service info with expected name or type are found or when multiple service infos are found and a single was expected.
    /// </exception>
    public static IServiceCollection AddServiceDiscovery(this IServiceCollection serviceCollection, Action<DiscoveryClientBuilder> builderAction = null)
    {
        ArgumentGuard.NotNull(serviceCollection);

        builderAction ??= builder => builder.Extensions.Add(new NoOpDiscoveryClientExtension());

        serviceCollection.RegisterDefaultApplicationInstanceInfo();
        ApplyDiscoveryOptions(serviceCollection, builderAction);

        serviceCollection.TryAddTransient<DiscoveryHttpMessageHandler>();
        serviceCollection.AddSingleton<IServiceInstanceProvider>(p => p.GetService<IDiscoveryClient>());
        serviceCollection.AddHttpClient("DiscoveryRandom").AddRandomLoadBalancer();
        serviceCollection.AddHttpClient("DiscoveryRoundRobin").AddRoundRobinLoadBalancer();
        serviceCollection.AddSingleton<IHostedService, DiscoveryClientService>();
        return serviceCollection;
    }

    /// <summary>
    /// Retrieve a single, named <see cref="IServiceInfo" /> that is expected to work with Service Discovery.
    /// </summary>
    /// <param name="configuration">
    /// The <see cref="IConfiguration" /> to search.
    /// </param>
    /// <param name="serviceName">
    /// Name of service binding to find.
    /// </param>
    /// <exception cref="ConnectorException">
    /// Thrown if no service info with expected name or type are found.
    /// </exception>
    public static IServiceInfo GetNamedDiscoveryServiceInfo(IConfiguration configuration, string serviceName)
    {
        IServiceInfo info = configuration.GetServiceInfo(serviceName);

        if (info == null)
        {
            throw new ConnectorException($"No service with name: {serviceName} found.");
        }

        if (!IsRecognizedDiscoveryService(info))
        {
            throw new ConnectorException($"Service with name: {serviceName} unrecognized Discovery ServiceInfo.");
        }

        return info;
    }

    /// <summary>
    /// Retrieve a single <see cref="IServiceInfo" /> that is expected to work with Service Discovery.
    /// </summary>
    /// <param name="configuration">
    /// The <see cref="IConfiguration" /> to search.
    /// </param>
    /// <exception cref="ConnectorException">
    /// Thrown if multiple service infos are found.
    /// </exception>
    public static IServiceInfo GetSingletonDiscoveryServiceInfo(IConfiguration configuration)
    {
        // Note: Could be other discovery type services in future
        IEnumerable<EurekaServiceInfo> eurekaInfos = configuration.GetServiceInfos<EurekaServiceInfo>();

        if (eurekaInfos.Any())
        {
            if (eurekaInfos.Count() != 1)
            {
                throw new ConnectorException("Multiple discovery service types bound to application.");
            }

            return eurekaInfos.First();
        }

        return null;
    }

    private static void ApplyDiscoveryOptions(IServiceCollection serviceCollection, Action<DiscoveryClientBuilder> builderAction)
    {
        var builder = new DiscoveryClientBuilder();

        builderAction?.Invoke(builder);

        if (builder.Extensions.Count > 1)
        {
            // TODO: don't BuildServiceProvider() here
            var configuration = serviceCollection.BuildServiceProvider().GetRequiredService<IConfiguration>();

            IEnumerable<IDiscoveryClientExtension> configured =
                builder.Extensions.Where(ext => ext.IsConfigured(configuration, GetSingletonDiscoveryServiceInfo(configuration)));

            if (!configured.Any() || configured.Count() > 1)
            {
                throw new AmbiguousMatchException(
                    "Multiple IDiscoveryClient implementations have been registered and 0 or more than 1 have been configured! This is not supported, please only use a single client type.");
            }

            builder.Extensions = configured.ToList();
        }

        foreach (IDiscoveryClientExtension ext in builder.Extensions)
        {
            ext.ApplyServices(serviceCollection);
        }
    }

    private static bool IsRecognizedDiscoveryService(IServiceInfo info)
    {
        return info is EurekaServiceInfo;
    }
}
