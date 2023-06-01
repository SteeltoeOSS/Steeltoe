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
using Steeltoe.Connectors.MySql.DynamicTypeAccess;

namespace Steeltoe.Connectors.MySql;

public static class MySqlServiceCollectionExtensions
{
    public static IServiceCollection AddMySql(this IServiceCollection services, IConfiguration configuration)
    {
        return AddMySql(services, configuration, null);
    }

    public static IServiceCollection AddMySql(this IServiceCollection services, IConfiguration configuration, Action<ConnectorSetupOptions>? setupAction)
    {
        return AddMySql(services, configuration, MySqlPackageResolver.Default, setupAction);
    }

    internal static IServiceCollection AddMySql(this IServiceCollection services, IConfiguration configuration, MySqlPackageResolver packageResolver,
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

        IReadOnlySet<string> optionNames = ConnectorOptionsBinder.RegisterNamedOptions<MySqlOptions>(services, configuration, "mysql", createHealthContributor);

        ConnectorCreateConnection createConnection = (serviceProvider, serviceBindingName) => setupOptions.CreateConnection != null
            ? setupOptions.CreateConnection(serviceProvider, serviceBindingName)
            : CreateConnection(serviceProvider, serviceBindingName, packageResolver);

        ConnectorFactoryShim<MySqlOptions>.Register(packageResolver.MySqlConnectionClass.Type, services, optionNames, createConnection, false);

        return services;
    }

    private static IHealthContributor CreateHealthContributor(IServiceProvider serviceProvider, string serviceBindingName, MySqlPackageResolver packageResolver)
    {
        ConnectorFactoryShim<MySqlOptions> connectorFactoryShim =
            ConnectorFactoryShim<MySqlOptions>.FromServiceProvider(serviceProvider, packageResolver.MySqlConnectionClass.Type);

        ConnectorShim<MySqlOptions> connectorShim = connectorFactoryShim.Get(serviceBindingName);

        var connection = (DbConnection)connectorShim.GetConnection();
        string hostName = GetHostNameFromConnectionString(packageResolver, connectorShim.Options.ConnectionString);
        var logger = serviceProvider.GetRequiredService<ILogger<RelationalDbHealthContributor>>();

        return new RelationalDbHealthContributor(connection, $"MySQL-{serviceBindingName}", hostName, logger);
    }

    private static string GetHostNameFromConnectionString(MySqlPackageResolver packageResolver, string? connectionString)
    {
        var connectionStringBuilderShim = MySqlConnectionStringBuilderShim.CreateInstance(packageResolver);
        connectionStringBuilderShim.Instance.ConnectionString = connectionString;
        return (string)connectionStringBuilderShim.Instance["host"];
    }

    private static DbConnection CreateConnection(IServiceProvider serviceProvider, string serviceBindingName, MySqlPackageResolver packageResolver)
    {
        var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<MySqlOptions>>();
        MySqlOptions options = optionsMonitor.Get(serviceBindingName);

        var mySqlConnectionShim = MySqlConnectionShim.CreateInstance(packageResolver, options.ConnectionString);
        return mySqlConnectionShim.Instance;
    }
}
