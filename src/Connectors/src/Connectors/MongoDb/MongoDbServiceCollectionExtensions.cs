// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Connectors.DynamicTypeAccess;
using Steeltoe.Connectors.MongoDb.DynamicTypeAccess;

namespace Steeltoe.Connectors.MongoDb;

public static class MongoDbServiceCollectionExtensions
{
    /// <summary>
    /// Registers a <see cref="ConnectorFactory{TOptions,TConnection}" /> (with type parameters <see cref="MongoDbOptions" /> and
    /// MongoDB.Driver.IMongoClient) to connect to a MongoDB database.
    /// </summary>
    /// <param name="services">
    /// The <see cref="IServiceCollection" /> to add services to.
    /// </param>
    /// <param name="configuration">
    /// The <see cref="IConfiguration" /> to read application settings from.
    /// </param>
    /// <returns>
    /// The <see cref="IServiceCollection" /> so that additional calls can be chained.
    /// </returns>
    public static IServiceCollection AddMongoDb(this IServiceCollection services, IConfiguration configuration)
    {
        return AddMongoDb(services, configuration, MongoDbPackageResolver.Default);
    }

    /// <summary>
    /// Registers a <see cref="ConnectorFactory{TOptions,TConnection}" /> (with type parameters <see cref="MongoDbOptions" /> and
    /// MongoDB.Driver.IMongoClient) to connect to a MongoDB database.
    /// </summary>
    /// <param name="services">
    /// The <see cref="IServiceCollection" /> to add services to.
    /// </param>
    /// <param name="configuration">
    /// The <see cref="IConfiguration" /> to read application settings from.
    /// </param>
    /// <param name="addAction">
    /// An optional delegate to configure this connector.
    /// </param>
    /// <returns>
    /// The <see cref="IServiceCollection" /> so that additional calls can be chained.
    /// </returns>
    public static IServiceCollection AddMongoDb(this IServiceCollection services, IConfiguration configuration, Action<ConnectorAddOptionsBuilder>? addAction)
    {
        return AddMongoDb(services, configuration, MongoDbPackageResolver.Default, addAction);
    }

    private static IServiceCollection AddMongoDb(this IServiceCollection services, IConfiguration configuration, MongoDbPackageResolver packageResolver,
        Action<ConnectorAddOptionsBuilder>? addAction = null)
    {
        ArgumentGuard.NotNull(services);
        ArgumentGuard.NotNull(configuration);
        ArgumentGuard.NotNull(packageResolver);

        if (!ConnectorFactoryShim<MongoDbOptions>.IsRegistered(packageResolver.MongoClientInterface.Type, services))
        {
            var optionsBuilder = new ConnectorAddOptionsBuilder(
                (serviceProvider, serviceBindingName) => CreateMongoClient(serviceProvider, serviceBindingName, packageResolver),
                (serviceProvider, serviceBindingName) => CreateHealthContributor(serviceProvider, serviceBindingName, packageResolver))
            {
                CacheConnection = false,
                EnableHealthChecks = services.All(descriptor => descriptor.ServiceType != typeof(HealthCheckService))
            };

            addAction?.Invoke(optionsBuilder);

            IReadOnlySet<string> optionNames = ConnectorOptionsBinder.RegisterNamedOptions<MongoDbOptions>(services, configuration, "mongodb",
                optionsBuilder.EnableHealthChecks ? optionsBuilder.CreateHealthContributor : null);

            ConnectorFactoryShim<MongoDbOptions>.Register(packageResolver.MongoClientInterface.Type, services, optionNames, optionsBuilder.CreateConnection,
                optionsBuilder.CacheConnection);
        }

        return services;
    }

    private static IHealthContributor CreateHealthContributor(IServiceProvider serviceProvider, string serviceBindingName,
        MongoDbPackageResolver packageResolver)
    {
        ConnectorFactoryShim<MongoDbOptions> connectorFactoryShim =
            ConnectorFactoryShim<MongoDbOptions>.FromServiceProvider(serviceProvider, packageResolver.MongoClientInterface.Type);

        ConnectorShim<MongoDbOptions> connectorShim = connectorFactoryShim.Get(serviceBindingName);

        object mongoClient = connectorShim.GetConnection();
        string hostName = GetHostNameFromConnectionString(connectorShim.Options.ConnectionString);
        var logger = serviceProvider.GetRequiredService<ILogger<MongoDbHealthContributor>>();

        return new MongoDbHealthContributor(mongoClient, hostName, logger)
        {
            ServiceName = serviceBindingName
        };
    }

    private static string GetHostNameFromConnectionString(string? connectionString)
    {
        if (connectionString == null)
        {
            return string.Empty;
        }

        var builder = new MongoDbConnectionStringBuilder
        {
            ConnectionString = connectionString
        };

        return (string)builder["server"]!;
    }

    private static object CreateMongoClient(IServiceProvider serviceProvider, string serviceBindingName, MongoDbPackageResolver packageResolver)
    {
        var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<MongoDbOptions>>();
        MongoDbOptions options = optionsMonitor.Get(serviceBindingName);

        var mongoClientShim = MongoClientShim.CreateInstance(packageResolver, options.ConnectionString!);
        return mongoClientShim.Instance;
    }
}
