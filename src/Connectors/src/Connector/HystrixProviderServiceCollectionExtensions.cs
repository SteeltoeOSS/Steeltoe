// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Common;
using Steeltoe.Connector.CosmosDb;
using Steeltoe.Connector.Services;

namespace Steeltoe.Connector.Hystrix;

public static class HystrixProviderServiceCollectionExtensions
{
    /// <summary>
    /// Adds HystrixConnectionFactory to your ServiceCollection.
    /// </summary>
    /// <param name="services">
    /// Your Service Collection.
    /// </param>
    /// <param name="configuration">
    /// Application Configuration.
    /// </param>
    /// <param name="contextLifetime">
    /// Lifetime of the service to inject.
    /// </param>
    /// <returns>
    /// IServiceCollection for chaining.
    /// </returns>
    public static IServiceCollection AddHystrixConnection(this IServiceCollection services, IConfiguration configuration,
        ServiceLifetime contextLifetime = ServiceLifetime.Singleton)
    {
        ArgumentGuard.NotNull(services);
        ArgumentGuard.NotNull(configuration);

        var info = configuration.GetSingletonServiceInfo<HystrixRabbitMQServiceInfo>();

        DoAdd(services, info, configuration, contextLifetime);
        return services;
    }

    /// <summary>
    /// Adds HystrixConnectionFactory to your ServiceCollection.
    /// </summary>
    /// <param name="services">
    /// Your Service Collection.
    /// </param>
    /// <param name="configuration">
    /// Application Configuration.
    /// </param>
    /// <param name="serviceName">
    /// Cloud Foundry service name binding.
    /// </param>
    /// <param name="contextLifetime">
    /// Lifetime of the service to inject.
    /// </param>
    /// <returns>
    /// IServiceCollection for chaining.
    /// </returns>
    public static IServiceCollection AddHystrixConnection(this IServiceCollection services, IConfiguration configuration, string serviceName,
        ServiceLifetime contextLifetime = ServiceLifetime.Singleton)
    {
        ArgumentGuard.NotNull(services);
        ArgumentGuard.NotNullOrEmpty(serviceName);
        ArgumentGuard.NotNull(configuration);

        var info = configuration.GetRequiredServiceInfo<HystrixRabbitMQServiceInfo>(serviceName);

        DoAdd(services, info, configuration, contextLifetime);
        return services;
    }

    private static void DoAdd(IServiceCollection services, HystrixRabbitMQServiceInfo info, IConfiguration configuration, ServiceLifetime contextLifetime)
    {
        Type rabbitFactory = RabbitMQTypeLocator.ConnectionFactory;
        var options = new HystrixProviderConnectorOptions(configuration);
        var factory = new HystrixProviderConnectorFactory(info, options, rabbitFactory);
        services.Add(new ServiceDescriptor(typeof(HystrixConnectionFactory), factory.Create, contextLifetime));
    }
}
