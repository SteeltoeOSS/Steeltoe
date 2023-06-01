// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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

    public static IServiceCollection AddRabbitMQ(this IServiceCollection services, IConfiguration configuration, Action<ConnectorSetupOptions>? setupAction)
    {
        return AddRabbitMQ(services, configuration, RabbitMQPackageResolver.Default, setupAction);
    }

    private static IServiceCollection AddRabbitMQ(this IServiceCollection services, IConfiguration configuration, RabbitMQPackageResolver packageResolver,
        Action<ConnectorSetupOptions>? setupAction)
    {
        ArgumentGuard.NotNull(services);
        ArgumentGuard.NotNull(configuration);
        ArgumentGuard.NotNull(packageResolver);

        var setupOptions = new ConnectorSetupOptions();
        setupAction?.Invoke(setupOptions);

        ConnectorCreateHealthContributor? createHealthContributor = setupOptions.EnableHealthChecks
            ? (serviceProvider, serviceBindingName) => setupOptions.CreateHealthContributor != null
                ? setupOptions.CreateHealthContributor(serviceProvider, serviceBindingName)
                : CreateHealthContributor(serviceProvider, serviceBindingName, packageResolver)
            : null;

        IReadOnlySet<string> optionNames =
            ConnectorOptionsBinder.RegisterNamedOptions<RabbitMQOptions>(services, configuration, "rabbitmq", createHealthContributor);

        ConnectorCreateConnection createConnection = (serviceProvider, serviceBindingName) => setupOptions.CreateConnection != null
            ? setupOptions.CreateConnection(serviceProvider, serviceBindingName)
            : CreateConnection(serviceProvider, serviceBindingName, packageResolver);

        ConnectorFactoryShim<RabbitMQOptions>.Register(packageResolver.ConnectionInterface.Type, services, optionNames, createConnection, true);

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

        // In RabbitMQ, connections are long-lived and auto-recover from network failures. Channels are multiplexed over a single connection.
        var connectionFactoryShim = ConnectionFactoryShim.CreateInstance(packageResolver);

        if (!string.IsNullOrEmpty(options.ConnectionString))
        {
            connectionFactoryShim.Uri = new Uri(options.ConnectionString);
        }

        ConnectionInterfaceShim connectionInterfaceShim = connectionFactoryShim.CreateConnection();
        return connectionInterfaceShim.Instance;
    }
}
