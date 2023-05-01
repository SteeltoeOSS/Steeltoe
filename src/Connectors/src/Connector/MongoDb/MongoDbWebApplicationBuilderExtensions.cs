// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Steeltoe.Common;
using Steeltoe.Common.HealthChecks;

namespace Steeltoe.Connector.MongoDb;

public static class MongoDbWebApplicationBuilderExtensions
{
    private static readonly Type ConnectionType = MongoDbTypeLocator.MongoClient;
    private static readonly Type ConnectionInterface = MongoDbTypeLocator.MongoClientInterface;

    public static WebApplicationBuilder AddMongoDb(this WebApplicationBuilder builder)
    {
        ArgumentGuard.NotNull(builder);

        var connectionStringPostProcessor = new MongoDbConnectionStringPostProcessor();

        Func<MongoDbOptions, string, object> createMongoClient = (options, _) => Activator.CreateInstance(ConnectionType, options.ConnectionString);

        BaseWebApplicationBuilderExtensions.RegisterConfigurationSource(builder.Configuration, connectionStringPostProcessor);
        BaseWebApplicationBuilderExtensions.RegisterNamedOptions<MongoDbOptions>(builder, "mongodb", CreateHealthContributor);
        BaseWebApplicationBuilderExtensions.RegisterConnectionFactory(builder.Services, ConnectionInterface, false, createMongoClient);

        return builder;
    }

    private static IHealthContributor CreateHealthContributor(IServiceProvider serviceProvider, string bindingName)
    {
        string connectionString = ConnectionFactoryInvoker.GetConnectionString<MongoDbOptions>(serviceProvider, bindingName, ConnectionInterface);
        string serviceName = $"MongoDB-{bindingName}";
        string hostName = GetHostNameFromConnectionString(connectionString);
        object mongoClient = ConnectionFactoryInvoker.CreateConnection<MongoDbOptions>(serviceProvider, bindingName, ConnectionInterface);
        var logger = serviceProvider.GetRequiredService<ILogger<MongoDbHealthContributor>>();

        return new MongoDbHealthContributor(mongoClient, serviceName, hostName, logger);
    }

    private static string GetHostNameFromConnectionString(string connectionString)
    {
        var builder = new MongoDbConnectionStringBuilder
        {
            ConnectionString = connectionString
        };

        return (string)builder["server"];
    }
}
