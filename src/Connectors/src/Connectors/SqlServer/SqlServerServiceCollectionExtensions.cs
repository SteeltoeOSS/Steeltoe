// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Data.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Connectors.DynamicTypeAccess;
using Steeltoe.Connectors.SqlServer.RuntimeTypeAccess;

namespace Steeltoe.Connectors.SqlServer;

public static class SqlServerServiceCollectionExtensions
{
    public static IServiceCollection AddSqlServer(this IServiceCollection services, IConfiguration configuration)
    {
        return AddSqlServer(services, configuration, null);
    }

    public static IServiceCollection AddSqlServer(this IServiceCollection services, IConfiguration configuration, Action<ConnectorAddOptions>? addAction)
    {
        return AddSqlServer(services, configuration, SqlServerPackageResolver.Default, addAction);
    }

    internal static IServiceCollection AddSqlServer(this IServiceCollection services, IConfiguration configuration, SqlServerPackageResolver packageResolver,
        Action<ConnectorAddOptions>? addAction)
    {
        ArgumentGuard.NotNull(services);
        ArgumentGuard.NotNull(configuration);
        ArgumentGuard.NotNull(packageResolver);

        if (!ConnectorFactoryShim<SqlServerOptions>.IsRegistered(packageResolver.SqlConnectionClass.Type, services))
        {
            var addOptions = new ConnectorAddOptions(
                (serviceProvider, serviceBindingName) => CreateConnection(serviceProvider, serviceBindingName, packageResolver),
                (serviceProvider, serviceBindingName) => CreateHealthContributor(serviceProvider, serviceBindingName, packageResolver))
            {
                CacheConnection = false,
                EnableHealthChecks = services.All(descriptor => descriptor.ServiceType != typeof(HealthCheckService))
            };

            addAction?.Invoke(addOptions);

            IReadOnlySet<string> optionNames = ConnectorOptionsBinder.RegisterNamedOptions<SqlServerOptions>(services, configuration, "sqlserver",
                addOptions.EnableHealthChecks ? addOptions.CreateHealthContributor : null);

            ConnectorFactoryShim<SqlServerOptions>.Register(packageResolver.SqlConnectionClass.Type, services, optionNames, addOptions.CreateConnection,
                addOptions.CacheConnection);
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
        string hostName = GetHostNameFromConnectionString(packageResolver, connectorShim.Options.ConnectionString);
        var logger = serviceProvider.GetRequiredService<ILogger<RelationalDbHealthContributor>>();

        return new RelationalDbHealthContributor(connection, $"SqlServer-{serviceBindingName}", hostName, logger);
    }

    private static string GetHostNameFromConnectionString(SqlServerPackageResolver packageResolver, string? connectionString)
    {
        var connectionStringBuilderShim = SqlConnectionStringBuilderShim.CreateInstance(packageResolver);
        connectionStringBuilderShim.Instance.ConnectionString = connectionString;
        return (string)connectionStringBuilderShim.Instance["server"];
    }

    private static DbConnection CreateConnection(IServiceProvider serviceProvider, string serviceBindingName, SqlServerPackageResolver packageResolver)
    {
        var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<SqlServerOptions>>();
        SqlServerOptions options = optionsMonitor.Get(serviceBindingName);

        var sqlConnectionShim = SqlConnectionShim.CreateInstance(packageResolver, options.ConnectionString);
        return sqlConnectionShim.Instance;
    }
}
