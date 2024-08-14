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
using Steeltoe.Connectors.SqlServer.RuntimeTypeAccess;

namespace Steeltoe.Connectors.SqlServer;

public static class SqlServerServiceCollectionExtensions
{
    /// <summary>
    /// Registers a <see cref="ConnectorFactory{TOptions,TConnection}" /> (with type parameters <see cref="SqlServerOptions" /> and
    /// Microsoft.Data.SqlClient.SqlConnection or System.Data.SqlClient.SqlConnection) to connect to a SQL Server database.
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
    public static IServiceCollection AddSqlServer(this IServiceCollection services, IConfiguration configuration)
    {
        return AddSqlServer(services, configuration, SqlServerPackageResolver.Default);
    }

    /// <summary>
    /// Registers a <see cref="ConnectorFactory{TOptions,TConnection}" /> (with type parameters <see cref="SqlServerOptions" /> and
    /// Microsoft.Data.SqlClient.SqlConnection or System.Data.SqlClient.SqlConnection) to connect to a SQL Server database.
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
    public static IServiceCollection AddSqlServer(this IServiceCollection services, IConfiguration configuration, Action<ConnectorAddOptionsBuilder>? addAction)
    {
        return AddSqlServer(services, configuration, SqlServerPackageResolver.Default, addAction);
    }

    internal static IServiceCollection AddSqlServer(this IServiceCollection services, IConfiguration configuration, SqlServerPackageResolver packageResolver,
        Action<ConnectorAddOptionsBuilder>? addAction = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(packageResolver);

        if (!ConnectorFactoryShim<SqlServerOptions>.IsRegistered(packageResolver.SqlConnectionClass.Type, services))
        {
            var optionsBuilder = new ConnectorAddOptionsBuilder(
                (serviceProvider, serviceBindingName) => CreateConnection(serviceProvider, serviceBindingName, packageResolver),
                (serviceProvider, serviceBindingName) => CreateHealthContributor(serviceProvider, serviceBindingName, packageResolver))
            {
                CacheConnection = false,
                EnableHealthChecks = services.All(descriptor => descriptor.ServiceType != typeof(HealthCheckService))
            };

            addAction?.Invoke(optionsBuilder);

            IReadOnlySet<string> optionNames = ConnectorOptionsBinder.RegisterNamedOptions<SqlServerOptions>(services, configuration, "sqlserver",
                optionsBuilder.EnableHealthChecks ? optionsBuilder.CreateHealthContributor : null);

            ConnectorFactoryShim<SqlServerOptions>.Register(packageResolver.SqlConnectionClass.Type, services, optionNames, optionsBuilder.CreateConnection,
                optionsBuilder.CacheConnection);
        }

        return services;
    }

    private static IHealthContributor CreateHealthContributor(IServiceProvider serviceProvider, string serviceBindingName,
        SqlServerPackageResolver packageResolver)
    {
        ConnectorFactoryShim<SqlServerOptions> connectorFactoryShim =
            ConnectorFactoryShim<SqlServerOptions>.FromServiceProvider(serviceProvider, packageResolver.SqlConnectionClass.Type);

        ConnectorShim<SqlServerOptions> connectorShim = connectorFactoryShim.Get(serviceBindingName);

        var connection = (DbConnection)connectorShim.GetConnection();
        string? hostName = GetHostNameFromConnectionString(packageResolver, connectorShim.Options.ConnectionString);
        var logger = serviceProvider.GetRequiredService<ILogger<RelationalDatabaseHealthContributor>>();

        return new RelationalDatabaseHealthContributor(connection, hostName, logger)
        {
            ServiceName = serviceBindingName
        };
    }

    private static string? GetHostNameFromConnectionString(SqlServerPackageResolver packageResolver, string? connectionString)
    {
        var connectionStringBuilderShim = SqlConnectionStringBuilderShim.CreateInstance(packageResolver);
        connectionStringBuilderShim.Instance.ConnectionString = connectionString;
        return (string?)connectionStringBuilderShim.Instance["server"];
    }

    private static DbConnection CreateConnection(IServiceProvider serviceProvider, string serviceBindingName, SqlServerPackageResolver packageResolver)
    {
        var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<SqlServerOptions>>();
        SqlServerOptions options = optionsMonitor.Get(serviceBindingName);

        var sqlConnectionShim = SqlConnectionShim.CreateInstance(packageResolver, options.ConnectionString);
        return sqlConnectionShim.Instance;
    }
}
