// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Connectors.DynamicTypeAccess;
using Steeltoe.Connectors.RabbitMQ.DynamicTypeAccess;

namespace Steeltoe.Connectors.RabbitMQ;

public static class RabbitMQServiceCollectionExtensions
{
    public static IServiceCollection AddRabbitMQ(this IServiceCollection services, IConfiguration configuration)
    {
        return AddRabbitMQ(services, configuration, null);
    }

    public static IServiceCollection AddRabbitMQ(this IServiceCollection services, IConfiguration configuration, Action<ConnectorAddOptions>? addAction)
    {
        return AddRabbitMQ(services, configuration, RabbitMQPackageResolver.Default, addAction);
    }

    private static IServiceCollection AddRabbitMQ(this IServiceCollection services, IConfiguration configuration, RabbitMQPackageResolver packageResolver,
        Action<ConnectorAddOptions>? addAction)
    {
        ArgumentGuard.NotNull(services);
        ArgumentGuard.NotNull(configuration);
        ArgumentGuard.NotNull(packageResolver);

        var addOptions = new ConnectorAddOptions(
            (serviceProvider, serviceBindingName) => CreateConnection(serviceProvider, serviceBindingName, packageResolver),
            (serviceProvider, serviceBindingName) => CreateHealthContributor(serviceProvider, serviceBindingName, packageResolver))
        {
            // From https://www.rabbitmq.com/dotnet-api-guide.html#connection-and-channel-lifespan:
            //   "Connections are meant to be long-lived. The underlying protocol is designed and optimized for long running connections.
            //   That means that opening a new connection per operation, e.g. a message published, is unnecessary and strongly discouraged
            //   as it will introduce a lot of network round-trips and overhead."
            CacheConnection = true,
            EnableHealthChecks = services.All(descriptor => descriptor.ServiceType != typeof(HealthCheckService))
        };

        addAction?.Invoke(addOptions);

        IReadOnlySet<string> optionNames = ConnectorOptionsBinder.RegisterNamedOptions<RabbitMQOptions>(services, configuration, "rabbitmq",
            addOptions.EnableHealthChecks ? addOptions.CreateHealthContributor : null);

        ConnectorFactoryShim<RabbitMQOptions>.Register(packageResolver.ConnectionInterface.Type, services, optionNames, addOptions.CreateConnection,
            addOptions.CacheConnection);

        return services;
    }

    private static IHealthContributor CreateHealthContributor(IServiceProvider serviceProvider, string serviceBindingName,
        RabbitMQPackageResolver packageResolver)
    {
        ConnectorFactoryShim<RabbitMQOptions> connectorFactoryShim =
            ConnectorFactoryShim<RabbitMQOptions>.FromServiceProvider(serviceProvider, packageResolver.ConnectionInterface.Type);

        ConnectorShim<RabbitMQOptions> connectorShim = connectorFactoryShim.Get(serviceBindingName);

        object connection = connectorShim.GetConnection();
        string hostName = GetHostNameFromConnectionString(connectorShim.Options.ConnectionString);
        var logger = serviceProvider.GetRequiredService<ILogger<RabbitMQHealthContributor>>();

        return new RabbitMQHealthContributor(connection, $"RabbitMQ-{serviceBindingName}", hostName, logger);
    }

    private static string GetHostNameFromConnectionString(string? connectionString)
    {
        var builder = new RabbitMQConnectionStringBuilder
        {
            ConnectionString = connectionString
        };

        return (string?)builder["host"] ?? "localhost";
    }

    private static IDisposable CreateConnection(IServiceProvider serviceProvider, string serviceBindingName, RabbitMQPackageResolver packageResolver)
    {
        var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<RabbitMQOptions>>();
        RabbitMQOptions options = optionsMonitor.Get(serviceBindingName);

        var connectionFactoryShim = ConnectionFactoryShim.CreateInstance(packageResolver);

        if (!string.IsNullOrEmpty(options.ConnectionString))
        {
            connectionFactoryShim.Uri = new Uri(options.ConnectionString);
        }

        ConnectionInterfaceShim connectionInterfaceShim = connectionFactoryShim.CreateConnection();
        return connectionInterfaceShim.Instance;
    }
}
