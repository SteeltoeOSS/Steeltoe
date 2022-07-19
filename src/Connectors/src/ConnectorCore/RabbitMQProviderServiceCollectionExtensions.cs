// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Connector.Services;
using System;
using System.Linq;

namespace Steeltoe.Connector.RabbitMQ;

public static class RabbitMQProviderServiceCollectionExtensions
{
    /// <summary>
    /// Add RabbitMQ and its IHealthContributor to a ServiceCollection.
    /// </summary>
    /// <param name="services">Service collection to add to.</param>
    /// <param name="config">App configuration.</param>
    /// <param name="contextLifetime">Lifetime of the service to inject.</param>
    /// <param name="addSteeltoeHealthChecks">Add Steeltoe healthChecks.</param>
    /// <returns>IServiceCollection for chaining.</returns>
    /// <remarks>RabbitMQ.Client.ConnectionFactory is retrievable as both ConnectionFactory and IConnectionFactory.</remarks>
    public static IServiceCollection AddRabbitMQConnection(this IServiceCollection services, IConfiguration config, ServiceLifetime contextLifetime = ServiceLifetime.Scoped, bool addSteeltoeHealthChecks = false)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (config == null)
        {
            throw new ArgumentNullException(nameof(config));
        }

        var info = config.GetSingletonServiceInfo<RabbitMQServiceInfo>();
        DoAdd(services, info, config, contextLifetime, addSteeltoeHealthChecks);

        return services;
    }

    /// <summary>
    /// Add RabbitMQ and its IHealthContributor to a ServiceCollection.
    /// </summary>
    /// <param name="services">Service collection to add to.</param>
    /// <param name="config">App configuration.</param>
    /// <param name="serviceName">cloud foundry service name binding.</param>
    /// <param name="contextLifetime">Lifetime of the service to inject.</param>
    /// <param name="addSteeltoeHealthChecks">Add Steeltoe healthChecks.</param>
    /// <returns>IServiceCollection for chaining.</returns>
    /// <remarks>RabbitMQ.Client.ConnectionFactory is retrievable as both ConnectionFactory and IConnectionFactory.</remarks>
    public static IServiceCollection AddRabbitMQConnection(this IServiceCollection services, IConfiguration config, string serviceName, ServiceLifetime contextLifetime = ServiceLifetime.Scoped, bool addSteeltoeHealthChecks = false)
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

        var info = config.GetRequiredServiceInfo<RabbitMQServiceInfo>(serviceName);

        DoAdd(services, info, config, contextLifetime, addSteeltoeHealthChecks);
        return services;
    }

    private static void DoAdd(IServiceCollection services, RabbitMQServiceInfo info, IConfiguration config, ServiceLifetime contextLifetime, bool addSteeltoeHealthChecks)
    {
        var rabbitMQInterfaceType = RabbitMQTypeLocator.ConnectionFactoryInterface;
        var rabbitMQImplementationType = RabbitMQTypeLocator.ConnectionFactory;

        var rabbitMQConfig = new RabbitMQProviderConnectorOptions(config);
        var factory = new RabbitMQProviderConnectorFactory(info, rabbitMQConfig, rabbitMQImplementationType);
        services.Add(new ServiceDescriptor(rabbitMQInterfaceType, factory.Create, contextLifetime));
        services.Add(new ServiceDescriptor(rabbitMQImplementationType, factory.Create, contextLifetime));
        if (!services.Any(s => s.ServiceType == typeof(HealthCheckService)) || addSteeltoeHealthChecks)
        {
            services.Add(new ServiceDescriptor(typeof(IHealthContributor), ctx => new RabbitMQHealthContributor(factory, ctx.GetService<ILogger<RabbitMQHealthContributor>>()), ServiceLifetime.Singleton));
        }
    }
}
