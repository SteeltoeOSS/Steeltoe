// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Connectors.CosmosDb.DynamicTypeAccess;
using Steeltoe.Connectors.DynamicTypeAccess;

namespace Steeltoe.Connectors.CosmosDb;

public static class CosmosDbServiceCollectionExtensions
{
    /// <summary>
    /// Registers a <see cref="ConnectorFactory{TOptions,TConnection}" /> (with type parameters <see cref="CosmosDbOptions" /> and
    /// Microsoft.Azure.Cosmos.CosmosClient) to connect to a CosmosDB database.
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
    public static IServiceCollection AddCosmosDb(this IServiceCollection services, IConfiguration configuration)
    {
        return AddCosmosDb(services, configuration, CosmosDbPackageResolver.Default);
    }

    /// <summary>
    /// Registers a <see cref="ConnectorFactory{TOptions,TConnection}" /> (with type parameters <see cref="CosmosDbOptions" /> and
    /// Microsoft.Azure.Cosmos.CosmosClient) to connect to a CosmosDB database.
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
    public static IServiceCollection AddCosmosDb(this IServiceCollection services, IConfiguration configuration, Action<ConnectorAddOptionsBuilder>? addAction)
    {
        return AddCosmosDb(services, configuration, CosmosDbPackageResolver.Default, addAction);
    }

    private static IServiceCollection AddCosmosDb(this IServiceCollection services, IConfiguration configuration, CosmosDbPackageResolver packageResolver,
        Action<ConnectorAddOptionsBuilder>? addAction = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(packageResolver);

        if (!ConnectorFactoryShim<CosmosDbOptions>.IsRegistered(packageResolver.CosmosClientClass.Type, services))
        {
            var optionsBuilder = new ConnectorAddOptionsBuilder(
                (serviceProvider, serviceBindingName) => CreateCosmosClient(serviceProvider, serviceBindingName, packageResolver),
                (serviceProvider, serviceBindingName) => CreateHealthContributor(serviceProvider, serviceBindingName, packageResolver))
            {
                // From https://learn.microsoft.com/en-us/dotnet/api/microsoft.azure.cosmos.cosmosclient:
                //   "It's recommended to maintain a single instance of CosmosClient per lifetime of the application
                //   which enables efficient connection management and performance."
                CacheConnection = true,
                EnableHealthChecks = services.All(descriptor => descriptor.ServiceType != typeof(HealthCheckService))
            };

            addAction?.Invoke(optionsBuilder);

            IReadOnlySet<string> optionNames = ConnectorOptionsBinder.RegisterNamedOptions<CosmosDbOptions>(services, configuration, "cosmosdb",
                optionsBuilder.EnableHealthChecks ? optionsBuilder.CreateHealthContributor : null);

            ConnectorFactoryShim<CosmosDbOptions>.Register(packageResolver.CosmosClientClass.Type, services, optionNames, optionsBuilder.CreateConnection,
                optionsBuilder.CacheConnection);
        }

        return services;
    }

    private static CosmosDbHealthContributor CreateHealthContributor(IServiceProvider serviceProvider, string serviceBindingName,
        CosmosDbPackageResolver packageResolver)
    {
        var logger = serviceProvider.GetRequiredService<ILogger<CosmosDbHealthContributor>>();

        return new CosmosDbHealthContributor(serviceBindingName, serviceProvider, packageResolver, logger)
        {
            Timeout = TimeSpan.FromSeconds(5)
        };
    }

    private static IDisposable CreateCosmosClient(IServiceProvider serviceProvider, string serviceBindingName, CosmosDbPackageResolver packageResolver)
    {
        var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<CosmosDbOptions>>();
        CosmosDbOptions options = optionsMonitor.Get(serviceBindingName);

        var cosmosClientShim = CosmosClientShim.CreateInstance(packageResolver, options.ConnectionString);
        return cosmosClientShim.Instance;
    }
}
