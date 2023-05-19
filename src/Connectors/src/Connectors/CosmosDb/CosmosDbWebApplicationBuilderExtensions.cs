// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Data.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Steeltoe.Common;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Connectors.CosmosDb.RuntimeTypeAccess;

namespace Steeltoe.Connectors.CosmosDb;

public delegate IDisposable CreateCosmosClient(CosmosDbOptions options, string serviceBindingName);

public static class CosmosDbWebApplicationBuilderExtensions
{
    public static WebApplicationBuilder AddCosmosDb(this WebApplicationBuilder builder, CreateCosmosClient? createCosmosClient = null)
    {
        return AddCosmosDb(builder, new CosmosDbPackageResolver(), createCosmosClient);
    }

    private static WebApplicationBuilder AddCosmosDb(this WebApplicationBuilder builder, CosmosDbPackageResolver packageResolver,
        CreateCosmosClient? createCosmosClient)
    {
        ArgumentGuard.NotNull(builder);
        ArgumentGuard.NotNull(packageResolver);

        var connectionStringPostProcessor = new CosmosDbConnectionStringPostProcessor();

        Func<CosmosDbOptions, string, object> createConnection = (options, serviceBindingName) => createCosmosClient != null
            ? createCosmosClient(options, serviceBindingName)
            : CosmosClientShim.CreateInstance(packageResolver, options.ConnectionString!).Instance;

        BaseWebApplicationBuilderExtensions.RegisterConfigurationSource(builder.Configuration, connectionStringPostProcessor);

        BaseWebApplicationBuilderExtensions.RegisterNamedOptions<CosmosDbOptions>(builder, "cosmosdb",
            (serviceProvider, bindingName) => CreateHealthContributor(serviceProvider, bindingName, packageResolver));

        BaseWebApplicationBuilderExtensions.RegisterConnectorFactory(builder.Services, packageResolver.CosmosClientClass.Type, true, createConnection);

        return builder;
    }

    private static IHealthContributor CreateHealthContributor(IServiceProvider serviceProvider, string bindingName, CosmosDbPackageResolver packageResolver)
    {
        string connectionString =
            ConnectorFactoryInvoker.GetConnectionString<CosmosDbOptions>(serviceProvider, bindingName, packageResolver.CosmosClientClass.Type);

        string serviceName = $"CosmosDB-{bindingName}";
        string hostName = GetHostNameFromConnectionString(connectionString);
        object cosmosClient = ConnectorFactoryInvoker.GetConnection<CosmosDbOptions>(serviceProvider, bindingName, packageResolver.CosmosClientClass.Type);
        var logger = serviceProvider.GetRequiredService<ILogger<CosmosDbHealthContributor>>();

        return new CosmosDbHealthContributor(cosmosClient, serviceName, hostName, logger);
    }

    private static string GetHostNameFromConnectionString(string connectionString)
    {
        var builder = new DbConnectionStringBuilder
        {
            ConnectionString = connectionString
        };

        var uri = new Uri((string)builder["AccountEndpoint"]);
        return uri.Host;
    }
}
