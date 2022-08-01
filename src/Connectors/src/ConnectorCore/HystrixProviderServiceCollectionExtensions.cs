// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Connector.RabbitMQ;
using Steeltoe.Connector.Services;

namespace Steeltoe.Connector.Hystrix;

public static class HystrixProviderServiceCollectionExtensions
{
    /// <summary>
    /// Adds HystrixConnectionFactory to your ServiceCollection.
    /// </summary>
    /// <param name="services">Your Service Collection.</param>
    /// <param name="config">Application Configuration.</param>
    /// <param name="contextLifetime">Lifetime of the service to inject.</param>
    /// <returns>IServiceCollection for chaining.</returns>
    public static IServiceCollection AddHystrixConnection(this IServiceCollection services, IConfiguration config, ServiceLifetime contextLifetime = ServiceLifetime.Singleton)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (config == null)
        {
            throw new ArgumentNullException(nameof(config));
        }

        var info = config.GetSingletonServiceInfo<HystrixRabbitMQServiceInfo>();

        DoAdd(services, info, config, contextLifetime);
        return services;
    }

    /// <summary>
    /// Adds HystrixConnectionFactory to your ServiceCollection.
    /// </summary>
    /// <param name="services">Your Service Collection.</param>
    /// <param name="config">Application Configuration.</param>
    /// <param name="serviceName">Cloud Foundry service name binding.</param>
    /// <param name="contextLifetime">Lifetime of the service to inject.</param>
    /// <returns>IServiceCollection for chaining.</returns>
    public static IServiceCollection AddHystrixConnection(this IServiceCollection services, IConfiguration config, string serviceName, ServiceLifetime contextLifetime = ServiceLifetime.Singleton)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (string.IsNullOrEmpty(serviceName))
        {
            throw new ArgumentNullException(nameof(serviceName));
        }

        if (config == null)
        {
            throw new ArgumentNullException(nameof(config));
        }

        var info = config.GetRequiredServiceInfo<HystrixRabbitMQServiceInfo>(serviceName);

        DoAdd(services, info, config, contextLifetime);
        return services;
    }

    private static void DoAdd(IServiceCollection services, HystrixRabbitMQServiceInfo info, IConfiguration config, ServiceLifetime contextLifetime)
    {
        var rabbitFactory = RabbitMQTypeLocator.ConnectionFactory;
        var hystrixConfig = new HystrixProviderConnectorOptions(config);
        var factory = new HystrixProviderConnectorFactory(info, hystrixConfig, rabbitFactory);
        services.Add(new ServiceDescriptor(typeof(HystrixConnectionFactory), factory.Create, contextLifetime));
    }
}
