// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Steeltoe.Common;
using Steeltoe.Common.HealthChecks;

namespace Steeltoe.Connector.RabbitMQ;

public delegate object CreateRabbitConnection(RabbitMQOptions options, string serviceBindingName);

public static class RabbitMQWebApplicationBuilderExtensions
{
    private static readonly Type ConnectionInterface = RabbitMQTypeLocator.ConnectionInterface;

    public static WebApplicationBuilder AddRabbitMQ(this WebApplicationBuilder builder)
    {
        return AddRabbitMQ(builder, CreateDefaultRabbitConnection);
    }

    public static WebApplicationBuilder AddRabbitMQ(this WebApplicationBuilder builder, CreateRabbitConnection createRabbitConnection)
    {
        ArgumentGuard.NotNull(builder);
        ArgumentGuard.NotNull(createRabbitConnection);

        var connectionStringPostProcessor = new RabbitMQConnectionStringPostProcessor();

        Func<RabbitMQOptions, string, object> createConnection = (options, serviceBindingName) => createRabbitConnection(options, serviceBindingName);

        BaseWebApplicationBuilderExtensions.RegisterConfigurationSource(builder.Configuration, connectionStringPostProcessor);
        BaseWebApplicationBuilderExtensions.RegisterNamedOptions<RabbitMQOptions>(builder, "rabbitmq", CreateHealthContributor);
        BaseWebApplicationBuilderExtensions.RegisterConnectionFactory(builder.Services, ConnectionInterface, true, createConnection);

        return builder;
    }

    private static object CreateDefaultRabbitConnection(RabbitMQOptions options, string serviceBindingName)
    {
        // In RabbitMQ, connections are long-lived and auto-recover from network failures. Channels are multiplexed over a single connection.
        object factory = Activator.CreateInstance(RabbitMQTypeLocator.ConnectionFactory, null);

        if (!string.IsNullOrEmpty(options.ConnectionString))
        {
            RabbitMQTypeLocator.ConnectionFactoryUrlPropertySetter.Invoke(factory, new object[]
            {
                new Uri(options.ConnectionString)
            });
        }

        return RabbitMQTypeLocator.CreateConnectionMethod.Invoke(factory, Array.Empty<object>());
    }

    private static IHealthContributor CreateHealthContributor(IServiceProvider serviceProvider, string bindingName)
    {
        string connectionString = ConnectionFactoryInvoker.GetConnectionString<RabbitMQOptions>(serviceProvider, bindingName, ConnectionInterface);
        string serviceName = $"RabbitMQ-{bindingName}";
        string hostName = GetHostNameFromConnectionString(connectionString);
        object connection = ConnectionFactoryInvoker.CreateConnection<RabbitMQOptions>(serviceProvider, bindingName, ConnectionInterface);
        var logger = serviceProvider.GetRequiredService<ILogger<RabbitMQHealthContributor>>();

        return new RabbitMQHealthContributor(connection, serviceName, hostName, logger);
    }

    private static string GetHostNameFromConnectionString(string connectionString)
    {
        var builder = new RabbitMQConnectionStringBuilder
        {
            ConnectionString = connectionString
        };

        return (string)builder["host"] ?? "localhost";
    }
}
