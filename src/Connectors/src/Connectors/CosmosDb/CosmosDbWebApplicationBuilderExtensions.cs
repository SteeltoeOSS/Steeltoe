// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Data.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Steeltoe.Common;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Configuration.CloudFoundry.ServiceBinding;
using Steeltoe.Configuration.CloudFoundry.ServiceBinding.PostProcessors;
using Steeltoe.Connectors.CosmosDb.RuntimeTypeAccess;
using Steeltoe.Connectors.RuntimeTypeAccess;

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

        RegisterPostProcessors(builder.Configuration);

        Func<IServiceProvider, string, IHealthContributor> createHealthContributor = (serviceProvider, serviceBindingName) =>
            CreateHealthContributor(serviceProvider, serviceBindingName, packageResolver);

        IReadOnlySet<string> optionNames =
            BaseWebApplicationBuilderExtensions.RegisterNamedOptions<CosmosDbOptions>(builder, "cosmosdb", createHealthContributor);

        Func<CosmosDbOptions, string, object> createConnection = (options, serviceBindingName) => createCosmosClient != null
            ? createCosmosClient(options, serviceBindingName)
            : CosmosClientShim.CreateInstance(packageResolver, options.ConnectionString!).Instance;

        ConnectorFactoryShim<CosmosDbOptions>.Register(packageResolver.CosmosClientClass.Type, builder.Services, optionNames, createConnection, true);

        return builder;
    }

    private static void RegisterPostProcessors(IConfigurationBuilder builder)
    {
        builder.AddCloudFoundryServiceBindings();
        CloudFoundryServiceBindingConfigurationSource cloudFoundrySource = builder.Sources.OfType<CloudFoundryServiceBindingConfigurationSource>().First();
        cloudFoundrySource.RegisterPostProcessor(new CosmosDbCloudFoundryPostProcessor());

        var connectionStringPostProcessor = new CosmosDbConnectionStringPostProcessor();
        var connectionStringSource = new ConnectionStringPostProcessorConfigurationSource();
        connectionStringSource.RegisterPostProcessor(connectionStringPostProcessor);
        builder.Add(connectionStringSource);
    }

    private static IHealthContributor CreateHealthContributor(IServiceProvider serviceProvider, string serviceBindingName,
        CosmosDbPackageResolver packageResolver)
    {
        ConnectorFactoryShim<CosmosDbOptions> connectorFactoryShim =
            ConnectorFactoryShim<CosmosDbOptions>.FromServiceProvider(serviceProvider, packageResolver.CosmosClientClass.Type);

        ConnectorShim<CosmosDbOptions> connectorShim = connectorFactoryShim.GetNamed(serviceBindingName);

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
}
