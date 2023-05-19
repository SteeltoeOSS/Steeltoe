// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Steeltoe.Common;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Connectors.RabbitMQ.RuntimeTypeAccess;

namespace Steeltoe.Connectors.RabbitMQ;

public delegate IDisposable CreateRabbitConnection(RabbitMQOptions options, string serviceBindingName);

public static class RabbitMQWebApplicationBuilderExtensions
{
    public static WebApplicationBuilder AddRabbitMQ(this WebApplicationBuilder builder, CreateRabbitConnection? createRabbitConnection = null)
    {
        return AddRabbitMQ(builder, new RabbitMQPackageResolver(), createRabbitConnection);
    }

    private static WebApplicationBuilder AddRabbitMQ(this WebApplicationBuilder builder, RabbitMQPackageResolver packageResolver,
        CreateRabbitConnection? createRabbitConnection)
    {
        ArgumentGuard.NotNull(builder);
        ArgumentGuard.NotNull(packageResolver);

        var connectionStringPostProcessor = new RabbitMQConnectionStringPostProcessor();

        Func<RabbitMQOptions, string, object> createConnection = (options, serviceBindingName) => createRabbitConnection != null
            ? createRabbitConnection(options, serviceBindingName)
            : CreateDefaultRabbitConnection(options, packageResolver);

        BaseWebApplicationBuilderExtensions.RegisterConfigurationSource(builder.Configuration, connectionStringPostProcessor);

        BaseWebApplicationBuilderExtensions.RegisterNamedOptions<RabbitMQOptions>(builder, "rabbitmq",
            (serviceProvider, bindingName) => CreateHealthContributor(serviceProvider, bindingName, packageResolver));

        BaseWebApplicationBuilderExtensions.RegisterConnectorFactory(builder.Services, packageResolver.ConnectionInterface.Type, true, createConnection);

        return builder;
    }

    private static IDisposable CreateDefaultRabbitConnection(RabbitMQOptions options, RabbitMQPackageResolver packageResolver)
    {
        // In RabbitMQ, connections are long-lived and auto-recover from network failures. Channels are multiplexed over a single connection.
        var connectionFactoryShim = ConnectionFactoryShim.CreateInstance(packageResolver);

        if (!string.IsNullOrEmpty(options.ConnectionString))
        {
            connectionFactoryShim.Uri = new Uri(options.ConnectionString);
        }

        ConnectionInterfaceShim connectionInterfaceShim = connectionFactoryShim.CreateConnection();
        return connectionInterfaceShim.Instance;
    }

    private static IHealthContributor CreateHealthContributor(IServiceProvider serviceProvider, string bindingName, RabbitMQPackageResolver packageResolver)
    {
        string connectionString =
            ConnectorFactoryInvoker.GetConnectionString<RabbitMQOptions>(serviceProvider, bindingName, packageResolver.ConnectionInterface.Type);

        string serviceName = $"RabbitMQ-{bindingName}";
        string hostName = GetHostNameFromConnectionString(connectionString);
        object connection = ConnectorFactoryInvoker.GetConnection<RabbitMQOptions>(serviceProvider, bindingName, packageResolver.ConnectionInterface.Type);
        var logger = serviceProvider.GetRequiredService<ILogger<RabbitMQHealthContributor>>();

        return new RabbitMQHealthContributor(connection, serviceName, hostName, logger);
    }

    private static string GetHostNameFromConnectionString(string connectionString)
    {
        var builder = new RabbitMQConnectionStringBuilder
        {
            ConnectionString = connectionString
        };

        return (string?)builder["host"] ?? "localhost";
    }
}
