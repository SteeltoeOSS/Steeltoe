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
    public static WebApplicationBuilder AddMongoDb(this WebApplicationBuilder builder)
    {
        ArgumentGuard.NotNull(builder);

        var connectionStringPostProcessor = new MongoDbConnectionStringPostProcessor();
        Type connectionType = MongoDbTypeLocator.MongoClient;

        BaseWebApplicationBuilderExtensions.RegisterConfigurationSource(builder.Configuration, connectionStringPostProcessor);
        BaseWebApplicationBuilderExtensions.RegisterNamedOptions<MongoDbOptions>(builder, "mongodb", CreateHealthContributor);
        BaseWebApplicationBuilderExtensions.RegisterConnectionFactory<MongoDbOptions>(builder.Services, connectionType);

        return builder;
    }

    private static IHealthContributor CreateHealthContributor(IServiceProvider serviceProvider, string bindingName)
    {
        string connectionString = ConnectionFactoryInvoker.GetConnectionString<MongoDbOptions>(serviceProvider, bindingName, MongoDbTypeLocator.MongoClient);
        string serviceName = $"MongoDB-{bindingName}";
        string hostName = GetHostNameFromConnectionString(connectionString);
        object mongoClient = ConnectionFactoryInvoker.CreateConnection<MongoDbOptions>(serviceProvider, bindingName, MongoDbTypeLocator.MongoClient);
        var logger = serviceProvider.GetRequiredService<ILogger<MongoDbHealthContributor>>();

        return new MongoDbHealthContributor(mongoClient, serviceName, hostName, logger);
    }

    private static string GetHostNameFromConnectionString(string connectionString)
    {
        var uri = new Uri(connectionString);
        return uri.Host;
    }
}
