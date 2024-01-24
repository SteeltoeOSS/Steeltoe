// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

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
    /// The <see cref="IServiceCollection" /> to add services to.
    /// </param>
    /// <param name="configuration">
    /// The <see cref="IConfiguration" /> to read application settings from.
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
    /// The <see cref="IServiceCollection" /> to add services to.
    /// </param>
    /// <param name="configuration">
    /// The <see cref="IConfiguration" /> to read application settings from.
    /// </param>
    /// <param name="serviceName">
    /// The name of the service binding to use.
    /// </param>
    public static IServiceCollection AddDiscoveryClient(this IServiceCollection services, IConfiguration configuration, string? serviceName)
    {
        ArgumentGuard.NotNull(services);
        ArgumentGuard.NotNull(configuration);

        Action<DiscoveryClientBuilder>? builderAction = null;

        IServiceInfo? info = string.IsNullOrEmpty(serviceName)
            ? GetSingleDiscoveryServiceInfo(configuration)
            : GetNamedDiscoveryServiceInfo(configuration, serviceName);

        // iterate assemblies for implementations of IDiscoveryClientExtension
        var implementations = new List<IDiscoveryClientExtension>();
        IEnumerable<Type> extensions = ReflectionHelpers.FindInterfacedTypesFromAssemblyAttribute<DiscoveryClientAssemblyAttribute>();

        foreach (Type extension in extensions)
        {
            implementations.Add((IDiscoveryClientExtension)Activator.CreateInstance(extension)!);
        }

        if (implementations.Count == 1)
        {
            builderAction = builder => builder.Extensions.Add(implementations[0]);
        }
        else if (implementations.Count > 1)
        {
            // If none configured, that's ok because AddServiceDiscovery will add a no-op discovery client.
            IDiscoveryClientExtension[] configured = implementations.Where(client => client.IsConfigured(configuration, info)).ToArray();

            if (configured.Length == 1)
            {
                builderAction = builder => builder.Extensions.Add(configured[0]);
            }
            else if (configured.Length > 1)
            {
                throw new InvalidOperationException(
                    "Multiple IDiscoveryClient implementations have been registered and configured. This is not supported, please only use a single client type.");
            }
        }

        return AddServiceDiscovery(services, configuration, builderAction);
    }

    /// <summary>
    /// Adds service discovery to your application. If <paramref name="builderAction" /> is not provided, a <see cref="NoOpDiscoveryClient" /> will be
    /// configured.
    /// </summary>
    /// <param name="services">
    /// The <see cref="IServiceCollection" /> to add services to.
    /// </param>
    /// <param name="configuration">
    /// The <see cref="IConfiguration" /> to read application settings from.
    /// </param>
    /// <param name="builderAction">
    /// Used to activate the desired <see cref="IDiscoveryClient" /> implementation.
    /// </param>
    /// <remarks>
    /// Also configures named HttpClients "DiscoveryRandom" and "DiscoveryRoundRobin" for automatic injection.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown if multiple <see cref="IDiscoveryClient" /> implementations are configured.
    /// </exception>
    /// <exception cref="ConnectorException">
    /// Thrown when no service info with the expected name or type was found, or when multiple service infos were found.
    /// </exception>
    public static IServiceCollection AddServiceDiscovery(this IServiceCollection services, IConfiguration configuration,
        Action<DiscoveryClientBuilder>? builderAction)
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
    /// Retrieves a single, named <see cref="IServiceInfo" /> that is expected to work with service discovery.
    /// </summary>
    /// <param name="configuration">
    /// The <see cref="IConfiguration" /> to read application settings from.
    /// </param>
    /// <param name="serviceName">
    /// The name of the service binding to use.
    /// </param>
    /// <exception cref="ConnectorException">
    /// Thrown when no service info with the expected name or type was found.
    /// </exception>
    public static IServiceInfo GetNamedDiscoveryServiceInfo(IConfiguration configuration, string serviceName)
    {
        ArgumentGuard.NotNull(configuration);

        IServiceInfo? info = configuration.GetServiceInfo(serviceName);

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
    /// Retrieves a single <see cref="IServiceInfo" /> that is expected to work with service discovery.
    /// </summary>
    /// <param name="configuration">
    /// The <see cref="IConfiguration" /> to read application settings from.
    /// </param>
    /// <exception cref="ConnectorException">
    /// Thrown when multiple service infos were found.
    /// </exception>
    public static IServiceInfo? GetSingleDiscoveryServiceInfo(IConfiguration configuration)
    {
        ArgumentGuard.NotNull(configuration);

        // Note: Could be other discovery type services in future
        EurekaServiceInfo[] eurekaInfos = configuration.GetServiceInfos<EurekaServiceInfo>().ToArray();

        if (eurekaInfos.Length > 0)
        {
            if (eurekaInfos.Length != 1)
            {
                throw new ConnectorException("Multiple discovery service types bound to application.");
            }

            return eurekaInfos[0];
        }

        return null;
    }

    private static void ApplyDiscoveryOptions(IServiceCollection services, IConfiguration configuration, Action<DiscoveryClientBuilder>? builderAction)
    {
        var builder = new DiscoveryClientBuilder();

        builderAction?.Invoke(builder);

        if (builder.Extensions.Count > 1)
        {
            IDiscoveryClientExtension[] configured = builder.Extensions
                .Where(extension => extension.IsConfigured(configuration, GetSingleDiscoveryServiceInfo(configuration))).ToArray();

            if (configured.Length != 1)
            {
                throw new InvalidOperationException(
                    "None or multiple IDiscoveryClient implementations have been registered and configured. This is not supported, please only use a single client type.");
            }

            builder.Extensions.Clear();
            builder.Extensions.Add(configured[0]);
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
