// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
    /// The incoming <paramref name="services" /> so that additional calls can be chained.
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
    /// The incoming <paramref name="services" /> so that additional calls can be chained.
    /// </returns>
    public static IServiceCollection AddMongoDb(this IServiceCollection services, IConfiguration configuration, Action<ConnectorAddOptionsBuilder>? addAction)
    {
        return AddMongoDb(services, configuration, MongoDbPackageResolver.Default, addAction);
    }

    private static IServiceCollection AddMongoDb(this IServiceCollection services, IConfiguration configuration, MongoDbPackageResolver packageResolver,
        Action<ConnectorAddOptionsBuilder>? addAction = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(packageResolver);

        if (!ConnectorFactoryShim<MongoDbOptions>.IsRegistered(packageResolver.MongoClientInterface.Type, services))
        {
            // Holds the final value, which is only known after addAction has run, but is read when a health contributor is created.
            var cacheConnectionBox = new StrongBox<bool>();

            var optionsBuilder = new ConnectorAddOptionsBuilder(
                (serviceProvider, serviceBindingName) => CreateMongoClient(serviceProvider, serviceBindingName, packageResolver),
                (serviceProvider, serviceBindingName) =>
                    CreateHealthContributor(serviceProvider, serviceBindingName, packageResolver, cacheConnectionBox.Value))
            {
                // From https://www.mongodb.com/docs/drivers/csharp/current/connect/mongoclient/#manage-mongoclient-disposables:
                //   "When you use MongoClient, we recommend giving it a singleton lifetime scope. However, this might cause excessive memory usage
                //   and undisposed resources. In v3.0 and later, MongoClient implements IDisposable. Disposing of MongoClient does not dispose
                //   of the cluster and the underlying connections."
                // CacheConnection has defaulted to false since introduced. Changing that breaks existing apps that dispose the returned MongoClient.
                CacheConnection = false,
                EnableHealthChecks = services.All(descriptor => descriptor.ServiceType != typeof(HealthCheckService))
            };

            addAction?.Invoke(optionsBuilder);
            cacheConnectionBox.Value = optionsBuilder.CacheConnection;

            IReadOnlySet<string> optionNames = ConnectorOptionsBinder.RegisterNamedOptions<MongoDbOptions>(services, configuration, "mongodb",
                optionsBuilder.EnableHealthChecks ? optionsBuilder.CreateHealthContributor : null);

            ConnectorFactoryShim<MongoDbOptions>.Register(packageResolver.MongoClientInterface.Type, services, optionNames, optionsBuilder.CreateConnection,
                optionsBuilder.CacheConnection);
        }

        return services;
    }

    private static MongoDbHealthContributor CreateHealthContributor(IServiceProvider serviceProvider, string serviceBindingName,
        MongoDbPackageResolver packageResolver, bool cacheConnection)
    {
        ConnectorFactoryShim<MongoDbOptions> connectorFactoryShim =
            ConnectorFactoryShim<MongoDbOptions>.FromServiceProvider(serviceProvider, packageResolver.MongoClientInterface.Type);

        ConnectorShim<MongoDbOptions> connectorShim = connectorFactoryShim.Get(serviceBindingName);

        Func<MongoClientInterfaceShim> getClient = () => new MongoClientInterfaceShim(packageResolver, connectorShim.GetConnection());
        string? hostName = GetHostNameFromConnectionString(connectorShim.Options.ConnectionString);
        var logger = serviceProvider.GetRequiredService<ILogger<MongoDbHealthContributor>>();

        return new MongoDbHealthContributor(serviceBindingName, getClient, !cacheConnection, hostName, logger);
    }

    private static string? GetHostNameFromConnectionString(string? connectionString)
    {
        if (connectionString == null)
        {
            return null;
        }

        var builder = new MongoDbConnectionStringBuilder
        {
            ConnectionString = connectionString
        };

        return (string?)builder["server"];
    }

    private static object CreateMongoClient(IServiceProvider serviceProvider, string serviceBindingName, MongoDbPackageResolver packageResolver)
    {
        var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<MongoDbOptions>>();
        MongoDbOptions options = optionsMonitor.Get(serviceBindingName);

        var mongoClientShim = MongoClientShim.CreateInstance(packageResolver, options.ConnectionString);
        return mongoClientShim.Instance;
    }
}
