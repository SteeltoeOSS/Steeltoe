// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Steeltoe.Common;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Connectors.MongoDb.RuntimeTypeAccess;
using Steeltoe.Connectors.RuntimeTypeAccess;

namespace Steeltoe.Connectors.MongoDb;

public static class MongoDbWebApplicationBuilderExtensions
{
    public static WebApplicationBuilder AddMongoDb(this WebApplicationBuilder builder)
    {
        return AddMongoDb(builder, new MongoDbPackageResolver());
    }

    private static WebApplicationBuilder AddMongoDb(this WebApplicationBuilder builder, MongoDbPackageResolver packageResolver)
    {
        ArgumentGuard.NotNull(builder);
        ArgumentGuard.NotNull(packageResolver);

        var connectionStringPostProcessor = new MongoDbConnectionStringPostProcessor();
        BaseWebApplicationBuilderExtensions.RegisterConfigurationSource(builder.Configuration, connectionStringPostProcessor);

        Func<IServiceProvider, string, IHealthContributor> createHealthContributor = (serviceProvider, serviceBindingName) =>
            CreateHealthContributor(serviceProvider, serviceBindingName, packageResolver);

        BaseWebApplicationBuilderExtensions.RegisterNamedOptions<MongoDbOptions>(builder, "mongodb", createHealthContributor);

        Func<MongoDbOptions, string, object> createMongoClient = (options, _) =>
            MongoClientShim.CreateInstance(packageResolver, options.ConnectionString!).Instance;

        ConnectorFactoryShim<MongoDbOptions>.Register(builder.Services, packageResolver.MongoClientInterface.Type, false, createMongoClient);

        return builder;
    }

    private static IHealthContributor CreateHealthContributor(IServiceProvider serviceProvider, string serviceBindingName,
        MongoDbPackageResolver packageResolver)
    {
        ConnectorFactoryShim<MongoDbOptions> connectorFactoryShim =
            ConnectorFactoryShim<MongoDbOptions>.FromServiceProvider(serviceProvider, packageResolver.MongoClientInterface.Type);

        ConnectorShim<MongoDbOptions> connectorShim = connectorFactoryShim.GetNamed(serviceBindingName);

        object mongoClient = connectorShim.GetConnection();
        string hostName = GetHostNameFromConnectionString(connectorShim.Options.ConnectionString);
        var logger = serviceProvider.GetRequiredService<ILogger<MongoDbHealthContributor>>();

        return new MongoDbHealthContributor(mongoClient, $"MongoDB-{serviceBindingName}", hostName, logger);
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
}
