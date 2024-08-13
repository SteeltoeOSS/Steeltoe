// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Data.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Connectors.DynamicTypeAccess;
using Steeltoe.Connectors.MySql.DynamicTypeAccess;

namespace Steeltoe.Connectors.MySql;

public static class MySqlServiceCollectionExtensions
{
    /// <summary>
    /// Registers a <see cref="ConnectorFactory{TOptions,TConnection}" /> (with type parameters <see cref="MySqlOptions" /> and
    /// MySqlConnector.MySqlConnection or MySql.Data.MySqlClient.MySqlConnection) to connect to a MySQL compatible database.
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
    public static IServiceCollection AddMySql(this IServiceCollection services, IConfiguration configuration)
    {
        return AddMySql(services, configuration, MySqlPackageResolver.Default);
    }

    /// <summary>
    /// Registers a <see cref="ConnectorFactory{TOptions,TConnection}" /> (with type parameters <see cref="MySqlOptions" /> and
    /// MySqlConnector.MySqlConnection or MySql.Data.MySqlClient.MySqlConnection) to connect to a MySQL compatible database.
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
    public static IServiceCollection AddMySql(this IServiceCollection services, IConfiguration configuration, Action<ConnectorAddOptionsBuilder>? addAction)
    {
        return AddMySql(services, configuration, MySqlPackageResolver.Default, addAction);
    }

    internal static IServiceCollection AddMySql(this IServiceCollection services, IConfiguration configuration, MySqlPackageResolver packageResolver,
        Action<ConnectorAddOptionsBuilder>? addAction = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(packageResolver);

        if (!ConnectorFactoryShim<MySqlOptions>.IsRegistered(packageResolver.MySqlConnectionClass.Type, services))
        {
            var optionsBuilder = new ConnectorAddOptionsBuilder(
                (serviceProvider, serviceBindingName) => CreateConnection(serviceProvider, serviceBindingName, packageResolver),
                (serviceProvider, serviceBindingName) => CreateHealthContributor(serviceProvider, serviceBindingName, packageResolver))
            {
                CacheConnection = false,
                EnableHealthChecks = services.All(descriptor => descriptor.ServiceType != typeof(HealthCheckService))
            };

            addAction?.Invoke(optionsBuilder);

            IReadOnlySet<string> optionNames = ConnectorOptionsBinder.RegisterNamedOptions<MySqlOptions>(services, configuration, "mysql",
                optionsBuilder.EnableHealthChecks ? optionsBuilder.CreateHealthContributor : null);

            ConnectorFactoryShim<MySqlOptions>.Register(packageResolver.MySqlConnectionClass.Type, services, optionNames, optionsBuilder.CreateConnection,
                optionsBuilder.CacheConnection);
        }

        return services;
    }

    private static IHealthContributor CreateHealthContributor(IServiceProvider serviceProvider, string serviceBindingName, MySqlPackageResolver packageResolver)
    {
        ConnectorFactoryShim<MySqlOptions> connectorFactoryShim =
            ConnectorFactoryShim<MySqlOptions>.FromServiceProvider(serviceProvider, packageResolver.MySqlConnectionClass.Type);

        ConnectorShim<MySqlOptions> connectorShim = connectorFactoryShim.Get(serviceBindingName);

        var connection = (DbConnection)connectorShim.GetConnection();
        string? hostName = GetHostNameFromConnectionString(packageResolver, connectorShim.Options.ConnectionString);
        var logger = serviceProvider.GetRequiredService<ILogger<RelationalDatabaseHealthContributor>>();

        return new RelationalDatabaseHealthContributor(connection, hostName, logger)
        {
            ServiceName = serviceBindingName
        };
    }

    private static string? GetHostNameFromConnectionString(MySqlPackageResolver packageResolver, string? connectionString)
    {
        var connectionStringBuilderShim = MySqlConnectionStringBuilderShim.CreateInstance(packageResolver);
        connectionStringBuilderShim.Instance.ConnectionString = connectionString;
        return (string?)connectionStringBuilderShim.Instance["host"];
    }

    private static DbConnection CreateConnection(IServiceProvider serviceProvider, string serviceBindingName, MySqlPackageResolver packageResolver)
    {
        var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<MySqlOptions>>();
        MySqlOptions options = optionsMonitor.Get(serviceBindingName);

        var mySqlConnectionShim = MySqlConnectionShim.CreateInstance(packageResolver, options.ConnectionString);
        return mySqlConnectionShim.Instance;
    }
}
