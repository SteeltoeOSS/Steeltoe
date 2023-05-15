// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Data.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Steeltoe.Common;
using Steeltoe.Common.HealthChecks;

namespace Steeltoe.Connectors.CosmosDb;

public delegate object CreateCosmosClient(CosmosDbOptions options, string serviceBindingName);

public static class CosmosDbWebApplicationBuilderExtensions
{
    private static readonly Type ConnectionType = CosmosDbTypeLocator.CosmosClient;

    public static WebApplicationBuilder AddCosmosDb(this WebApplicationBuilder builder)
    {
        // CosmosClient has no constructor with only connection string parameter, so pass null for CosmosClientOptions parameter.
        CreateCosmosClient createCosmosClient = (options, _) => Activator.CreateInstance(ConnectionType, options.ConnectionString, null);

        return AddCosmosDb(builder, createCosmosClient);
    }

    public static WebApplicationBuilder AddCosmosDb(this WebApplicationBuilder builder, CreateCosmosClient createCosmosClient)
    {
        ArgumentGuard.NotNull(builder);
        ArgumentGuard.NotNull(createCosmosClient);

        var connectionStringPostProcessor = new CosmosDbConnectionStringPostProcessor();

        Func<CosmosDbOptions, string, object> createConnection = (options, serviceBindingName) => createCosmosClient(options, serviceBindingName);

        BaseWebApplicationBuilderExtensions.RegisterConfigurationSource(builder.Configuration, connectionStringPostProcessor);
        BaseWebApplicationBuilderExtensions.RegisterNamedOptions<CosmosDbOptions>(builder, "cosmosdb", CreateHealthContributor);
        BaseWebApplicationBuilderExtensions.RegisterConnectionFactory(builder.Services, ConnectionType, true, createConnection);

        return builder;
    }

    private static IHealthContributor CreateHealthContributor(IServiceProvider serviceProvider, string bindingName)
    {
        string connectionString = ConnectionFactoryInvoker.GetConnectionString<CosmosDbOptions>(serviceProvider, bindingName, ConnectionType);
        string serviceName = $"CosmosDB-{bindingName}";
        string hostName = GetHostNameFromConnectionString(connectionString);
        object cosmosClient = ConnectionFactoryInvoker.GetConnection<CosmosDbOptions>(serviceProvider, bindingName, ConnectionType);
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
