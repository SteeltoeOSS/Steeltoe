// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Connectors.DynamicTypeAccess;
using Steeltoe.Connectors.RabbitMQ.DynamicTypeAccess;

namespace Steeltoe.Connectors.RabbitMQ;

public static class RabbitMQServiceCollectionExtensions
{
    /// <summary>
    /// Registers a <see cref="ConnectorFactory{TOptions,TConnection}" /> (with type parameters <see cref="RabbitMQOptions" /> and
    /// RabbitMQ.Client.IConnection) to connect to a RabbitMQ server.
    /// </summary>
    /// <param name="services">
    /// The <see cref="IServiceCollection" /> to add services to.
    /// </param>
    /// <param name="configuration">
    /// The <see cref="IConfiguration" /> to read application settings from.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="services" /> so that additional calls can be chained.
    /// </returns>
    public static IServiceCollection AddRabbitMQ(this IServiceCollection services, IConfiguration configuration)
    {
        return AddRabbitMQ(services, configuration, RabbitMQPackageResolver.Default);
    }

    /// <summary>
    /// Registers a <see cref="ConnectorFactory{TOptions,TConnection}" /> (with type parameters <see cref="RabbitMQOptions" /> and
    /// RabbitMQ.Client.IConnection) to connect to a RabbitMQ server.
    /// </summary>
    /// <param name="services">
    /// The <see cref="IServiceCollection" /> to add services to.
    /// </param>
    /// <param name="configuration">
    /// The <see cref="IConfiguration" /> to read application settings from.
    /// </param>
    /// <param name="addAction">
    /// An optional delegate to configure this connector.
    /// </param>
    /// <returns>
    /// The incoming <paramref name="services" /> so that additional calls can be chained.
    /// </returns>
    public static IServiceCollection AddRabbitMQ(this IServiceCollection services, IConfiguration configuration, Action<ConnectorAddOptionsBuilder>? addAction)
    {
        return AddRabbitMQ(services, configuration, RabbitMQPackageResolver.Default, addAction);
    }

    private static IServiceCollection AddRabbitMQ(this IServiceCollection services, IConfiguration configuration, RabbitMQPackageResolver packageResolver,
        Action<ConnectorAddOptionsBuilder>? addAction = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(packageResolver);

        if (!ConnectorFactoryShim<RabbitMQOptions>.IsRegistered(packageResolver.ConnectionInterface.Type, services))
        {
            var optionsBuilder = new ConnectorAddOptionsBuilder(
                (serviceProvider, serviceBindingName) => CreateConnection(serviceProvider, serviceBindingName, packageResolver),
                (serviceProvider, serviceBindingName) => CreateHealthContributor(serviceProvider, serviceBindingName, packageResolver))
            {
                // From https://www.rabbitmq.com/dotnet-api-guide.html#connection-and-channel-lifespan:
                //   "Connections are meant to be long-lived. The underlying protocol is designed and optimized for long-running connections.
                //   That means that opening a new connection per operation, e.g. a message published, is unnecessary and strongly discouraged
                //   as it will introduce a lot of network round-trips and overhead."
                CacheConnection = true,
                EnableHealthChecks = services.All(descriptor => descriptor.ServiceType != typeof(HealthCheckService))
            };

            addAction?.Invoke(optionsBuilder);

            IReadOnlySet<string> optionNames = ConnectorOptionsBinder.RegisterNamedOptions<RabbitMQOptions>(services, configuration, "rabbitmq",
                optionsBuilder.EnableHealthChecks ? optionsBuilder.CreateHealthContributor : null);

            ConnectorFactoryShim<RabbitMQOptions>.Register(packageResolver.ConnectionInterface.Type, services, optionNames, optionsBuilder.CreateConnection,
                optionsBuilder.CacheConnection);
        }

        return services;
    }

    private static RabbitMQHealthContributor CreateHealthContributor(IServiceProvider serviceProvider, string serviceBindingName,
        RabbitMQPackageResolver packageResolver)
    {
        // Not using the Steeltoe ConnectorFactory here, because obtaining a connection throws when RabbitMQ is down at application startup.

        var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<RabbitMQOptions>>();
        string? connectionString = optionsMonitor.Get(serviceBindingName).ConnectionString;

        string hostName = GetHostNameFromConnectionString(connectionString);
        var logger = serviceProvider.GetRequiredService<ILogger<RabbitMQHealthContributor>>();

        var connectionFactoryShim = ConnectionFactoryShim.CreateInstance(packageResolver);

        if (connectionString != null)
        {
            connectionFactoryShim.Uri = new Uri(connectionString);
        }

        return new RabbitMQHealthContributor(connectionFactoryShim.Instance, hostName, logger)
        {
            ServiceName = serviceBindingName
        };
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

        ConnectionFactoryInterfaceShim connectionFactoryInterfaceShim = connectionFactoryShim.AsInterface();
        ConnectionInterfaceShim connectionInterfaceShim = connectionFactoryInterfaceShim.CreateConnection();
        return connectionInterfaceShim.Instance;
    }
}
