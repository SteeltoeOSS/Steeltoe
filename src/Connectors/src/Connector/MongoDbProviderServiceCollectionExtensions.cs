// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Steeltoe.Common;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Common.Reflection;
using Steeltoe.Connector.Services;

namespace Steeltoe.Connector.MongoDb;

public static class MongoDbProviderServiceCollectionExtensions
{
    /// <summary>
    /// Add MongoDb to a ServiceCollection.
    /// </summary>
    /// <param name="services">
    /// Service collection to add to.
    /// </param>
    /// <param name="configuration">
    /// App configuration.
    /// </param>
    /// <param name="contextLifetime">
    /// Lifetime of the service to inject.
    /// </param>
    /// <param name="addSteeltoeHealthChecks">
    /// Add Steeltoe healthChecks.
    /// </param>
    /// <returns>
    /// IServiceCollection for chaining.
    /// </returns>
    public static IServiceCollection AddMongoClient(this IServiceCollection services, IConfiguration configuration,
        ServiceLifetime contextLifetime = ServiceLifetime.Singleton, bool addSteeltoeHealthChecks = false)
    {
        ArgumentGuard.NotNull(services);
        ArgumentGuard.NotNull(configuration);

        var info = configuration.GetSingletonServiceInfo<MongoDbServiceInfo>();

        DoAdd(services, info, configuration, contextLifetime, addSteeltoeHealthChecks);
        return services;
    }

    /// <summary>
    /// Add MongoDb to a ServiceCollection.
    /// </summary>
    /// <param name="services">
    /// Service collection to add to.
    /// </param>
    /// <param name="configuration">
    /// App configuration.
    /// </param>
    /// <param name="serviceName">
    /// cloud foundry service name binding.
    /// </param>
    /// <param name="contextLifetime">
    /// Lifetime of the service to inject.
    /// </param>
    /// <param name="addSteeltoeHealthChecks">
    /// Add Steeltoe healthChecks.
    /// </param>
    /// <returns>
    /// IServiceCollection for chaining.
    /// </returns>
    public static IServiceCollection AddMongoClient(this IServiceCollection services, IConfiguration configuration, string serviceName,
        ServiceLifetime contextLifetime = ServiceLifetime.Singleton, bool addSteeltoeHealthChecks = false)
    {
        ArgumentGuard.NotNull(services);
        ArgumentGuard.NotNullOrEmpty(serviceName);
        ArgumentGuard.NotNull(configuration);

        var info = configuration.GetRequiredServiceInfo<MongoDbServiceInfo>(serviceName);

        DoAdd(services, info, configuration, contextLifetime, addSteeltoeHealthChecks);
        return services;
    }

    private static void DoAdd(IServiceCollection services, MongoDbServiceInfo info, IConfiguration configuration, ServiceLifetime contextLifetime,
        bool addSteeltoeHealthChecks = false)
    {
        Type mongoClient = MongoDbTypeLocator.MongoClient;
        var options = new MongoDbConnectorOptions(configuration);
        var clientFactory = new MongoDbConnectorFactory(info, options, mongoClient);
        services.Add(new ServiceDescriptor(MongoDbTypeLocator.MongoClientInterface, clientFactory.Create, contextLifetime));
        services.Add(new ServiceDescriptor(mongoClient, clientFactory.Create, contextLifetime));

        if (!services.Any(s => s.ServiceType == typeof(HealthCheckService)) || addSteeltoeHealthChecks)
        {
            services.Add(new ServiceDescriptor(typeof(IHealthContributor),
                ctx => new MongoDbHealthContributor(clientFactory, ctx.GetService<ILogger<MongoDbHealthContributor>>()), ServiceLifetime.Singleton));
        }

        Type mongoInfo = ReflectionHelpers.FindType(MongoDbTypeLocator.Assemblies, MongoDbTypeLocator.MongoConnectionInfo);
        var urlFactory = new MongoDbConnectorFactory(info, options, mongoInfo);
        services.Add(new ServiceDescriptor(mongoInfo, urlFactory.Create, contextLifetime));
    }
}
