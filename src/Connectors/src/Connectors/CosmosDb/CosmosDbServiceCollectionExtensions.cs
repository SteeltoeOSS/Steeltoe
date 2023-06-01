// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Data.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Connectors.CosmosDb.DynamicTypeAccess;
using Steeltoe.Connectors.DynamicTypeAccess;

namespace Steeltoe.Connectors.CosmosDb;

public static class CosmosDbServiceCollectionExtensions
{
    public static IServiceCollection AddCosmosDb(this IServiceCollection services, IConfiguration configuration)
    {
        return AddCosmosDb(services, configuration, null);
    }

    public static IServiceCollection AddCosmosDb(this IServiceCollection services, IConfiguration configuration, Action<ConnectorSetupOptions>? setupAction)
    {
        return AddCosmosDb(services, configuration, CosmosDbPackageResolver.Default, setupAction);
    }

    private static IServiceCollection AddCosmosDb(this IServiceCollection services, IConfiguration configuration, CosmosDbPackageResolver packageResolver,
        Action<ConnectorSetupOptions>? setupAction)
    {
        ArgumentGuard.NotNull(services);
        ArgumentGuard.NotNull(configuration);
        ArgumentGuard.NotNull(packageResolver);

        var setupOptions = new ConnectorSetupOptions();
        setupAction?.Invoke(setupOptions);

        ConnectorCreateHealthContributor? createHealthContributor = setupOptions.EnableHealthChecks
            ? (serviceProvider, serviceBindingName) => setupOptions.CreateHealthContributor != null
                ? setupOptions.CreateHealthContributor(serviceProvider, serviceBindingName)
                : CreateHealthContributor(serviceProvider, serviceBindingName, packageResolver)
            : null;

        IReadOnlySet<string> optionNames =
            ConnectorOptionsBinder.RegisterNamedOptions<CosmosDbOptions>(services, configuration, "cosmosdb", createHealthContributor);

        ConnectorCreateConnection createConnection = (serviceProvider, serviceBindingName) => setupOptions.CreateConnection != null
            ? setupOptions.CreateConnection(serviceProvider, serviceBindingName)
            : CreateCosmosClient(serviceProvider, serviceBindingName, packageResolver);

        ConnectorFactoryShim<CosmosDbOptions>.Register(packageResolver.CosmosClientClass.Type, services, optionNames, createConnection, true);

        return services;
    }

    private static IHealthContributor CreateHealthContributor(IServiceProvider serviceProvider, string serviceBindingName,
        CosmosDbPackageResolver packageResolver)
    {
        ConnectorFactoryShim<CosmosDbOptions> connectorFactoryShim =
            ConnectorFactoryShim<CosmosDbOptions>.FromServiceProvider(serviceProvider, packageResolver.CosmosClientClass.Type);

        ConnectorShim<CosmosDbOptions> connectorShim = connectorFactoryShim.Get(serviceBindingName);

        object cosmosClient = connectorShim.GetConnection();
        string hostName = GetHostNameFromConnectionString(connectorShim.Options.ConnectionString);
        var logger = serviceProvider.GetRequiredService<ILogger<CosmosDbHealthContributor>>();

        return new CosmosDbHealthContributor(cosmosClient, $"CosmosDB-{serviceBindingName}", hostName, logger);
    }

    private static string GetHostNameFromConnectionString(string? connectionString)
    {
        if (connectionString == null)
        {
            return string.Empty;
        }

        var builder = new DbConnectionStringBuilder
        {
            ConnectionString = connectionString
        };

        var uri = new Uri((string)builder["AccountEndpoint"]);
        return uri.Host;
    }

    private static IDisposable CreateCosmosClient(IServiceProvider serviceProvider, string serviceBindingName, CosmosDbPackageResolver packageResolver)
    {
        var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<CosmosDbOptions>>();
        CosmosDbOptions options = optionsMonitor.Get(serviceBindingName);

        var cosmosClientShim = CosmosClientShim.CreateInstance(packageResolver, options.ConnectionString!);
        return cosmosClientShim.Instance;
    }
}
