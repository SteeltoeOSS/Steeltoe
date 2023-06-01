// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Data.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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

    public static IServiceCollection AddSqlServer(this IServiceCollection services, IConfiguration configuration, Action<ConnectorSetupOptions>? setupAction)
    {
        return AddSqlServer(services, configuration, SqlServerPackageResolver.Default, setupAction);
    }

    internal static IServiceCollection AddSqlServer(this IServiceCollection services, IConfiguration configuration, SqlServerPackageResolver packageResolver,
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
            ConnectorOptionsBinder.RegisterNamedOptions<SqlServerOptions>(services, configuration, "sqlserver", createHealthContributor);

        ConnectorCreateConnection createConnection = (serviceProvider, serviceBindingName) => setupOptions.CreateConnection != null
            ? setupOptions.CreateConnection(serviceProvider, serviceBindingName)
            : CreateConnection(serviceProvider, serviceBindingName, packageResolver);

        ConnectorFactoryShim<SqlServerOptions>.Register(packageResolver.SqlConnectionClass.Type, services, optionNames, createConnection, false);

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
