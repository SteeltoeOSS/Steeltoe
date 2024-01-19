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
using Steeltoe.Connectors.CloudFoundry;
using Steeltoe.Connectors.Services;
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
    public static IServiceCollection AddDiscoveryClient(this IServiceCollection services, IConfiguration configuration)
    {
        return AddDiscoveryClient(services, configuration, null);
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
    public static IServiceCollection AddDiscoveryClient(this IServiceCollection services, IConfiguration configuration, string serviceName)
    {
        ArgumentGuard.NotNull(services);
        ArgumentGuard.NotNull(configuration);

        Action<DiscoveryClientBuilder> builderAction = null;

        IServiceInfo info = string.IsNullOrEmpty(serviceName)
            ? GetSingleDiscoveryServiceInfo(configuration)
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
            builderAction = builder => builder.Extensions.Add(implementations[0]);
        }
        else if (implementations.Count > 1)
        {
            // if none configured, that's ok because AddServiceDiscovery has a plan
            IDiscoveryClientExtension[] configured = implementations.Where(client => client.IsConfigured(configuration, info)).ToArray();

            if (configured.Length == 1)
            {
                builderAction = builder => builder.Extensions.Add(configured.Single());
            }
            else if (configured.Length > 1)
            {
                throw new AmbiguousMatchException(
                    "Multiple IDiscoveryClient implementations have been added and configured! This is not supported, please only configure a single client type.");
            }
        }

        return AddServiceDiscovery(services, configuration, builderAction);
    }

    /// <summary>
    /// Adds service discovery to your application. If <paramref name="builderAction" /> is not provided, a <see cref="NoOpDiscoveryClient" /> will be
    /// configured.
    /// </summary>
    /// <param name="services">
    /// <see cref="IServiceCollection" /> to configure.
    /// </param>
    /// <param name="configuration">
    /// Application configuration.
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
    public static IServiceCollection AddServiceDiscovery(this IServiceCollection services, IConfiguration configuration,
        Action<DiscoveryClientBuilder> builderAction = null)
    {
        ArgumentGuard.NotNull(services);
        ArgumentGuard.NotNull(configuration);

        builderAction ??= builder => builder.Extensions.Add(new NoOpDiscoveryClientExtension());

        services.RegisterDefaultApplicationInstanceInfo();
        ApplyDiscoveryOptions(services, configuration, builderAction);

        services.TryAddTransient<DiscoveryHttpMessageHandler>();
        services.AddHttpClient("DiscoveryRandom").AddRandomLoadBalancer();
        services.AddHttpClient("DiscoveryRoundRobin").AddRoundRobinLoadBalancer();
        services.AddSingleton<IHostedService, DiscoveryClientService>();
        return services;
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
        ArgumentGuard.NotNull(configuration);

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
    public static IServiceInfo GetSingleDiscoveryServiceInfo(IConfiguration configuration)
    {
        ArgumentGuard.NotNull(configuration);

        // Note: Could be other discovery type services in future
        EurekaServiceInfo[] eurekaInfos = configuration.GetServiceInfos<EurekaServiceInfo>().ToArray();

        if (eurekaInfos.Any())
        {
            if (eurekaInfos.Length != 1)
            {
                throw new ConnectorException("Multiple discovery service types bound to application.");
            }

            return eurekaInfos[0];
        }

        return null;
    }

    private static void ApplyDiscoveryOptions(IServiceCollection services, IConfiguration configuration, Action<DiscoveryClientBuilder> builderAction)
    {
        var builder = new DiscoveryClientBuilder();

        builderAction?.Invoke(builder);

        if (builder.Extensions.Count > 1)
        {
            IDiscoveryClientExtension[] configured = builder.Extensions
                .Where(extension => extension.IsConfigured(configuration, GetSingleDiscoveryServiceInfo(configuration))).ToArray();

            if (!configured.Any() || configured.Length > 1)
            {
                throw new AmbiguousMatchException(
                    "Multiple IDiscoveryClient implementations have been registered and 0 or more than 1 have been configured! This is not supported, please only use a single client type.");
            }

            builder.Extensions = configured.ToList();
        }

        foreach (IDiscoveryClientExtension ext in builder.Extensions)
        {
            ext.ApplyServices(services);
        }
    }

    private static bool IsRecognizedDiscoveryService(IServiceInfo info)
    {
        return info is EurekaServiceInfo;
    }
}
