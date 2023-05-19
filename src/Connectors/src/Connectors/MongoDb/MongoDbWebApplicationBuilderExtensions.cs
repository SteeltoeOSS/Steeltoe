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

        Func<MongoDbOptions, string, object> createMongoClient = (options, _) =>
            MongoClientShim.CreateInstance(packageResolver, options.ConnectionString).Instance;

        BaseWebApplicationBuilderExtensions.RegisterConfigurationSource(builder.Configuration, connectionStringPostProcessor);

        BaseWebApplicationBuilderExtensions.RegisterNamedOptions<MongoDbOptions>(builder, "mongodb",
            (serviceProvider, bindingName) => CreateHealthContributor(serviceProvider, bindingName, packageResolver));

        BaseWebApplicationBuilderExtensions.RegisterConnectorFactory(builder.Services, packageResolver.MongoClientInterface.Type, false, createMongoClient);

        return builder;
    }

    private static IHealthContributor CreateHealthContributor(IServiceProvider serviceProvider, string bindingName, MongoDbPackageResolver packageResolver)
    {
        string connectionString =
            ConnectorFactoryInvoker.GetConnectionString<MongoDbOptions>(serviceProvider, bindingName, packageResolver.MongoClientInterface.Type);

        string serviceName = $"MongoDB-{bindingName}";
        string hostName = GetHostNameFromConnectionString(connectionString);
        object mongoClient = ConnectorFactoryInvoker.GetConnection<MongoDbOptions>(serviceProvider, bindingName, packageResolver.MongoClientInterface.Type);
        var logger = serviceProvider.GetRequiredService<ILogger<MongoDbHealthContributor>>();

        return new MongoDbHealthContributor(mongoClient, serviceName, hostName, logger);
    }

    private static string GetHostNameFromConnectionString(string connectionString)
    {
        var builder = new MongoDbConnectionStringBuilder
        {
            ConnectionString = connectionString
        };

        return (string)builder["server"]!;
    }
}
